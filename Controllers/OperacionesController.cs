using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Data;
using IncidenciasApp.Services;

namespace IncidenciasApp.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPieHostWebSocketService _webSocketService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IPieHostWebSocketService webSocketService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _webSocketService = webSocketService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias()
    {
        var incidencias = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync();

        return View(incidencias);
    }

    // GET: /Operaciones/EstadoVigente (para consultar estado vigente al reconectar WebSocket)
    [HttpGet]
    public async Task<IActionResult> EstadoVigente()
    {
        var vigentes = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync();

        return Json(vigentes);
    }

    // POST: /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);
        if (incidencia != null && incidencia.Estado == "Abierta")
        {
            // 1. Guardar primero el estado en la base de datos
            incidencia.Estado = "Cerrada";
            await _context.SaveChangesAsync();
            _logger.LogInformation("Incidencia {Id} cerrada en la base de datos.", id);

            // 2. Publicar desde el servidor el evento IncidenciaActualizada con Id y Estado en PieHost
            await _webSocketService.PublicarIncidenciaActualizadaAsync(id, "Cerrada");
        }

        return RedirectToAction(nameof(Incidencias));
    }
}
