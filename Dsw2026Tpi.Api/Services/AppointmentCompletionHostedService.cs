using Dsw2026Tpi.Application.Interfaces;

namespace Dsw2026Tpi.Api.Services;

public class AppointmentCompletionHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentCompletionHostedService> _logger;

    public AppointmentCompletionHostedService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentCompletionHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessExpiredAppointmentsAsync();

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessExpiredAppointmentsAsync();
        }
    }

    private async Task ProcessExpiredAppointmentsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
            await appointmentService.CompleteExpiredAppointments();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al completar automáticamente los turnos vencidos");
        }
    }
}
