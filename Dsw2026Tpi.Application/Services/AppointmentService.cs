using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IPersistence persistence, ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<AppointmentModel.Response> Create(AppointmentModel.Request request)
    {
        ValidationsExtensions.ValidateStringLength(request.Motive, 3, 500, ErrorCodes.APPOINTMENT_INVALID_MOTIVE, nameof(ErrorCodes.APPOINTMENT_INVALID_MOTIVE));

        if (!request.Patient.Dni.IsDniValid())
            throw new ValidationException(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND, nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));

        var doctor = await _persistence.GetById<Doctor>(request.DoctorId)
        ?? throw new EntityNotFoundException(nameof(Doctor));

        var patient = await _persistence.First<Patient>(p => p.Dni == request.Patient.Dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var turn = await _persistence.GetById<Turn>(request.AvailabilitySlotId, nameof(Turn.Availability))
            ?? throw new EntityNotFoundException(nameof(Turn));

        if (turn.Availability?.DoctorId != request.DoctorId)
            throw new ValidationException(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE, nameof(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE));

        if (turn.ScheduledDate.Date < DateTime.UtcNow.Date)
            throw new ValidationException(ErrorCodes.APPOINTMENT_PAST_DATE, nameof(ErrorCodes.APPOINTMENT_PAST_DATE));

        if (turn.State != TurnState.AVAILABLE)
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE), ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE);

        var appointment = new Date(turn.ScheduledDate, patient, turn, request.Motive);

        try
        {
            turn.Reserve(appointment);
            await _persistence.Add(appointment);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Conflicto de concurrencia al reservar turno {TurnId} para paciente {Dni}",
                request.AvailabilitySlotId, request.Patient.Dni);
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONCURRENCY),
                ErrorCodes.APPOINTMENT_CONCURRENCY);
        }

        _logger.LogInformation(
            "Turno reservado: TurnId={TurnId}, Paciente={Dni}, Fecha={Fecha}",
            turn.Id, request.Patient.Dni, turn.ScheduledDate);

        return ToResponse(appointment, patient);
    }

    public async Task<IEnumerable<AppointmentModel.Response>> GetByPatient(string dni, string? callerUserId, bool isAdmin)
    {
        if (!isAdmin)
        {
            var caller = await _persistence.First<Patient>(p => p.UserId == callerUserId)
                ?? throw new AuthorizationException();

            if (caller.Dni != dni)
                throw new AuthorizationException();
        }

        var patient = await _persistence.First<Patient>(p => p.Dni == dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var dates = await _persistence.GetFiltered<Date>(
        d => d.PatientId == patient.Id && d.Status == DateState.BOOKED);

        return (dates ?? Enumerable.Empty<Date>())
            .Select(d => ToResponse(d, patient));
    }

    public async Task<Pagination<AppointmentModel.SearchResponse>> Search(int pageSize, int pageIndex, string? patientDni, Guid? doctorId, Guid? specialtyId, DateTime? date)
    {
        if (!string.IsNullOrWhiteSpace(patientDni) && !patientDni.IsDniValid())
        {
            throw new ValidationException(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND,nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));
        }

        string[] includes =
        {
            "Patient",
            "Turn.Availability.Doctor.Specialty"
        };

        var page = await _persistence.Paginate<Date, DateTime>(
        pageSize, pageIndex,
        d => (string.IsNullOrWhiteSpace(patientDni) || (d.Patient != null && d.Patient.Dni == patientDni))
          && (!doctorId.HasValue || (d.Turn != null && d.Turn.Availability != null && d.Turn.Availability.DoctorId == doctorId.Value))
          && (!specialtyId.HasValue || (d.Turn != null && d.Turn.Availability != null && d.Turn.Availability.Doctor != null && d.Turn.Availability.Doctor.SpecialityId == specialtyId.Value))
          && (!date.HasValue || (d.Turn != null && d.Turn.ScheduledDate.Date == date.Value.Date)),
        d => d.AppointmentDate,
        includes);

        return page.Map(d => new AppointmentModel.SearchResponse(
            d.Id, d.AppointmentDate, d.Status, d.Motive,
            d.Turn?.StartTime ?? TimeSpan.Zero, d.Turn?.EndTime ?? TimeSpan.Zero,
            new AppointmentModel.PatientSearchResponse(d.Patient?.Dni ?? "Sin DNI", d.Patient?.Name ?? "Sin Nombre"),
            new AppointmentModel.DoctorSearchResponse(
                d.Turn?.Availability?.DoctorId ?? Guid.Empty,
                d.Turn?.Availability?.Doctor?.Name ?? "Sin Nombre",
                new AppointmentModel.SpecialtySearchResponse(
                    d.Turn?.Availability?.Doctor?.SpecialityId ?? Guid.Empty,
                    d.Turn?.Availability?.Doctor?.Speciality?.Name ?? "Sin Especialidad"))));

    }

    public async Task Cancel(Guid id)
    {
        var appointment = await _persistence.GetById<Date>(id)
            ?? throw new EntityNotFoundException(nameof(Date));

        if (appointment.Status != DateState.BOOKED)
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_ALREADY_CANCELLED),
                ErrorCodes.APPOINTMENT_ALREADY_CANCELLED);

        appointment.Cancel(DateTime.UtcNow);
        await _persistence.Update(appointment);

        if (appointment.Turn is not null)
        {
            appointment.Turn.Release();
            await _persistence.Update(appointment.Turn);
        }

        _logger.LogInformation(
            "Turno cancelado: AppointmentId={Id}", id);
    }

    private static AppointmentModel.Response ToResponse(Date date, Patient patient) =>
        new(
            date.Id,
            date.AppointmentDate,
            date.Status,
            date.CancellationDate,
            date.Motive,
            date.TurnId ?? Guid.Empty,
            date.PatientId ?? Guid.Empty,
            patient.Dni,
            date.Turn?.StartTime ?? TimeSpan.Zero,
            date.Turn?.EndTime ?? TimeSpan.Zero);
}
