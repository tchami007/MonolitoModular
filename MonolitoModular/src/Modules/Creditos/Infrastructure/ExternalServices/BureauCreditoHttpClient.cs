using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// <summary>
/// Adaptador HTTP al buró de crédito externo.
///
/// Responsabilidad: comunicación de red — obtener la respuesta cruda del buró
/// en su propio esquema (BureauExternoResponse).
/// La traducción al modelo de dominio es responsabilidad del BureauCreditoAcl.
///
/// Antes de ADR-001: este método mezclaba HTTP + traducción inline.
/// Después de ADR-001: dos pasos explícitos y separados.
///
/// En producción, SimularRespuestaExterna se reemplaza por:
///   HttpClient.GetAsync(url) + JsonSerializer.Deserialize{BureauExternoResponse}(json)
/// </summary>
public class BureauCreditoHttpClient : IBureauCreditoService
{
    private static readonly Random _rng = new();
    private readonly BureauCreditoAcl _acl;

    public BureauCreditoHttpClient(BureauCreditoAcl acl)
    {
        _acl = acl;
    }

    public Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado)
    {
        // Simula degradación o caída del buró externo ~40% del tiempo.
        // Esto permite disparar el Circuit Breaker en pocas llamadas durante la demo.
        if (_rng.NextDouble() < 0.4)
            throw new HttpRequestException(
                "Buró de crédito no disponible — timeout de conexión simulado.");

        // Paso 1: obtener la respuesta cruda en el esquema externo del buró.
        var respuestaExterna = SimularRespuestaExterna(clienteId);

        // Paso 2: traducir al modelo de dominio a través del ACL.
        var resultado = _acl.Traducir(respuestaExterna);

        return Task.FromResult(resultado);
    }

    /// <summary>
    /// Simula la respuesta raw del buró en su propio esquema (escala 1–950).
    /// En producción: HttpClient.GetAsync(...) + JsonSerializer.Deserialize&lt;BureauExternoResponse&gt;(...)
    /// </summary>
    private static BureauExternoResponse SimularRespuestaExterna(Guid clienteId)
    {
        var puntaje = _rng.Next(285, 903);  // escala real del buró: 1–950
        return new BureauExternoResponse
        {
            CodigoCliente        = clienteId.ToString(),
            PuntajeRiesgo        = puntaje,
            CodigoResultado      = "APROBADO",
            DescripcionResultado = $"Evaluación crediticia completada para cliente {clienteId}.",
            FechaConsulta        = DateTime.UtcNow.ToString("o")
        };
    }
}
