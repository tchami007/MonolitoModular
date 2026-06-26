using MonolitoModular.Creditos.Application.DTOs;

namespace MonolitoModular.Creditos.Application.Services;

public interface ICreditoService
{
    Task<CreditoDto?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<CreditoDto>> ObtenerTodosAsync();
    Task<IEnumerable<CreditoDto>> ObtenerPorClienteAsync(Guid clienteId);
    Task<CreditoDto> SolicitarAsync(SolicitarCreditoDto dto);
    Task<CreditoDto?> AprobarAsync(Guid id);
    Task<CreditoDto?> RechazarAsync(Guid id);
    Task<CreditoDto?> ActivarAsync(Guid id);
    Task<CreditoDto?> CancelarAsync(Guid id);
}
