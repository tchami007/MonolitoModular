using MonolitoModular.Clientes.Domain.Entities;
using MonolitoModular.Clientes.Domain.Repositories;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Clientes.Infrastructure.Seeders;

/// <summary>
/// Siembra una cartera ficticia de 10 clientes con datos en castellano.
/// Los GUIDs son fijos y deterministas — los otros módulos los reutilizan
/// sin necesidad de consultar este módulo (boundaries respetados).
/// </summary>
public class ClientesDataSeeder : IDataSeeder
{
    private readonly IClienteRepository _repository;

    public string ModuleName => "Clientes";
    public int Order => 1;

    // GUIDs públicos y estables — otros módulos los importan como constantes
    public static readonly Guid IdGarcia       = new("aaaaaaaa-0001-0000-0000-000000000001");
    public static readonly Guid IdLopez        = new("aaaaaaaa-0001-0000-0000-000000000002");
    public static readonly Guid IdFernandez    = new("aaaaaaaa-0001-0000-0000-000000000003");
    public static readonly Guid IdMartinez     = new("aaaaaaaa-0001-0000-0000-000000000004");
    public static readonly Guid IdRodriguez    = new("aaaaaaaa-0001-0000-0000-000000000005");
    public static readonly Guid IdGonzalez     = new("aaaaaaaa-0001-0000-0000-000000000006");
    public static readonly Guid IdSanchez      = new("aaaaaaaa-0001-0000-0000-000000000007");
    public static readonly Guid IdPerez        = new("aaaaaaaa-0001-0000-0000-000000000008");
    public static readonly Guid IdTorres       = new("aaaaaaaa-0001-0000-0000-000000000009");
    public static readonly Guid IdRuiz         = new("aaaaaaaa-0001-0000-0000-000000000010");

    public ClientesDataSeeder(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task SeedAsync()
    {
        // Idempotente: si ya existe el primer cliente, no repite la carga
        var existente = await _repository.ObtenerPorIdAsync(IdGarcia);
        if (existente is not null) return;

        var clientes = new List<Cliente>
        {
            new(IdGarcia,    "Juan",      "García",     "20-35421678-9", "juan.garcia@email.com"),
            new(IdLopez,     "Carlos",    "López",      "20-28743921-4", "carlos.lopez@email.com"),
            new(IdFernandez, "María",     "Fernández",  "27-41236789-3", "maria.fernandez@email.com"),
            new(IdMartinez,  "Ana",       "Martínez",   "27-33891245-6", "ana.martinez@email.com"),
            new(IdRodriguez, "Pedro",     "Rodríguez",  "20-29145867-1", "pedro.rodriguez@email.com"),
            new(IdGonzalez,  "Laura",     "González",   "27-37654321-8", "laura.gonzalez@email.com"),
            new(IdSanchez,   "Diego",     "Sánchez",    "20-44123456-2", "diego.sanchez@email.com"),
            new(IdPerez,     "Sofía",     "Pérez",      "27-39876543-7", "sofia.perez@email.com"),
            new(IdTorres,    "Roberto",   "Torres",     "20-31987654-5", "roberto.torres@email.com"),
            new(IdRuiz,      "Valentina", "Ruiz",       "27-46321098-0", "valentina.ruiz@email.com"),
        };

        foreach (var cliente in clientes)
            await _repository.AgregarAsync(cliente);
    }
}
