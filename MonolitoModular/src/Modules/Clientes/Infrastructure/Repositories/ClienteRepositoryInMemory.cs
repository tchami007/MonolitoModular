using MonolitoModular.Clientes.Domain.Entities;
using MonolitoModular.Clientes.Domain.Repositories;

namespace MonolitoModular.Clientes.Infrastructure.Repositories;

/// <summary>
/// Implementación in-memory del repositorio de clientes.
/// Reemplazar por implementación con EF Core / Dapper en producción.
/// </summary>
public class ClienteRepositoryInMemory : IClienteRepository
{
    private readonly Dictionary<Guid, Cliente> _store = new();

    public Task<Cliente?> ObtenerPorIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var cliente);
        return Task.FromResult(cliente);
    }

    public Task<IEnumerable<Cliente>> ObtenerTodosAsync()
    {
        return Task.FromResult<IEnumerable<Cliente>>(_store.Values.ToList());
    }

    public Task AgregarAsync(Cliente cliente)
    {
        _store[cliente.Id] = cliente;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Cliente cliente)
    {
        _store[cliente.Id] = cliente;
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}
