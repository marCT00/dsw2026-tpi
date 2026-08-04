using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Reason);
    public record PatientRequest(string Dni);

    public record Response(
        Guid Id,
        DateTime AppointmentDate,
        DateState Status,
        DateTime? CancellationDate,
        string Reason,
        Guid TurnId,
        Guid PatientId,
        string? PatientDni,
        TimeSpan StartTime,
        TimeSpan EndTime);

    public record SearchResponse(
        Guid Id,
        DateTime AppointmentDate,
        DateState Status,
        string Reason,
        TimeSpan StartTime,
        TimeSpan EndTime,
        PatientSearchResponse Patient,
        DoctorSearchResponse Doctor);

    public record DoctorSearchResponse(Guid DoctorId, string Name, SpecialtySearchResponse Specialty);
    public record SpecialtySearchResponse(Guid SpecialtyId, string Name);
    public record PatientSearchResponse(string Dni, string Name);
}
