using Microsoft.Extensions.DependencyInjection;
using MonolitoModular.Creditos.Application.Services;
using MonolitoModular.Creditos.Domain.Repositories;
using MonolitoModular.Creditos.Infrastructure.Repositories;
using MonolitoModular.Creditos.Infrastructure.Seeders;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Creditos;

public static class CreditosModule
{
    public static IServiceCollection AddCreditosModule(this IServiceCollection services)
    {
        services.AddSingleton<ICreditoRepository, CreditoRepositoryInMemory>();
        services.AddScoped<ICreditoService, CreditoService>();

        // Seeder
        services.AddTransient<IDataSeeder, CreditosDataSeeder>();

        return services;
    }
}
