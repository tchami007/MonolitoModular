using MonolitoModular.CajaDeAhorro.Domain.Entities;
using MonolitoModular.CajaDeAhorro.Domain.Repositories;

namespace MonolitoModular.CajaDeAhorro.Infrastructure.Repositories;

public class CuentaAhorroRepositoryInMemory : ICuentaAhorroRepository
{
    private readonly Dictionary<Guid, CuentaAhorro> _store = new();

    public Task<CuentaAhorro?> ObtenerPorIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var cuenta);
        return Task.FromResult(cuenta);
    }

    public Task<IEnumerable<CuentaAhorro>> ObtenerTodosAsync() =>
        Task.FromResult<IEnumerable<CuentaAhorro>>(_store.Values.ToList());

    public Task<IEnumerable<CuentaAhorro>> ObtenerPorClienteAsync(Guid clienteId) =>
        Task.FromResult<IEnumerable<CuentaAhorro>>(_store.Values.Where(c => c.ClienteId == clienteId).ToList());

    public Task AgregarAsync(CuentaAhorro cuenta)
    {
        _store[cuenta.Id] = cuenta;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(CuentaAhorro cuenta)
    {
        _store[cuenta.Id] = cuenta;
        return Task.CompletedTask;
    }
}
