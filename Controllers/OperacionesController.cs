using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Data;
using IncidenciasApp.Models;
using IncidenciasApp.Services;

namespace IncidenciasApp.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IRedisCacheService cacheService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias()
    {
        // Intentar leer de Redis Cache (60 segundos)
        var cachedIncidencias = await _cacheService.GetIncidenciasAbiertasCacheAsync();
        if (cachedIncidencias != null)
        {
            _logger.LogInformation("Lectura exitosa desde REDIS: {Count} incidencias abiertas.", cachedIncidencias.Count);
            ViewBag.FuenteDatos = "Redis Cache (Rápida - 60s)";
            return View(cachedIncidencias);
        }

        // Si no está en caché (Cache Miss), consultar la Base de Datos SQLite
        _logger.LogInformation("Lectura desde BASE DE DATOS (SQLite) para incidencias abiertas.");
        var incidencias = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync();

        // Guardar en Redis por 60 segundos
        await _cacheService.SetIncidenciasAbiertasCacheAsync(incidencias, TimeSpan.FromSeconds(60));
        ViewBag.FuenteDatos = "Base de Datos SQLite (Guardada en Caché)";

        return View(incidencias);
    }

    // POST: /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);
        if (incidencia != null && incidencia.Estado == "Abierta")
        {
            incidencia.Estado = "Cerrada";
            await _context.SaveChangesAsync();
            _logger.LogInformation("Incidencia {Id} cerrada en la base de datos.", id);

            // Al cerrar una incidencia, invalidar la clave del listado antes de volver a consultarlo
            await _cacheService.InvalidateIncidenciasAbiertasCacheAsync();
        }

        return RedirectToAction(nameof(Incidencias));
    }
}
