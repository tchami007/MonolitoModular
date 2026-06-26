using MonolitoModular.Clientes.Domain.Entities;

namespace MonolitoModular.Clientes.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para Cliente.
/// Definido en Domain — la Infrastructure lo implementa.
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<Cliente>> ObtenerTodosAsync();
    Task AgregarAsync(Cliente cliente);
    Task ActualizarAsync(Cliente cliente);
    Task EliminarAsync(Guid id);
}
