using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Models;

namespace IncidenciasApp.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await context.Database.EnsureCreatedAsync();

        // Seed Supervisor User
        var supervisorEmail = "supervisor@bicis.com";
        var existingUser = await userManager.FindByEmailAsync(supervisorEmail);
        if (existingUser == null)
        {
            var user = new IdentityUser
            {
                UserName = supervisorEmail,
                Email = supervisorEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Supervisor123!");
        }

        // Seed Incidencias
        if (!await context.Incidencias.AnyAsync())
        {
            var incidencias = new List<Incidencia>
            {
                new Incidencia
                {
                    Estacion = "Estacion Central",
                    Descripcion = "Frenos defectuosos en bicicleta #104",
                    Prioridad = "Alta",
                    Estado = "Abierta",
                    FechaReporte = DateTime.UtcNow.AddHours(-5)
                },
                new Incidencia
                {
                    Estacion = "Miraflores Larco",
                    Descripcion = "Cadena rota en estacion de anclaje 3",
                    Prioridad = "Media",
                    Estado = "Abierta",
                    FechaReporte = DateTime.UtcNow.AddHours(-4)
                },
                new Incidencia
                {
                    Estacion = "San Isidro Golf",
                    Descripcion = "Pantalla tactil no responde al escanear QR",
                    Prioridad = "Baja",
                    Estado = "Abierta",
                    FechaReporte = DateTime.UtcNow.AddHours(-3)
                },
                new Incidencia
                {
                    Estacion = "Surco Benavides",
                    Descripcion = "Neumatico desinflado en bicicleta #88",
                    Prioridad = "Alta",
                    Estado = "Abierta",
                    FechaReporte = DateTime.UtcNow.AddHours(-2)
                },
                new Incidencia
                {
                    Estacion = "Barranco Pedro de Osma",
                    Descripcion = "Pedal desajustado y manillar flojo",
                    Prioridad = "Media",
                    Estado = "Abierta",
                    FechaReporte = DateTime.UtcNow.AddHours(-1)
                },
                new Incidencia
                {
                    Estacion = "San Borja Real Plaza",
                    Descripcion = "Perdida de senal GPS en estacion",
                    Prioridad = "Baja",
                    Estado = "Cerrada",
                    FechaReporte = DateTime.UtcNow.AddMinutes(-30)
                }
            };

            await context.Incidencias.AddRangeAsync(incidencias);
            await context.SaveChangesAsync();
        }
    }
}
