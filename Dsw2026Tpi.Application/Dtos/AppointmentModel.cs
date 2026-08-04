using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Motive);
    public record PatientRequest(string Dni);

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

    public record SearchResponse(
        Guid Id,
        DateTime AppointmentDate,
        DateState Status,
        string Motive,
        TimeSpan StartTime,
        TimeSpan EndTime,
        PatientSearchResponse Patient,
        DoctorSearchResponse Doctor);

    public record PatientSearchResponse(string Dni, string Name);
    public record DoctorSearchResponse(string Name, string Specialty);
}
