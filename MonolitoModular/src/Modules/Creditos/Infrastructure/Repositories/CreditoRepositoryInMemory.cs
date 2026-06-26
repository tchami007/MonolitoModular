using MonolitoModular.Creditos.Domain.Entities;
using MonolitoModular.Creditos.Domain.Repositories;

namespace MonolitoModular.Creditos.Infrastructure.Repositories;

public class CreditoRepositoryInMemory : ICreditoRepository
{
    private readonly Dictionary<Guid, Credito> _store = new();

    public Task<Credito?> ObtenerPorIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var credito);
        return Task.FromResult(credito);
    }

    public Task<IEnumerable<Credito>> ObtenerTodosAsync() =>
        Task.FromResult<IEnumerable<Credito>>(_store.Values.ToList());

    public Task<IEnumerable<Credito>> ObtenerPorClienteAsync(Guid clienteId) =>
        Task.FromResult<IEnumerable<Credito>>(_store.Values.Where(c => c.ClienteId == clienteId).ToList());

    public Task AgregarAsync(Credito credito)
    {
        _store[credito.Id] = credito;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Credito credito)
    {
        _store[credito.Id] = credito;
        return Task.CompletedTask;
    }
}
