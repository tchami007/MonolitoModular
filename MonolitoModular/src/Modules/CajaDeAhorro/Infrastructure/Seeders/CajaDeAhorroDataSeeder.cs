using MonolitoModular.CajaDeAhorro.Domain.Entities;
using MonolitoModular.CajaDeAhorro.Domain.Repositories;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.CajaDeAhorro.Infrastructure.Seeders;

/// <summary>
/// Siembra 11 cuentas de ahorro con saldos variados para simular una cartera.
/// Algunos clientes tienen más de una cuenta. Una cuenta está cerrada.
/// </summary>
public class CajaDeAhorroDataSeeder : IDataSeeder
{
    private readonly ICuentaAhorroRepository _repository;

    public string ModuleName => "CajaDeAhorro";
    public int Order => 3;

    // Réplica local de IDs de clientes (sin dependencia al módulo Clientes)
    private static readonly Guid IdGarcia    = new("aaaaaaaa-0001-0000-0000-000000000001");
    private static readonly Guid IdLopez     = new("aaaaaaaa-0001-0000-0000-000000000002");
    private static readonly Guid IdFernandez = new("aaaaaaaa-0001-0000-0000-000000000003");
    private static readonly Guid IdMartinez  = new("aaaaaaaa-0001-0000-0000-000000000004");
    private static readonly Guid IdRodriguez = new("aaaaaaaa-0001-0000-0000-000000000005");
    private static readonly Guid IdGonzalez  = new("aaaaaaaa-0001-0000-0000-000000000006");
    private static readonly Guid IdSanchez   = new("aaaaaaaa-0001-0000-0000-000000000007");
    private static readonly Guid IdPerez     = new("aaaaaaaa-0001-0000-0000-000000000008");
    private static readonly Guid IdTorres    = new("aaaaaaaa-0001-0000-0000-000000000009");
    private static readonly Guid IdRuiz      = new("aaaaaaaa-0001-0000-0000-000000000010");

    private static readonly Guid SeedId1  = new("cccccccc-0003-0000-0000-000000000001");
    private static readonly Guid SeedId2  = new("cccccccc-0003-0000-0000-000000000002");
    private static readonly Guid SeedId3  = new("cccccccc-0003-0000-0000-000000000003");
    private static readonly Guid SeedId4  = new("cccccccc-0003-0000-0000-000000000004");
    private static readonly Guid SeedId5  = new("cccccccc-0003-0000-0000-000000000005");
    private static readonly Guid SeedId6  = new("cccccccc-0003-0000-0000-000000000006");
    private static readonly Guid SeedId7  = new("cccccccc-0003-0000-0000-000000000007");
    private static readonly Guid SeedId8  = new("cccccccc-0003-0000-0000-000000000008");
    private static readonly Guid SeedId9  = new("cccccccc-0003-0000-0000-000000000009");
    private static readonly Guid SeedId10 = new("cccccccc-0003-0000-0000-000000000010");
    private static readonly Guid SeedId11 = new("cccccccc-0003-0000-0000-000000000011");

    public CajaDeAhorroDataSeeder(ICuentaAhorroRepository repository)
    {
        _repository = repository;
    }

    public async Task SeedAsync()
    {
        var existente = await _repository.ObtenerPorIdAsync(SeedId1);
        if (existente is not null) return;

        // Tuplas: (id, clienteId, numeroCuenta, saldoInicial, cerrada)
        var datos = new List<(Guid Id, Guid ClienteId, string Numero, decimal Saldo, bool Cerrada)>
        {
            (SeedId1,  IdGarcia,    "CA-0001-00004521-3", 85_400.50m,  false),
            (SeedId2,  IdGarcia,    "CA-0001-00009874-1", 12_000.00m,  false),  // García tiene 2 cuentas
            (SeedId3,  IdLopez,     "CA-0001-00002341-7", 230_750.25m, false),
            (SeedId4,  IdFernandez, "CA-0001-00007612-4", 47_300.00m,  false),
            (SeedId5,  IdMartinez,  "CA-0001-00003398-9", 5_800.75m,   false),
            (SeedId6,  IdRodriguez, "CA-0001-00005523-2", 412_900.00m, false),
            (SeedId7,  IdRodriguez, "CA-0001-00008841-6", 18_500.00m,  false),  // Rodríguez tiene 2 cuentas
            (SeedId8,  IdGonzalez,  "CA-0001-00001190-5", 96_200.50m,  false),
            (SeedId9,  IdSanchez,   "CA-0001-00006677-8", 1_250_000.00m, false),
            (SeedId10, IdPerez,     "CA-0001-00004412-0", 33_450.25m,  false),
            (SeedId11, IdTorres,    "CA-0001-00009001-3", 0m,          true),   // cuenta cerrada
        };

        foreach (var (id, clienteId, numero, saldo, cerrada) in datos)
        {
            var cuenta = new CuentaAhorro(id, clienteId, numero);

            if (saldo > 0)
                cuenta.Depositar(saldo);

            if (cerrada)
                cuenta.Cerrar();

            await _repository.AgregarAsync(cuenta);
        }
    }
}
