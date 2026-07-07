using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;

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

    public static async Task<IHost> LoadUserData(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;

        try
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            string adminRole = "Admin";
            string userRole = "User";

            // Crear roles si no existen
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
                Log.Information($"Rol '{adminRole}' creado.");
            }
            if (!await roleManager.RoleExistsAsync(userRole))
            {
                await roleManager.CreateAsync(new IdentityRole(userRole));
                Log.Information($"Rol '{userRole}' creado.");
            }

            string jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Sources", "users.json");
            if (!File.Exists(jsonFilePath))
            {
                Log.Error($" Archivo 'admins.json' no encontrado en: {jsonFilePath}");
                return host;
            }

            string json = await File.ReadAllTextAsync(jsonFilePath);
            var adminsToSeed = JsonSerializer.Deserialize<List<UserDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (adminsToSeed == null) return host;
            foreach (var adminData in adminsToSeed)
            {
                var adminUser = await userManager.FindByNameAsync(adminData.Username);
                if (adminUser == null)
                {
                    adminUser = new IdentityUser
                    {
                        UserName = adminData.Username,
                        Email = adminData.Email,
                        EmailConfirmed = true
                    };
                    var createResult = await userManager.CreateAsync(adminUser, adminData.Password);

                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        Log.Information($"Admin '{adminData.Username}' creado con rol '{adminRole}'.");
                    }
                    else
                    {
                        Log.Error($" Error al crear admin '{adminData.Username}':");
                        foreach (var error in createResult.Errors)
                        {
                            Log.Error($"- {error.Description}");
                        }
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        Log.Information($"Rol '{adminRole}' asignado a usuario existente '{adminUser.UserName}'.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error durante la carga de administradores.");
        }
        return host;
    }
}
