using Dsw2026Tpi.Api.Settings;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations
{
    public static class RateLimitingConfigurationExtentions
    {
        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var rateLimitingSettings = new RateLimitingSettings();
            configuration.GetSection("RateLimiting").Bind(rateLimitingSettings);
            var windowTime = TimeSpan.FromMinutes(rateLimitingSettings.WindowMinutes);

            services.AddRateLimiter(options =>
            {
                // Política 1: Login Admin
                options.AddFixedWindowLimiter("AdminLoginPolicy", opt =>
                {
                    opt.PermitLimit = rateLimitingSettings.AdminLoginLimit;
                    opt.Window = windowTime;
                    opt.QueueLimit = 0;
                });

                // Política 2: Login Paciente
                options.AddFixedWindowLimiter("PatientLoginPolicy", opt =>
                {
                    opt.PermitLimit = rateLimitingSettings.PatientLoginLimit;
                    opt.Window = windowTime;
                    opt.QueueLimit = 0;
                });

                // Política 3: Reservas (Por Paciente Autenticado)
                options.AddPolicy("BookingPolicy", context =>
                {
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? context.Connection.RemoteIpAddress?.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingSettings.BookingLimit,
                            Window = windowTime,
                            QueueLimit = 0
                        });
                });

                // Política 4: Global
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var identity = context.User.Identity?.IsAuthenticated == true
                        ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        : context.Connection.RemoteIpAddress?.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(identity ?? "anonymous", _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingSettings.GlobalLimit,
                            Window = windowTime,
                            QueueLimit = 0
                        });
                });

                // Manejador global para rechazos
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    // Logging de forma limpia en métodos de extensión
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("RateLimiting");

                    logger.LogWarning("Rate limit superado. IP/User: {IpUser}",
                        context.HttpContext.Connection.RemoteIpAddress);

                    var errorResponse = new
                    {
                        errorCode = "RATE_LIMIT_EXCEEDED",
                        message = "Se ha superado el límite de solicitudes permitidas. Intente nuevamente más tarde."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, token);
                };
            });

            return services;
        }
    }
}
