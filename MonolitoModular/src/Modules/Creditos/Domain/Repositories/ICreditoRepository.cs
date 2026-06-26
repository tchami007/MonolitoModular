using MonolitoModular.Creditos.Domain.Entities;

namespace MonolitoModular.Creditos.Domain.Repositories;

public interface ICreditoRepository
{
    Task<Credito?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<Credito>> ObtenerTodosAsync();
    Task<IEnumerable<Credito>> ObtenerPorClienteAsync(Guid clienteId);
    Task AgregarAsync(Credito credito);
    Task ActualizarAsync(Credito credito);
}
