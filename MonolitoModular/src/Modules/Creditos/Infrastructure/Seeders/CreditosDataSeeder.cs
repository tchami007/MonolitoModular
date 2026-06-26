using MonolitoModular.Creditos.Domain.Entities;
using MonolitoModular.Creditos.Domain.Repositories;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Creditos.Infrastructure.Seeders;

/// <summary>
/// Siembra 12 créditos en distintos estados para simular una cartera real.
/// Usa los GUIDs fijos del módulo Clientes — sin importar el proyecto Clientes,
/// solo replicando los valores constantes (módulos aislados).
/// </summary>
public class CreditosDataSeeder : IDataSeeder
{
    private readonly ICreditoRepository _repository;

    public string ModuleName => "Creditos";
    public int Order => 2;

    // Réplica local de los IDs de clientes (sin dependencia al módulo Clientes)
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

    private static readonly Guid SeedId1  = new("bbbbbbbb-0002-0000-0000-000000000001");
    private static readonly Guid SeedId2  = new("bbbbbbbb-0002-0000-0000-000000000002");
    private static readonly Guid SeedId3  = new("bbbbbbbb-0002-0000-0000-000000000003");
    private static readonly Guid SeedId4  = new("bbbbbbbb-0002-0000-0000-000000000004");
    private static readonly Guid SeedId5  = new("bbbbbbbb-0002-0000-0000-000000000005");
    private static readonly Guid SeedId6  = new("bbbbbbbb-0002-0000-0000-000000000006");
    private static readonly Guid SeedId7  = new("bbbbbbbb-0002-0000-0000-000000000007");
    private static readonly Guid SeedId8  = new("bbbbbbbb-0002-0000-0000-000000000008");
    private static readonly Guid SeedId9  = new("bbbbbbbb-0002-0000-0000-000000000009");
    private static readonly Guid SeedId10 = new("bbbbbbbb-0002-0000-0000-000000000010");
    private static readonly Guid SeedId11 = new("bbbbbbbb-0002-0000-0000-000000000011");
    private static readonly Guid SeedId12 = new("bbbbbbbb-0002-0000-0000-000000000012");

    public CreditosDataSeeder(ICreditoRepository repository)
    {
        _repository = repository;
    }

    public async Task SeedAsync()
    {
        var existente = await _repository.ObtenerPorIdAsync(SeedId1);
        if (existente is not null) return;

        // Tuplas: (id, clienteId, monto, tasa, plazoMeses, estadoFinal)
        var datos = new List<(Guid Id, Guid ClienteId, decimal Monto, decimal Tasa, int Plazo, EstadoCredito Estado)>
        {
            (SeedId1,  IdGarcia,    150_000m, 42.5m, 24, EstadoCredito.Vigente),
            (SeedId2,  IdGarcia,     80_000m, 38.0m, 12, EstadoCredito.Cancelado),
            (SeedId3,  IdLopez,     320_000m, 45.0m, 36, EstadoCredito.Vigente),
            (SeedId4,  IdFernandez, 200_000m, 40.0m, 24, EstadoCredito.Aprobado),
            (SeedId5,  IdMartinez,   50_000m, 35.5m,  6, EstadoCredito.Solicitado),
            (SeedId6,  IdRodriguez, 500_000m, 48.0m, 48, EstadoCredito.Vigente),
            (SeedId7,  IdRodriguez,  30_000m, 36.0m,  3, EstadoCredito.Rechazado),
            (SeedId8,  IdGonzalez,  180_000m, 41.0m, 18, EstadoCredito.Vigente),
            (SeedId9,  IdSanchez,   750_000m, 50.0m, 60, EstadoCredito.Aprobado),
            (SeedId10, IdPerez,     120_000m, 39.5m, 12, EstadoCredito.Vigente),
            (SeedId11, IdTorres,     60_000m, 37.0m,  6, EstadoCredito.Cancelado),
            (SeedId12, IdRuiz,      400_000m, 44.0m, 36, EstadoCredito.Solicitado),
        };

        foreach (var (id, clienteId, monto, tasa, plazo, estadoFinal) in datos)
        {
            var credito = new Credito(id, clienteId, monto, tasa, plazo);

            // Avanzar el ciclo de vida hasta el estado deseado
            if (estadoFinal == EstadoCredito.Aprobado)
                credito.Aprobar();
            else if (estadoFinal == EstadoCredito.Vigente)
            {
                credito.Aprobar();
                credito.Activar();
            }
            else if (estadoFinal == EstadoCredito.Rechazado)
                credito.Rechazar();
            else if (estadoFinal == EstadoCredito.Cancelado)
            {
                credito.Aprobar();
                credito.Activar();
                credito.Cancelar();
            }

            await _repository.AgregarAsync(credito);
        }
    }
}
