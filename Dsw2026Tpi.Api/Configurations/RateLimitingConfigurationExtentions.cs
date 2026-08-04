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
                options.AddPolicy("AdminLoginPolicy", context => //login Admin
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingSettings.AdminLoginLimit,
                            Window = windowTime,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("PatientLoginPolicy", context =>   //login paciente
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingSettings.PatientLoginLimit,
                            Window = windowTime,
                            QueueLimit = 0
                        });
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

                    logger.LogWarning("Rate limit superado. IP/User: {IpUser}. Endpoint: {Endpoint}",
                        context.HttpContext.Connection.RemoteIpAddress, context.HttpContext.Request.Path);

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
