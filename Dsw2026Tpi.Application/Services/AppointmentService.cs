using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

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
        ValidationsExtensions.ValidateStringLength(request.Reason, 5, 500, ErrorCodes.APPOINTMENT_INVALID_REASON, nameof(ErrorCodes.APPOINTMENT_INVALID_REASON));

        if (!request.Patient.Dni.IsDniValid())
            throw new ValidationException(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND, nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));

        var doctor = await _persistence.GetById<Doctor>(request.DoctorId)
        ?? throw new EntityNotFoundException(nameof(Doctor));

        var patient = await _persistence.First<Patient>(p => p.Dni == request.Patient.Dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var turn = await _persistence.GetById<Turn>(request.AvailabilitySlotId, "Availability.Doctor.Speciality")
            ?? throw new EntityNotFoundException(nameof(Turn));

        if (turn.Availability?.DoctorId != request.DoctorId)
            throw new ValidationException(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE, nameof(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE));

        if (turn.ScheduledDate.Date < DateTime.Now.Date)
            throw new ValidationException(ErrorCodes.APPOINTMENT_PAST_DATE, nameof(ErrorCodes.APPOINTMENT_PAST_DATE));

        if (turn.State != TurnState.AVAILABLE)
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE), ErrorCodes.APPOINTMENT_SLOT_UNAVAILABLE);

        var appointment = new Date(turn.ScheduledDate, patient, turn, request.Reason);

        try
        {
            turn.Reserve(appointment);
            await _persistence.Add(appointment);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Conflicto de concurrencia al reservar turno {TurnId} para paciente {Dni}",
                request.AvailabilitySlotId, request.Patient.Dni);
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONCURRENCY),
                ErrorCodes.APPOINTMENT_CONCURRENCY);
        }

        _logger.LogInformation(
            "Turno reservado: TurnId={TurnId}, Paciente={Dni}, Fecha={Fecha}",
            turn.Id, request.Patient.Dni, turn.ScheduledDate);

        return ToResponse(appointment, patient);
    }

    public async Task<IEnumerable<AppointmentModel.Response>> GetByPatient(string dni, string? callerUserId, bool isAdmin)
    {
        await CompleteExpiredAppointments();

        if (!isAdmin)
        {
            var caller = await _persistence.First<Patient>(p => p.UserId == callerUserId)
                ?? throw new AuthorizationException();

            if (caller.Dni != dni)
                throw new AuthorizationException();
        }

        var patient = await _persistence.First<Patient>(p => p.Dni == dni)
            ?? throw new EntityNotFoundException(nameof(Patient));

        var dates = await _persistence.GetFiltered<Date>(
        d => d.PatientId == patient.Id && d.Status == DateState.BOOKED, "Turn.Availability.Doctor.Speciality");

        return (dates ?? Enumerable.Empty<Date>())
            .Select(d => ToResponse(d, patient));
    }

    public async Task<Pagination<AppointmentModel.SearchResponse>> Search(int pageSize, int pageIndex, string? patientDni, Guid? doctorId, Guid? SpecialityId, string? date, DateState? status = null)
    {
        await CompleteExpiredAppointments();

        if (!string.IsNullOrWhiteSpace(patientDni) && !patientDni.IsDniValid())
        {
            throw new ValidationException(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND, nameof(ErrorCodes.APPOINTMENT_PATIENT_NOT_FOUND));
        }

        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var validDate))
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR));

            parsedDate = validDate.Date;
        }

        string[] includes =
        {
            "Patient",
            "Turn.Availability.Doctor.Speciality"
        };

        var page = await _persistence.Paginate<Date, DateTime>(
        pageSize, pageIndex,
        d => (string.IsNullOrWhiteSpace(patientDni) || (d.Patient != null && d.Patient.Dni == patientDni))
          && (!doctorId.HasValue || (d.Turn != null && d.Turn.Availability != null && d.Turn.Availability.DoctorId == doctorId.Value))
          && (!SpecialityId.HasValue || (d.Turn != null && d.Turn.Availability != null && d.Turn.Availability.Doctor != null && d.Turn.Availability.Doctor.SpecialityId == SpecialityId.Value))
          && (!parsedDate.HasValue || (d.Turn != null && d.Turn.ScheduledDate.Date == parsedDate.Value))
          && (!status.HasValue || d.Status == status.Value),
        d => d.AppointmentDate,
        includes);

        return page.Map(d => new AppointmentModel.SearchResponse(
            d.Id, 
            d.AppointmentDate.ToString("yyyy-MM-dd"), 
            d.Status, 
            d.Motive,
            d.Turn?.StartTime ?? TimeSpan.Zero, 
            d.Turn?.EndTime ?? TimeSpan.Zero,
            new AppointmentModel.PatientSearchResponse(
                d.Patient?.Dni ?? "Sin DNI", 
                d.Patient?.Name ?? "Sin Nombre"),
                new AppointmentModel.DoctorSearchResponse(
                    d.Turn?.Availability?.DoctorId ?? Guid.Empty,
                    d.Turn?.Availability?.Doctor?.Name ?? "Sin Nombre",
                    new AppointmentModel.SpecialitySearchResponse(
                        d.Turn?.Availability?.Doctor?.SpecialityId ?? Guid.Empty,
                        d.Turn?.Availability?.Doctor?.Speciality?.Name ?? "Sin Especialidad"))));

    }

    public async Task Cancel(Guid id, string? callerUserId, bool isAdmin)
    {
        await CompleteExpiredAppointments();

        var appointment = await _persistence.GetById<Date>(id, nameof(Date.Turn), nameof(Date.Patient))
            ?? throw new EntityNotFoundException(nameof(Date));

        if (!isAdmin && appointment.Patient?.UserId != callerUserId)
            throw new AuthorizationException();

        if (appointment.Status != DateState.BOOKED)
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_ALREADY_CANCELLED),
                ErrorCodes.APPOINTMENT_ALREADY_CANCELLED);

        appointment.Cancel(DateTime.Now);
        await _persistence.Update(appointment);

        _logger.LogInformation(
            "Turno cancelado: AppointmentId={Id}", id);
    }

    public async Task<AppointmentModel.Response> UpdateStatus(Guid id, AppointmentModel.UpdateStatusRequest request)
    {
        if (request is null || (request.Status != DateState.ATTENDED && request.Status != DateState.NO_SHOW))
        {
            throw new ValidationException(
                ErrorCodes.APPOINTMENT_INVALID_STATUS_TRANSITION,
                nameof(ErrorCodes.APPOINTMENT_INVALID_STATUS_TRANSITION));
        }

        var appointment = await _persistence.GetById<Date>(
                id,
                nameof(Date.Patient),
                "Turn.Availability.Doctor.Speciality")
            ?? throw new EntityNotFoundException(nameof(Date));

        if (appointment.Status != DateState.BOOKED)
        {
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_STATUS_CHANGE_NOT_ALLOWED),
                ErrorCodes.APPOINTMENT_STATUS_CHANGE_NOT_ALLOWED);
        }

        if (request.Status == DateState.ATTENDED)
        {
            appointment.Complete();
        }
        else
        {
            appointment.NoShow();
        }

        await _persistence.Update(appointment);

        _logger.LogInformation(
            "Estado de turno actualizado: AppointmentId={Id}, NewStatus={Status}",
            id, appointment.Status);

        return ToResponse(appointment, appointment.Patient);
    }

    public async Task<int> CompleteExpiredAppointments()
    {
        var now = DateTime.Now;
        var tomorrow = now.Date.AddDays(1);

        var candidates = await _persistence.GetFiltered<Date>(
            d => d.Status == DateState.BOOKED && d.AppointmentDate < tomorrow,
            nameof(Date.Turn));

        var expired = (candidates ?? Enumerable.Empty<Date>())
            .Where(d => HasAppointmentEnded(d, now))
            .ToList();

        if (expired.Count == 0)
            return 0;

        var completedCount = 0;
        foreach (var appointment in expired)
        {
            try
            {
                appointment.Complete();
                await _persistence.Update(appointment);
                completedCount++;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    "Conflicto de concurrencia al completar automáticamente el turno {AppointmentId}",
                    appointment.Id);
            }
        }

        if (completedCount > 0)
        {
            _logger.LogInformation(
                "Se marcaron automáticamente como ATTENDED {Count} turno(s) cuyo horario de finalización ya pasó",
                completedCount);
        }

        return completedCount;
    }

    private static bool HasAppointmentEnded(Date appointment, DateTime now)
    {
        if (appointment.Turn is not null)
        {
            var endDateTime = appointment.Turn.ScheduledDate.Date + appointment.Turn.EndTime;
            return endDateTime <= now;
        }

        return appointment.AppointmentDate.Date < now.Date;
    }

    private static AppointmentModel.Response ToResponse(Date date, Patient? patient)
    {
        var doctor = date.Turn?.Availability?.Doctor;
        var doctorDto = doctor is not null
            ? new AppointmentModel.DoctorSearchResponse(
                doctor.Id,
                doctor.Name,
                new AppointmentModel.SpecialitySearchResponse(
                    doctor.SpecialityId ?? Guid.Empty,
                    doctor.Speciality?.Name ?? "Sin Especialidad"))
            : null;

        return new(
            date.Id,
            date.AppointmentDate.ToString("yyyy-MM-dd"),
            date.Status,
            date.CancellationDate,
            date.Motive,
            date.TurnId ?? Guid.Empty,
            date.PatientId ?? Guid.Empty,
            patient?.Dni,
            date.Turn?.StartTime ?? TimeSpan.Zero,
            date.Turn?.EndTime ?? TimeSpan.Zero,
            doctorDto);
    }

    }
