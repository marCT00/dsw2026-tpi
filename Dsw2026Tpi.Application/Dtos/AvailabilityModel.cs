using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AvailabilityModel
{
    public record DayRuleRequest(string DayOfWeek, string StartTime, string EndTime);

    public record Request(Guid DoctorId, int Month, IEnumerable<DayRuleRequest> Days);

    public record Response(Guid Id, int Year, int Month, DayOfWeek DayOfWeek,
        TimeSpan StartTime, TimeSpan EndTime, Guid DoctorId);

    public record DoctorAvailabilityResponse(Guid Id, DateTime Day, TimeSpan Start, TimeSpan End, Guid DoctorId);
}
