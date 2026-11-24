using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Services;

public class SeedDataHostedService : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeedDataHostedService> _logger;

    public SeedDataHostedService(
        IServiceProvider serviceProvider,
        ILogger<SeedDataHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var persistence = scope.ServiceProvider.GetRequiredService<IPersistence>();

        var existingCount = await persistence.Count<Speciality>(_ => true);
        if (existingCount > 0)
        {
            _logger.LogInformation("Seed inicial omitido: ya existen {Count} especialidades en la base", existingCount);
            return;
        }

        var seedPath = Path.Combine(AppContext.BaseDirectory, "Sources", "seed-data.json");
        if (!File.Exists(seedPath))
        {
            _logger.LogWarning("No se encontró el archivo seed-data.json en {Path}", seedPath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath, cancellationToken);
        var seed = JsonSerializer.Deserialize<SeedPayload>(json, JsonOptions);
        if (seed is null)
        {
            _logger.LogWarning("El archivo seed-data.json está vacío o mal formado");
            return;
        }

        var specialityService = scope.ServiceProvider.GetRequiredService<ISpecialityService>();
        var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
        var availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
        var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Especialidades
        var specialitiesByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in seed.Specialities)
        {
            var created = await specialityService.Create(
                new SpecialityModel.Request(item.Name, item.Description));
            specialitiesByName[created.Name] = created.Id;
        }

        // 2. Médicos
        var doctorsByLicense = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in seed.Doctors)
        {
            if (!specialitiesByName.TryGetValue(item.SpecialityName, out var specialityId))
            {
                _logger.LogWarning("Especialidad '{Speciality}' no encontrada para el médico '{Doctor}'",
                    item.SpecialityName, item.Name);
                continue;
            }

            var created = await doctorService.Create(
                new DoctorModel.Request(item.Name, item.LicenseNumber, specialityId));
            doctorsByLicense[created.LicenseNumber] = created.Id;
        }

        // 3. Disponibilidades (y generación de slots de 30 min)
        var currentMonth = DateTime.Now.Month;
        foreach (var item in seed.Availabilities)
        {
            if (!doctorsByLicense.TryGetValue(item.DoctorLicense, out var doctorId))
                continue;

            var targetMonth = item.Month >= currentMonth ? item.Month : currentMonth;
            var days = item.Days
                .Select(d => new AvailabilityModel.DayRuleRequest(d.DayOfWeek, d.StartTime, d.EndTime))
                .ToList();

            await availabilityService.Create(
                new AvailabilityModel.Request(doctorId, targetMonth, days));
        }

        // 4. Pacientes (Identity + tabla Patients)
        foreach (var item in seed.Patients)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = item.Email,
                    Email = item.Email,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("No se pudo crear el usuario paciente {Email}: {Errors}",
                        item.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                await userManager.AddToRoleAsync(user, Roles.Patient);
            }

            var existingPatient = await persistence.First<Patient>(p => p.Dni == item.Dni);
            if (existingPatient is null)
            {
                var patient = new Patient(item.Dni, user.Id, item.Name, item.PhoneNumber);
                await persistence.Add(patient);
            }
        }

        // 5. Turnos / Citas
        var createdAppointments = 0;
        foreach (var item in seed.Appointments)
        {
            if (!doctorsByLicense.TryGetValue(item.DoctorLicense, out var doctorId))
                continue;

            var slots = (await availabilityService.GetSlotsByDoctor(doctorId)).ToList();
            if (slots.Count == 0)
            {
                _logger.LogWarning("No hay slots disponibles para el médico {License}", item.DoctorLicense);
                continue;
            }

            var slot = slots[0];
            await appointmentService.Create(new AppointmentModel.Request(
                doctorId,
                slot.Id,
                new AppointmentModel.PatientRequest(item.PatientDni),
                item.Reason));
            createdAppointments++;
        }

        _logger.LogInformation(
            "Seed inicial completado desde seed-data.json: {Specialities} especialidades, {Doctors} médicos, {Patients} pacientes, {Appointments} citas",
            specialitiesByName.Count, doctorsByLicense.Count, seed.Patients.Count, createdAppointments);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private sealed record SeedPayload(
        List<SeedSpeciality> Specialities,
        List<SeedDoctor> Doctors,
        List<SeedAvailability> Availabilities,
        List<SeedPatient> Patients,
        List<SeedAppointment> Appointments);

    private sealed record SeedSpeciality(string Name, string Description);
    private sealed record SeedDoctor(string Name, string LicenseNumber, string SpecialityName);
    private sealed record SeedAvailability(string DoctorLicense, int Month, List<SeedDayRule> Days);
    private sealed record SeedDayRule(string DayOfWeek, string StartTime, string EndTime);
    private sealed record SeedPatient(string Email, string Dni, string Name, string PhoneNumber);
    private sealed record SeedAppointment(string PatientDni, string DoctorLicense, string Reason);
}
