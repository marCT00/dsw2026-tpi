using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Dsw2026Tpi.Api.Configurations;

public static class SecurityConfigurationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener parámetros para creación del JWT desde appsettings.json
        var jwtConfig = configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var key = Encoding.UTF8.GetBytes(keyText);
        //Agregar autenticación
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                //Definir parámetros para la generación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener configuración para CORS desde appsettings.json
        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()?
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => origin.TrimEnd('/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

        //Si no se definió configuración en el archivo, utilizar la que se define
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost",
                "https://localhost"
            ];
        }

        //Agregar CORS con la política por defecto a partir de las URLs definidas
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true
            };

        }).AddEntityFrameworkStores<AuthenticationDbContext>()
          .AddDefaultTokenProviders();
        return services;
    }
}
