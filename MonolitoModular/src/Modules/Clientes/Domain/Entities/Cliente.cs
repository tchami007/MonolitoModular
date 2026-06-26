using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Clientes.Domain.Entities;

public class Cliente : Entity<Guid>
{
    public string Nombre { get; private set; }
    public string Apellido { get; private set; }
    public string Dni { get; private set; }
    public string Email { get; private set; }
    public DateTime FechaAlta { get; private set; }

    // Constructor privado para EF Core / deserialización
    private Cliente() : base()
    {
        Nombre = string.Empty;
        Apellido = string.Empty;
        Dni = string.Empty;
        Email = string.Empty;
    }

    public Cliente(Guid id, string nombre, string apellido, string dni, string email)
        : base(id)
    {
        Nombre = nombre;
        Apellido = apellido;
        Dni = dni;
        Email = email;
        FechaAlta = DateTime.UtcNow;
    }

    public void ActualizarDatos(string nombre, string apellido, string email)
    {
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
