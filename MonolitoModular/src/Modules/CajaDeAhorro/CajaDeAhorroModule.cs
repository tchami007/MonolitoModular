using Microsoft.Extensions.DependencyInjection;
using MonolitoModular.CajaDeAhorro.Application.Services;
using MonolitoModular.CajaDeAhorro.Domain.Repositories;
using MonolitoModular.CajaDeAhorro.Infrastructure.Repositories;
using MonolitoModular.CajaDeAhorro.Infrastructure.Seeders;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.CajaDeAhorro;

public static class CajaDeAhorroModule
{
    public static IServiceCollection AddCajaDeAhorroModule(this IServiceCollection services)
    {
        services.AddSingleton<ICuentaAhorroRepository, CuentaAhorroRepositoryInMemory>();
        services.AddScoped<ICuentaAhorroService, CuentaAhorroService>();

        // Seeder
        services.AddTransient<IDataSeeder, CajaDeAhorroDataSeeder>();

        return services;
    }
}
