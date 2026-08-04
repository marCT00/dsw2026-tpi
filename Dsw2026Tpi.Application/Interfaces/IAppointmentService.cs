using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request);
    Task<IEnumerable<AppointmentModel.SearchResponse>> Search(string patientDni, Guid? doctorId);
    Task Cancel(Guid id);
    Task<IEnumerable<AppointmentModel.Response>> GetByPatient(string dni, string? callerUserId, bool isAdmin);
}
