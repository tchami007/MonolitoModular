using Microsoft.AspNetCore.Mvc;
using MonolitoModular.Creditos.Application.DTOs;
using MonolitoModular.Creditos.Application.Services;

namespace MonolitoModular.Creditos.Presentation.Controllers;

[ApiController]
[Route("api/creditos")]
public class CreditosController : ControllerBase
{
    private readonly ICreditoService _service;

    public CreditosController(ICreditoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.ObtenerTodosAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var credito = await _service.ObtenerPorIdAsync(id);
        return credito is null ? NotFound() : Ok(credito);
    }

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> GetByCliente(Guid clienteId) =>
        Ok(await _service.ObtenerPorClienteAsync(clienteId));

    [HttpPost]
    public async Task<IActionResult> Solicitar([FromBody] SolicitarCreditoDto dto)
    {
        var credito = await _service.SolicitarAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = credito.Id }, credito);
    }

    [HttpPatch("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        var credito = await _service.AprobarAsync(id);
        return credito is null ? NotFound() : Ok(credito);
    }

    [HttpPatch("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id)
    {
        var credito = await _service.RechazarAsync(id);
        return credito is null ? NotFound() : Ok(credito);
    }

    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        var credito = await _service.ActivarAsync(id);
        return credito is null ? NotFound() : Ok(credito);
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var credito = await _service.CancelarAsync(id);
        return credito is null ? NotFound() : Ok(credito);
    }
}
