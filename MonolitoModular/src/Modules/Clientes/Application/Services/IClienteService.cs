using MonolitoModular.Clientes.Application.DTOs;

namespace MonolitoModular.Clientes.Application.Services;

public interface IClienteService
{
    Task<ClienteDto?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<ClienteDto>> ObtenerTodosAsync();
    Task<ClienteDto> CrearAsync(CrearClienteDto dto);
    Task<ClienteDto?> ActualizarAsync(Guid id, ActualizarClienteDto dto);
    Task<bool> EliminarAsync(Guid id);
}
