using Microsoft.AspNetCore.Mvc;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.API.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly IEnumerable<IDataSeeder> _seeders;
    private readonly ILogger<SeedController> _logger;

    public SeedController(IEnumerable<IDataSeeder> seeders, ILogger<SeedController> logger)
    {
        _seeders = seeders;
        _logger = logger;
    }

    /// <summary>
    /// Popula datos de prueba en todos los módulos activos.
    /// Idempotente: llamarlo más de una vez no duplica datos.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SeedAll()
    {
        var resultados = new List<object>();

        // Ejecutar en orden (Clientes → Créditos → CajaDeAhorro)
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            try
            {
                await seeder.SeedAsync();
                _logger.LogInformation("Seed completado: {Modulo}", seeder.ModuleName);
                resultados.Add(new { modulo = seeder.ModuleName, estado = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sembrar datos del módulo {Modulo}", seeder.ModuleName);
                resultados.Add(new { modulo = seeder.ModuleName, estado = "error", detalle = ex.Message });
            }
        }

        return Ok(new
        {
            mensaje = "Seed ejecutado",
            modulos = resultados
        });
    }

    /// <summary>
    /// Informa qué seeders están registrados y su orden de ejecución.
    /// </summary>
    [HttpGet]
    public IActionResult Info()
    {
        var info = _seeders
            .OrderBy(s => s.Order)
            .Select(s => new { modulo = s.ModuleName, orden = s.Order });

        return Ok(new { seedersRegistrados = info });
    }
}
