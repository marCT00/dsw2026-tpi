using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AvailabilityModel
{
    public record DayRuleRequest(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime);

    public record Request(Guid DoctorId, int Year, int Month, IEnumerable<DayRuleRequest> Days);

    public record Response(Guid Id, int Year, int Month, DayOfWeek DayOfWeek,
        TimeSpan StartTime, TimeSpan EndTime, Guid DoctorId);

    public record SlotResponse(Guid Id, DateTime ScheduledDate, TimeSpan StartTime,
        TimeSpan EndTime, TurnState State, Guid AvailabilityId); //M: esto no se usa, pero lo dejo por si acaso

    public record DoctorAvailabilityResponse(Guid Id, string Day, string StartTime, string EndTime);
}
