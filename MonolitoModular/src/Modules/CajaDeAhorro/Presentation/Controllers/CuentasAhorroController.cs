using Microsoft.AspNetCore.Mvc;
using MonolitoModular.CajaDeAhorro.Application.DTOs;
using MonolitoModular.CajaDeAhorro.Application.Services;

namespace MonolitoModular.CajaDeAhorro.Presentation.Controllers;

[ApiController]
[Route("api/cuentas-ahorro")]
public class CuentasAhorroController : ControllerBase
{
    private readonly ICuentaAhorroService _service;

    public CuentasAhorroController(ICuentaAhorroService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.ObtenerTodosAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cuenta = await _service.ObtenerPorIdAsync(id);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> GetByCliente(Guid clienteId) =>
        Ok(await _service.ObtenerPorClienteAsync(clienteId));

    [HttpPost]
    public async Task<IActionResult> Abrir([FromBody] AbrirCuentaDto dto)
    {
        var cuenta = await _service.AbrirCuentaAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = cuenta.Id }, cuenta);
    }

    [HttpPatch("{id:guid}/depositar")]
    public async Task<IActionResult> Depositar(Guid id, [FromBody] OperacionDto dto)
    {
        var cuenta = await _service.DepositarAsync(id, dto);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }

    [HttpPatch("{id:guid}/extraer")]
    public async Task<IActionResult> Extraer(Guid id, [FromBody] OperacionDto dto)
    {
        try
        {
            var cuenta = await _service.ExtraerAsync(id, dto);
            return cuenta is null ? NotFound() : Ok(cuenta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cerrar")]
    public async Task<IActionResult> Cerrar(Guid id)
    {
        var cuenta = await _service.CerrarCuentaAsync(id);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }
}
