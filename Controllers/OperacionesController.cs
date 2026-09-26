using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Data;
using IncidenciasApp.Services;

namespace IncidenciasApp.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAlgoliaSearchService _algoliaSearchService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context, 
        IAlgoliaSearchService algoliaSearchService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _algoliaSearchService = algoliaSearchService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias?q=frenos
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Query = q ?? string.Empty;

        if (string.IsNullOrWhiteSpace(q))
        {
            // Con búsqueda vacía, presentar la lista habitual
            var todasAbiertas = await _context.Incidencias
                .Where(i => i.Estado == "Abierta")
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();

            return View(todasAbiertas);
        }

        // Búsqueda con Algolia en servidor
        var matchIds = await _algoliaSearchService.BuscarIdsIncidenciasAsync(q);

        // El servidor consulta Algolia y muestra solo incidencias abiertas existentes en la base
        var resultados = await _context.Incidencias
            .Where(i => matchIds.Contains(i.Id) && i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync();

        return View(resultados);
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
        }

        return RedirectToAction(nameof(Incidencias));
    }
}
