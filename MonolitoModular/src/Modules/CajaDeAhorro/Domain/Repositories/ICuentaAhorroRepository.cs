using MonolitoModular.CajaDeAhorro.Domain.Entities;

namespace MonolitoModular.CajaDeAhorro.Domain.Repositories;

public interface ICuentaAhorroRepository
{
    Task<CuentaAhorro?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<CuentaAhorro>> ObtenerTodosAsync();
    Task<IEnumerable<CuentaAhorro>> ObtenerPorClienteAsync(Guid clienteId);
    Task AgregarAsync(CuentaAhorro cuenta);
    Task ActualizarAsync(CuentaAhorro cuenta);
}
