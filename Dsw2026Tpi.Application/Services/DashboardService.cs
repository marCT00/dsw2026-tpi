using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IPersistence _persistence;
    private readonly IAppointmentService _appointmentService;

    public DashboardService(IPersistence persistence, IAppointmentService appointmentService)
    {
        _persistence = persistence;
        _appointmentService = appointmentService;
    }

    public async Task<DashboardModel.StatsResponse> GetStats()
    {
        await _appointmentService.CompleteExpiredAppointments();

        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var activeDoctors = await _persistence.Count<Doctor>(d => !d.Deleted);
        var doctorsAddedThisMonth = await _persistence.Count<Doctor>(
            d => !d.Deleted && d.CreatedAt >= monthStart && d.CreatedAt < nextMonthStart);
        var activeSpecialities = await _persistence.Count<Speciality>(s => !s.Deleted);
        var todayConfirmedAppointments = await _persistence.Count<Date>(
            d => d.Status == DateState.BOOKED && d.AppointmentDate >= today && d.AppointmentDate < tomorrow);

        return new DashboardModel.StatsResponse(
            activeDoctors,
            doctorsAddedThisMonth,
            activeSpecialities,
            todayConfirmedAppointments);
    }
}
