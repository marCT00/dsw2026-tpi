using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Api.Services;

public class AdminSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeederHostedService> _logger;

    public AdminSeederHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AdminSeederHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var adminSection = _configuration.GetSection("AdminSeed");
        var email = adminSection["Email"] ?? "admin@system.com";
        var password = adminSection["Password"] ?? throw new InvalidOperationException(
            "Falta configurar AdminSeed:Password en appsettings.json");

        if (!await roleManager.RoleExistsAsync(Roles.Administrator))
        {
            _logger.LogWarning("El rol {Role} no existe todavía, se omite el seeding del admin", Roles.Administrator);
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            _logger.LogInformation("El usuario admin ya existe: {Email}", email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            _logger.LogError("No se pudo crear el usuario admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Administrator);
        _logger.LogInformation("Usuario admin creado: {Email}", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}