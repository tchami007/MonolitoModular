using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// <summary>
/// Implementación real del cliente HTTP al buró de crédito externo.
///
/// En este ejemplo educativo la llamada HTTP está simulada:
/// - ~40% de las veces lanza HttpRequestException para permitir que el
///   Circuit Breaker entre en acción y pueda observarse el comportamiento.
/// - El restante 60% devuelve un score aleatorio en rango real (300–950).
///
/// En producción, aquí viviría el HttpClient real con la URL del buró,
/// headers de autenticación, manejo de timeouts y deserialización JSON.
/// </summary>
public class BureauCreditoHttpClient : IBureauCreditoService
{
    private static readonly Random _rng = new();

    public Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado)
    {
        // Simula degradación o caída del buró externo ~40% del tiempo.
        // Esto permite disparar el Circuit Breaker en pocas llamadas durante la demo.
        if (_rng.NextDouble() < 0.4)
            throw new HttpRequestException(
                "Buró de crédito no disponible — timeout de conexión simulado.");

        var score = _rng.Next(300, 951);

        return Task.FromResult(new ResultadoBureau(
            Score:   score,
            Fuente:  FuenteScore.BureauExterno,
            Detalle: $"Score real obtenido del buró externo para cliente {clienteId}. " +
                     $"Monto evaluado: ${montoSolicitado:N2}."
        ));
    }
}
