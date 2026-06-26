using Microsoft.Extensions.DependencyInjection;
using MonolitoModular.Clientes.Application.Services;
using MonolitoModular.Clientes.Domain.Repositories;
using MonolitoModular.Clientes.Infrastructure.Repositories;
using MonolitoModular.Clientes.Infrastructure.Seeders;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Clientes;

/// <summary>
/// Punto de entrada del módulo Clientes para el contenedor de DI.
/// El host llama a este método — no conoce los detalles internos del módulo.
/// </summary>
public static class ClientesModule
{
    public static IServiceCollection AddClientesModule(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IClienteRepository, ClienteRepositoryInMemory>();

        // Application
        services.AddScoped<IClienteService, ClienteService>();

        // Seeder
        services.AddTransient<IDataSeeder, ClientesDataSeeder>();

        return services;
    }
}
