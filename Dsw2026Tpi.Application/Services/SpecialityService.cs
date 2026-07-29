using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialityService : ISpecialityService
{
    private readonly IPersistence _persistence;

    public SpecialityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var specialities = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => s.IsActive && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)),
            s => s.Name);

        return specialities.Map(ToResponse);
    }

    public async Task<SpecialityModel.Response> GetById(Guid id)
    {
        var speciality = await _persistence.GetById<Speciality>(id)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        return ToResponse(speciality);
    }

    public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        Validate(request);

        var exists = await _persistence.First<Speciality>(s => s.Name == request.Name && s.IsActive);
        if (exists is not null)
            throw new ConflictException(nameof(ErrorCodes.SPECIALITY_NAME_CONFLICT), ErrorCodes.SPECIALITY_NAME_CONFLICT);

        var speciality = new Speciality(request.Name, request.Description);
        await _persistence.Add(speciality);

        return ToResponse(speciality);
    }

    public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
    {
        Validate(request);

        var speciality = await _persistence.GetById<Speciality>(id)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        var duplicate = await _persistence.First<Speciality>(s => s.Name == request.Name && s.IsActive && s.Id != id);
        if (duplicate is not null)
            throw new ConflictException(nameof(ErrorCodes.SPECIALITY_NAME_CONFLICT), ErrorCodes.SPECIALITY_NAME_CONFLICT);

        speciality.UpdateDetails(request.Name, request.Description);
        await _persistence.Update(speciality);

        return ToResponse(speciality);
    }

    public async Task Delete(Guid id)
    {
        var speciality = await _persistence.GetById<Speciality>(id)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        speciality.Deactivate();
        await _persistence.Update(speciality);
    }

    private static void Validate(SpecialityModel.Request request)
    {
        ValidationsExtensions.ValidateStringLength(
            request.Name, 3, 100,
            ErrorCodes.SPECIALITY_INVALID_NAME, nameof(ErrorCodes.SPECIALITY_INVALID_NAME));

        ValidationsExtensions.ValidateStringLength(
            request.Description, 10, 100,
            ErrorCodes.SPECIALITY_INVALID_DESCRIPTION, nameof(ErrorCodes.SPECIALITY_INVALID_DESCRIPTION));
    }

    private static SpecialityModel.Response ToResponse(Speciality speciality) =>
        new(speciality.Id, speciality.Name, speciality.Description, speciality.IsActive);
}