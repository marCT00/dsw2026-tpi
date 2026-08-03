using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IEnumerable<AvailabilityModel.Response>> Create(AvailabilityModel.Request request);
    Task<IEnumerable<AvailabilityModel.Response>> Update(AvailabilityModel.Request request);
    Task<IEnumerable<AvailabilityModel.DoctorAvailabilityResponse>> GetSlotsByDoctor(Guid doctorId,int? year = null, int? month = null);
}
