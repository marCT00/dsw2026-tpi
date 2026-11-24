namespace Dsw2026Tpi.Application.Dtos;

public static class DashboardModel
{
    public record StatsResponse(
        int ActiveDoctors,
        int DoctorsAddedThisMonth,
        int ActiveSpecialities,
        int TodayConfirmedAppointments);
}
