using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AvailabilityService> _logger;
    private const int SlotDurationMinutes = 30;

    public AvailabilityService(IPersistence persistence, ILogger<AvailabilityService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> Create(AvailabilityModel.Request request)
    {
        var doctor = await _persistence.GetById<Doctor>(request.DoctorId)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        ValidationsExtensions.ValidateMonthYear(request.Month, request.Year);
        var days = request.Days.ToList();

        var dayRulesBasic = days.Select(d => (d.StartTime, d.EndTime));
        ValidationsExtensions.ValidateDayRules(dayRulesBasic, SlotDurationMinutes);

        var dayRulesForOverlap = days.Select(d => (d.DayOfWeek, d.StartTime, d.EndTime));
        ValidationsExtensions.ValidateInternalOverlap(dayRulesForOverlap);


        await ValidateDbOverlap(request.DoctorId, request.Year, request.Month, days, null);

        var created = new List<AvailabilityModel.Response>();
        foreach (var day in days)
        {
            var availability = new Availability(
                request.Month, request.Year, day.DayOfWeek,
                day.StartTime, day.EndTime, doctor);

            await _persistence.Add(availability);
            await GenerateSlots(availability, request.Year, request.Month);
            created.Add(new AvailabilityModel.Response(
                availability.Id, request.Year, request.Month,
                day.DayOfWeek, day.StartTime, day.EndTime, request.DoctorId));
        }

        _logger.LogInformation(
            "Disponibilidades creadas para médico {DoctorId}, {Year}/{Month} — {Count} reglas",
            request.DoctorId, request.Year, request.Month, days.Count);

        return created;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> Update(AvailabilityModel.Request request)
    {
        var doctor = await _persistence.GetById<Doctor>(request.DoctorId)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        ValidationsExtensions.ValidateMonthYear(request.Month, request.Year);
        var days = request.Days.ToList();

        var dayRulesBasic = days.Select(d => (d.StartTime, d.EndTime));
        ValidationsExtensions.ValidateDayRules(dayRulesBasic, SlotDurationMinutes);

        var dayRulesForOverlap = days.Select(d => (d.DayOfWeek, d.StartTime, d.EndTime));
        ValidationsExtensions.ValidateInternalOverlap(dayRulesForOverlap);

        var existingAvailabilities = await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == request.DoctorId
                && a.Year == request.Year
                && a.Month == request.Month);

        if (existingAvailabilities is not null && existingAvailabilities.Any())
        {

            var existingIds = existingAvailabilities.Select(a => a.Id).ToList();

            var existingTurns = await _persistence.GetFiltered<Turn>(
                t => t.AvailabilityId != null && existingIds.Contains(t.AvailabilityId.Value));

            if (existingTurns is not null && existingTurns.Any(t => t.State != TurnState.AVAILABLE))
                throw new ConflictException(
                    nameof(ErrorCodes.AVAILABILITY_MONTH_HAS_BOOKINGS),
                    ErrorCodes.AVAILABILITY_MONTH_HAS_BOOKINGS);

            if (existingTurns is not null)
                foreach (var turn in existingTurns)
                    await _persistence.Delete(turn);

            foreach (var existing in existingAvailabilities)
                await _persistence.Delete(existing);
        }
        var created = new List<AvailabilityModel.Response>();
        foreach (var day in days)
        {
            var availability = new Availability(
                request.Month, request.Year, day.DayOfWeek,
                day.StartTime, day.EndTime, doctor);

            await _persistence.Add(availability);
            await GenerateSlots(availability, request.Year, request.Month);
            created.Add(new AvailabilityModel.Response(
                availability.Id, request.Year, request.Month,
                day.DayOfWeek, day.StartTime, day.EndTime, request.DoctorId));
        }

        _logger.LogInformation(
            "Disponibilidades actualizadas para médico {DoctorId}, {Year}/{Month} — {Count} reglas",
            request.DoctorId, request.Year, request.Month, days.Count);

        return created;
    }

    public async Task<IEnumerable<AvailabilityModel.DoctorAvailabilityResponse>> GetSlotsByDoctor(Guid doctorId, int? year = null, int? month = null)
    {
        var doctor = await _persistence.GetById<Doctor>(doctorId)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        var availabilities = await _persistence.GetFiltered<Availability>(
        a => a.DoctorId == doctorId
            && (!year.HasValue || a.Year == year.Value)
            && (!month.HasValue || a.Month == month.Value));

        if (availabilities is null || !availabilities.Any())
            return Enumerable.Empty<AvailabilityModel.DoctorAvailabilityResponse>();
        return availabilities.Select(a => new AvailabilityModel.DoctorAvailabilityResponse(
            a.Id,
            a.DayOfWeek.ToString().ToUpper(),
            a.StartTime.ToString(@"hh\:mm"), 
            a.EndTime.ToString(@"hh\:mm")
           ));
    }

    private async Task GenerateSlots(Availability availability, int year, int month)
    {
        var today = DateTime.UtcNow.Date;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        //feriados
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var jsonPath = Path.Combine(basePath, "Sources", "holidays.json");
        var holidays = new List<string>();

        if (File.Exists(jsonPath))
        {
            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);

            if (doc.RootElement.TryGetProperty("feriados", out var feriadosElement))
            {
                holidays = feriadosElement.Deserialize<List<string>>() ?? new List<string>();
            }
        } else {
            throw new FileNotFoundException($"El archivo de feriados no se encontró en la ruta: {jsonPath}");
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var dateString = date.ToString("yyyy-MM-dd");
            bool isHoliday = holidays.Contains(dateString);


            if (date.DayOfWeek != availability.DayOfWeek || date < today || isHoliday)
                continue;

            var current = availability.StartTime;
            while (current + TimeSpan.FromMinutes(SlotDurationMinutes) <= availability.EndTime)
            {
                var slotEnd = current + TimeSpan.FromMinutes(SlotDurationMinutes);
                var turn = new Turn(date, current, slotEnd, availability);
                await _persistence.Add(turn);
                current = slotEnd;
            }
        }
    }

    private async Task ValidateDbOverlap(Guid doctorId, int year, int month,List<AvailabilityModel.DayRuleRequest> newDays,Guid? excludeAvailabilityId)
    {
        var existingRules = await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == doctorId 
            && a.Year == year && a.Month == month 
            && (!excludeAvailabilityId.HasValue || a.Id != excludeAvailabilityId.Value));

        if (existingRules is null) return;
        foreach (var newDay in newDays)
        { 
            var overlapping = existingRules
                .Where(e => e.DayOfWeek == newDay.DayOfWeek 
                && e.StartTime < newDay.EndTime
                && newDay.StartTime < e.EndTime);

            if (overlapping.Any())
                throw new ConflictException(nameof(ErrorCodes.AVAILABILITY_OVERLAP),ErrorCodes.AVAILABILITY_OVERLAP);

        }

    }
}
