using MonolitoModular.CajaDeAhorro.Application.DTOs;

namespace MonolitoModular.CajaDeAhorro.Application.Services;

public interface ICuentaAhorroService
{
    Task<CuentaAhorroDto?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<CuentaAhorroDto>> ObtenerTodosAsync();
    Task<IEnumerable<CuentaAhorroDto>> ObtenerPorClienteAsync(Guid clienteId);
    Task<CuentaAhorroDto> AbrirCuentaAsync(AbrirCuentaDto dto);
    Task<CuentaAhorroDto?> DepositarAsync(Guid id, OperacionDto dto);
    Task<CuentaAhorroDto?> ExtraerAsync(Guid id, OperacionDto dto);
    Task<CuentaAhorroDto?> CerrarCuentaAsync(Guid id);
}
