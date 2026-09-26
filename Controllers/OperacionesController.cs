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
    private readonly IAlgoliaSearchService _algoliaSearchService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IRedisCacheService cacheService,
        IAlgoliaSearchService algoliaSearchService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _algoliaSearchService = algoliaSearchService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias?q=frenos
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Query = q ?? string.Empty;

        // Si hay búsqueda de texto con Algolia: consultar directamente sin usar caché de Redis
        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("Consulta directa a Algolia con query '{Query}' (sin pasar por caché Redis).", q);
            var matchIds = await _algoliaSearchService.BuscarIdsIncidenciasAsync(q);

            var resultados = await _context.Incidencias
                .Where(i => matchIds.Contains(i.Id) && i.Estado == "Abierta")
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();

            ViewBag.FuenteDatos = "Búsqueda Algolia (Directa sin Caché)";
            return View(resultados);
        }

        // Listado general (búsqueda vacía): cachear por 60 segundos con Redis
        var cachedIncidencias = await _cacheService.GetIncidenciasAbiertasCacheAsync();
        if (cachedIncidencias != null)
        {
            _logger.LogInformation("[REDIS HIT] Listado general obtenido de Redis Cache: {Count} incidencias.", cachedIncidencias.Count);
            ViewBag.FuenteDatos = "Redis Cache (Rápida - 60s)";
            return View(cachedIncidencias);
        }

        // Cache Miss: consultar de SQLite y guardar en Redis por 60 segundos
        _logger.LogInformation("[DATABASE HIT / CACHE MISS] Listado general consultado de SQLite.");
        var incidencias = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync();

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
