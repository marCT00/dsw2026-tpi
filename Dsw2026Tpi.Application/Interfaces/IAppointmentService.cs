using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request);
    Task<Pagination<AppointmentModel.SearchResponse>> Search(int pageSize, int pageIndex, string? patientDni, Guid? doctorId, Guid? SpecialityId, string? date);
    Task Cancel(Guid id, string? callerUserId, bool isAdmin);
    Task<IEnumerable<AppointmentModel.Response>> GetByPatient(string dni, string? callerUserId, bool isAdmin);
}
