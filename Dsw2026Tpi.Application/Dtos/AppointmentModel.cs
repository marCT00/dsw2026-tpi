using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Dtos;

public static class AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Reason);
    public record PatientRequest(string Dni);
    public record UpdateStatusRequest(DateState Status);

    public record Response(
        Guid Id,
        String AppointmentDate,
        DateState Status,
        DateTime? CancellationDate,
        string Reason,
        Guid TurnId,
        Guid PatientId,
        string? PatientDni,
        TimeSpan StartTime,
        TimeSpan EndTime,
        DoctorSearchResponse? Doctor = null);

    public record SearchResponse(
        Guid Id,
        String AppointmentDate,
        DateState Status,
        string Reason,
        TimeSpan StartTime,
        TimeSpan EndTime,
        PatientSearchResponse Patient,
        DoctorSearchResponse Doctor);


    public record DoctorSearchResponse(Guid DoctorId, string Name, SpecialitySearchResponse Speciality);
    public record SpecialitySearchResponse(Guid SpecialityId, string Name);
    public record PatientSearchResponse(string Dni, string Name);
}
