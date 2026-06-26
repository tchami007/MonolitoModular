using Microsoft.Extensions.DependencyInjection;
using MonolitoModular.Creditos.Application.Services;
using MonolitoModular.Creditos.Domain.ExternalServices;
using MonolitoModular.Creditos.Domain.Repositories;
using MonolitoModular.Creditos.Infrastructure.ExternalServices;
using MonolitoModular.Creditos.Infrastructure.Repositories;
using MonolitoModular.Creditos.Infrastructure.Seeders;
using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Creditos;

public static class CreditosModule
{
    public static IServiceCollection AddCreditosModule(this IServiceCollection services)
    {
        // ── Repositorio ──────────────────────────────────────────────────────
        services.AddSingleton<ICreditoRepository, CreditoRepositoryInMemory>();

        // ── ACL: traductor entre el buró externo y el dominio ────────────────
        // Singleton: no tiene estado mutable; es una función de traducción pura.
        // Reutilizable entre requests concurrentes sin riesgo de condición de carrera.
        services.AddSingleton<BureauCreditoAcl>();

        // ── Circuit Breaker para el Buró de Crédito ──────────────────────────
        // El CB debe vivir como Singleton: su estado (Cerrado/Abierto/MedioAbierto)
        // y el contador de fallos deben persistir entre requests HTTP.
        // Un Scoped o Transient resetearía el estado en cada request — inútil.
        // BureauCreditoHttpClient recibe el ACL por constructor (inyección normal).
        services.AddSingleton<BureauCreditoHttpClient>();
        services.AddSingleton<IBureauCreditoService>(sp =>
            new CircuitBreakerBureauCreditoService(
                inner:              sp.GetRequiredService<BureauCreditoHttpClient>(),
                umbralFallos:       3,
                tiempoEnfriamiento: TimeSpan.FromSeconds(30)
            ));

        // ── Servicio de aplicación ───────────────────────────────────────────
        services.AddScoped<ICreditoService, CreditoService>();

        // ── Seeder ───────────────────────────────────────────────────────────
        services.AddTransient<IDataSeeder, CreditosDataSeeder>();

        return services;
    }
}
