using MonolitoModular.CajaDeAhorro.Application.DTOs;
using MonolitoModular.CajaDeAhorro.Domain.Entities;
using MonolitoModular.CajaDeAhorro.Domain.Repositories;

namespace MonolitoModular.CajaDeAhorro.Application.Services;

public class CuentaAhorroService : ICuentaAhorroService
{
    private readonly ICuentaAhorroRepository _repository;

    public CuentaAhorroService(ICuentaAhorroRepository repository)
    {
        _repository = repository;
    }

    public async Task<CuentaAhorroDto?> ObtenerPorIdAsync(Guid id)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(id);
        return cuenta is null ? null : MapToDto(cuenta);
    }

    public async Task<IEnumerable<CuentaAhorroDto>> ObtenerTodosAsync()
    {
        var cuentas = await _repository.ObtenerTodosAsync();
        return cuentas.Select(MapToDto);
    }

    public async Task<IEnumerable<CuentaAhorroDto>> ObtenerPorClienteAsync(Guid clienteId)
    {
        var cuentas = await _repository.ObtenerPorClienteAsync(clienteId);
        return cuentas.Select(MapToDto);
    }

    public async Task<CuentaAhorroDto> AbrirCuentaAsync(AbrirCuentaDto dto)
    {
        var cuenta = new CuentaAhorro(Guid.NewGuid(), dto.ClienteId, dto.NumeroCuenta);
        await _repository.AgregarAsync(cuenta);
        return MapToDto(cuenta);
    }

    public async Task<CuentaAhorroDto?> DepositarAsync(Guid id, OperacionDto dto)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(id);
        if (cuenta is null) return null;
        cuenta.Depositar(dto.Monto);
        await _repository.ActualizarAsync(cuenta);
        return MapToDto(cuenta);
    }

    public async Task<CuentaAhorroDto?> ExtraerAsync(Guid id, OperacionDto dto)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(id);
        if (cuenta is null) return null;
        cuenta.Extraer(dto.Monto);
        await _repository.ActualizarAsync(cuenta);
        return MapToDto(cuenta);
    }

    public async Task<CuentaAhorroDto?> CerrarCuentaAsync(Guid id)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(id);
        if (cuenta is null) return null;
        cuenta.Cerrar();
        await _repository.ActualizarAsync(cuenta);
        return MapToDto(cuenta);
    }

    private static CuentaAhorroDto MapToDto(CuentaAhorro c) =>
        new(c.Id, c.ClienteId, c.NumeroCuenta, c.Saldo, c.FechaApertura, c.Activa);
}
