using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex, d => string.IsNullOrWhiteSpace(name) ||
                                                   d.Name.Contains(name), x => x.Name, nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        ValidationsExtensions.ValidateStringLength(request.Name, 3, 100, ErrorCodes.DOCTOR_INVALID_NAME, nameof(ErrorCodes.DOCTOR_INVALID_NAME));
        var exists = await _persistence.First<Doctor>(d => d.LicenseNumber == request.LicenseNumber);
        if (exists is not null)
            throw new ConflictException(nameof(ErrorCodes.DOCTOR_LICENSE_NUMBER_CONFLICT), ErrorCodes.DOCTOR_LICENSE_NUMBER_CONFLICT);
        ValidationsExtensions.ValidateStringLength(request.LicenseNumber, 1, 50, ErrorCodes.DOCTOR_INVALID_LICENSE_NUMBER, nameof(ErrorCodes.DOCTOR_INVALID_LICENSE_NUMBER));

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);
        await _persistence.Add(doctor);

        return new DoctorModel.Response(doctor.Id, doctor.Name, doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));
    }

    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor)); 
        var duplicate = await _persistence.First<Doctor>(d => d.LicenseNumber == request.LicenseNumber && d.Id != id);
        if (duplicate is not null)
            throw new ConflictException(nameof(ErrorCodes.DOCTOR_LICENSE_NUMBER_CONFLICT), ErrorCodes.DOCTOR_LICENSE_NUMBER_CONFLICT);

        ValidationsExtensions.ValidateStringLength(request.Name, 3, 100, ErrorCodes.DOCTOR_INVALID_NAME, nameof(ErrorCodes.DOCTOR_INVALID_NAME));
        ValidationsExtensions.ValidateStringLength(request.LicenseNumber, 1, 50, ErrorCodes.DOCTOR_INVALID_LICENSE_NUMBER, nameof(ErrorCodes.DOCTOR_INVALID_LICENSE_NUMBER));

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        doctor.Update(request.Name, request.LicenseNumber, speciality);
        await _persistence.Update(doctor);

        return new DoctorModel.Response(doctor.Id, doctor.Name, doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));
    }

    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        doctor.Deactivate();
        await _persistence.Update(doctor);
    }
}
