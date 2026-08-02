using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AppointmentModel
{
    public record Request(string Dni, Guid TurnId, string Motive);

    public record Response(
        Guid Id,
        DateTime AppointmentDate,
        DateState Status,
        DateTime? CancellationDate,
        string Motive,
        Guid TurnId,
        Guid PatientId,
        string? PatientDni,
        TimeSpan StartTime,
        TimeSpan EndTime);
}
