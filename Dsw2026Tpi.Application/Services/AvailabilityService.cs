using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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

        ValidateMonthYear(request.Month, request.Year);
        var days = request.Days.ToList();
        ValidateDayRules(days);
        ValidateInternalOverlap(days);
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

        ValidateMonthYear(request.Month, request.Year);
        var days = request.Days.ToList();
        ValidateDayRules(days);
        ValidateInternalOverlap(days);

        var existingTurns = await _persistence.GetFiltered<Turn>(
            t => t.Availability != null
                && t.Availability.DoctorId == request.DoctorId
                && t.Availability.Year == request.Year
                && t.Availability.Month == request.Month
                && t.State != TurnState.AVAILABLE);

        if (existingTurns is not null && existingTurns.Any())
            throw new ConflictException(
                nameof(ErrorCodes.AVAILABILITY_MONTH_HAS_BOOKINGS),
                ErrorCodes.AVAILABILITY_MONTH_HAS_BOOKINGS);

        var existingAvailabilities = await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == request.DoctorId
                && a.Year == request.Year
                && a.Month == request.Month);

        if (existingAvailabilities is not null)
        {
            foreach (var existing in existingAvailabilities)
            {
                var turns = await _persistence.GetFiltered<Turn>(
                    t => t.AvailabilityId == existing.Id);
                if (turns is not null)
                {
                    foreach (var turn in turns)
                        await _persistence.Delete(turn);
                }
                await _persistence.Delete(existing);
            }
        }

        ValidateDbOverlap(request.DoctorId, request.Year, request.Month, days, null)
            .GetAwaiter().GetResult();

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

    public async Task<IEnumerable<AvailabilityModel.SlotResponse>> GetSlotsByDoctor(
        Guid doctorId, int? year = null, int? month = null)
    {
        var doctor = await _persistence.GetById<Doctor>(doctorId)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        var turns = await _persistence.GetFiltered<Turn>(
            t => t.Availability != null
                && t.Availability.DoctorId == doctorId
                && (!year.HasValue || t.Availability.Year == year.Value)
                && (!month.HasValue || t.Availability.Month == month.Value));

        return (turns ?? Enumerable.Empty<Turn>())
            .Select(t => new AvailabilityModel.SlotResponse(
                t.Id, t.ScheduledDate, t.StartTime, t.EndTime, t.State, t.AvailabilityId ?? Guid.Empty));
    }

    private async Task GenerateSlots(Availability availability, int year, int month)
    {
        var today = DateTime.UtcNow.Date;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            if (date.DayOfWeek != availability.DayOfWeek || date < today)
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

    private static void ValidateMonthYear(int month, int year)
    {
        if (month < 1 || month > 12)
            throw new ValidationException(
                ErrorCodes.AVAILABILITY_INVALID_MONTH,
                nameof(ErrorCodes.AVAILABILITY_INVALID_MONTH));

        if (year < DateTime.UtcNow.Year)
            throw new ValidationException(
                ErrorCodes.AVAILABILITY_INVALID_YEAR,
                nameof(ErrorCodes.AVAILABILITY_INVALID_YEAR));
    }

    private static void ValidateDayRules(List<AvailabilityModel.DayRuleRequest> days)
    {
        if (days.Count == 0)
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                "Se debe proporcionar al menos una regla de día");

        foreach (var day in days)
        {
            if (day.StartTime >= day.EndTime)
                throw new ValidationException(
                    ErrorCodes.AVAILABILITY_INVALID_RANGE,
                    nameof(ErrorCodes.AVAILABILITY_INVALID_RANGE));

            if (!IsAlignedToGrid(day.StartTime) || !IsAlignedToGrid(day.EndTime))
                throw new ValidationException(
                    ErrorCodes.AVAILABILITY_INVALID_DURATION,
                    nameof(ErrorCodes.AVAILABILITY_INVALID_DURATION));
        }
    }

    private static bool IsAlignedToGrid(TimeSpan time) =>
        time.Minutes % SlotDurationMinutes == 0 && time.Seconds == 0;

    private static void ValidateInternalOverlap(List<AvailabilityModel.DayRuleRequest> days)
    {
        var grouped = days.GroupBy(d => d.DayOfWeek);
        foreach (var group in grouped)
        {
            var sorted = group.OrderBy(d => d.StartTime).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].StartTime < sorted[i + 1].EndTime &&
                    sorted[i + 1].StartTime < sorted[i].EndTime)
                {
                    throw new ConflictException(
                        nameof(ErrorCodes.AVAILABILITY_OVERLAP),
                        ErrorCodes.AVAILABILITY_OVERLAP);
                }
            }
        }
    }

    private async Task ValidateDbOverlap(
        Guid doctorId, int year, int month,
        List<AvailabilityModel.DayRuleRequest> newDays,
        Guid? excludeAvailabilityId)
    {
        var existingRules = await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == doctorId
                && a.Year == year
                && a.Month == month
                && (!excludeAvailabilityId.HasValue || a.Id != excludeAvailabilityId.Value));

        if (existingRules is null)
            return;

        foreach (var newDay in newDays)
        {
            var overlapping = existingRules
                .Where(e => e.DayOfWeek == newDay.DayOfWeek
                    && e.StartTime < newDay.EndTime
                    && newDay.StartTime < e.EndTime);

            if (overlapping.Any())
                throw new ConflictException(
                    nameof(ErrorCodes.AVAILABILITY_OVERLAP),
                    ErrorCodes.AVAILABILITY_OVERLAP);
        }
    }
}
