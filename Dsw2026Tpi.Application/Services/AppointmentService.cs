using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IPersistence persistence, ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<AppointmentModel.Response> Create(AppointmentModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Motive) || request.Motive.Length < 3 || request.Motive.Length > 500)
            throw new ValidationException(
                ErrorCodes.APPOINTMENT_INVALID_MOTIVE,
                nameof(ErrorCodes.APPOINTMENT_INVALID_MOTIVE));

        if (request.Dni.Length < 7 || request.Dni.Length > 10 || !request.Dni.All(char.IsDigit))
            throw new ValidationException(
                ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND,
                nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));

        var patient = await _persistence.First<Patient>(p => p.Dni == request.Dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var turn = await _persistence.GetById<Turn>(request.TurnId)
            ?? throw new EntityNotFoundException(nameof(Turn));

        if (turn.ScheduledDate.Date < DateTime.UtcNow.Date)
            throw new ValidationException(
                ErrorCodes.APPOINTMENT_PAST_DATE,
                nameof(ErrorCodes.APPOINTMENT_PAST_DATE));

        if (turn.State != TurnState.AVAILABLE)
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE),
                ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE);

        var appointment = new Date(turn.ScheduledDate, patient, turn, request.Motive);

        try
        {
            turn.Reserve(appointment);
            await _persistence.Add(appointment);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Conflicto de concurrencia al reservar turno {TurnId} para paciente {Dni}",
                request.TurnId, request.Dni);
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONCURRENCY),
                ErrorCodes.APPOINTMENT_CONCURRENCY);
        }

        _logger.LogInformation(
            "Turno reservado: TurnId={TurnId}, Paciente={Dni}, Fecha={Fecha}",
            turn.Id, request.Dni, turn.ScheduledDate);

        return ToResponse(appointment, patient);
    }

    public async Task<IEnumerable<AppointmentModel.Response>> GetByPatient(string dni)
    {
        if (dni.Length < 7 || dni.Length > 10 || !dni.All(char.IsDigit))
            throw new ValidationException(
                ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND,
                nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));

        var patient = await _persistence.First<Patient>(p => p.Dni == dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var dates = await _persistence.GetFiltered<Date>(
            d => d.PatientId == patient.Id);

        return (dates ?? Enumerable.Empty<Date>())
            .Select(d => ToResponse(d, patient));
    }

    public async Task Cancel(Guid id)
    {
        var appointment = await _persistence.GetById<Date>(id)
            ?? throw new EntityNotFoundException(nameof(Date));

        if (appointment.Status != DateState.BOOKED)
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_ALREADY_CANCELLED),
                ErrorCodes.APPOINTMENT_ALREADY_CANCELLED);

        appointment.Cancel(DateTime.UtcNow);
        await _persistence.Update(appointment);

        if (appointment.Turn is not null)
        {
            appointment.Turn.Release();
            await _persistence.Update(appointment.Turn);
        }

        _logger.LogInformation(
            "Turno cancelado: AppointmentId={Id}", id);
    }

    private static AppointmentModel.Response ToResponse(Date date, Patient patient) =>
        new(
            date.Id,
            date.AppointmentDate,
            date.Status,
            date.CancellationDate,
            date.Motive,
            date.TurnId ?? Guid.Empty,
            date.PatientId ?? Guid.Empty,
            patient.Dni,
            date.Turn?.StartTime ?? TimeSpan.Zero,
            date.Turn?.EndTime ?? TimeSpan.Zero);
}
