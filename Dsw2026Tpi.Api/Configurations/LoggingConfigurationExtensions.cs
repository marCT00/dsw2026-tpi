using Serilog;

namespace Dsw2026Tpi.Api.Configurations;

public static class LoggingConfigurationExtensions
{
    public static void AddSerilogConfiguration(this WebApplicationBuilder builder)
    {
        // Configurar Serilog desde appsettings.json
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        // Reemplazar el logger por defecto con Serilog
        builder.Host.UseSerilog();
    }

    public static void UseSerilogRequestLogging(this WebApplication app)
    {
        // Middleware para logging de requests HTTP
        app.UseSerilogRequestLogging();
    }
}
