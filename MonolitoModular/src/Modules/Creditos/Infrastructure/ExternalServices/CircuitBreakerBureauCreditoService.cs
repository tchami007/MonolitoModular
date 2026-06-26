using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// <summary>
/// Estados posibles del disyuntor (Circuit Breaker).
/// </summary>
public enum EstadoCircuito
{
    /// <summary>Circuito cerrado: las llamadas pasan normalmente al proveedor externo.</summary>
    Cerrado,

    /// <summary>Circuito abierto: el proveedor falló repetidamente. Se devuelve fallback inmediatamente.</summary>
    Abierto,

    /// <summary>En prueba: el tiempo de enfriamiento expiró. Se deja pasar una llamada de prueba.</summary>
    MedioAbierto
}

/// <summary>
/// Implementación del patrón Circuit Breaker como Decorator sobre IBureauCreditoService.
///
/// MÁQUINA DE ESTADOS:
///
///   CERRADO ──(N fallos >= umbral)──► ABIERTO ──(cooldown expiró)──► MEDIO_ABIERTO
///      ▲                                                                    │
///      └─────────────────(llamada exitosa)──────────────────────────────────┘
///                         (fallo en MedioAbierto) ──────────────────► ABIERTO
///
/// COMPORTAMIENTO POR ESTADO:
///   CERRADO     → llama al _inner (BureauCreditoHttpClient); cuenta fallos.
///   ABIERTO     → NO llama al _inner; devuelve FallbackLocal() directamente.
///   MEDIO_ABIERTO → deja pasar UNA llamada de prueba:
///                    éxito → vuelve a CERRADO (reset completo).
///                    fallo → vuelve a ABIERTO (reinicia timer).
///
/// FALLBACK LOCAL:
///   Calcula un score conservador basado en el monto solicitado:
///     ≤  $5.000   → Score 700  (riesgo bajo, aprobable)
///     ≤ $20.000   → Score 580  (riesgo medio, revisión manual)
///     >  $20.000  → Score 420  (riesgo alto, probable rechazo)
/// </summary>
public class CircuitBreakerBureauCreditoService : IBureauCreditoService
{
    private readonly IBureauCreditoService _inner;
    private readonly int _umbralFallos;
    private readonly TimeSpan _tiempoEnfriamiento;

    private EstadoCircuito _estado = EstadoCircuito.Cerrado;
    private int _contadorFallos = 0;
    private DateTime? _abiertoEn = null;

    // Lock para garantizar que la transición de estados sea thread-safe.
    // El CB vive como Singleton y puede recibir requests concurrentes.
    private readonly object _lock = new();

    public CircuitBreakerBureauCreditoService(
        IBureauCreditoService inner,
        int umbralFallos = 3,
        TimeSpan? tiempoEnfriamiento = null)
    {
        _inner = inner;
        _umbralFallos = umbralFallos;
        _tiempoEnfriamiento = tiempoEnfriamiento ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>Expone el estado actual del circuito (útil para health checks y logs).</summary>
    public EstadoCircuito Estado => _estado;

    public async Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado)
    {
        // ── Evaluación del estado ANTES de llamar ────────────────────────────
        lock (_lock)
        {
            if (_estado == EstadoCircuito.Abierto)
            {
                if (DateTime.UtcNow - _abiertoEn >= _tiempoEnfriamiento)
                {
                    // Cooldown expiró: permitir una llamada de prueba
                    _estado = EstadoCircuito.MedioAbierto;
                }
                else
                {
                    // Circuito todavía abierto: fallback inmediato, sin tocar el externo
                    return FallbackLocal(montoSolicitado,
                        $"Circuito ABIERTO — buró no disponible. " +
                        $"Reapertura en {(_tiempoEnfriamiento - (DateTime.UtcNow - _abiertoEn.Value)).Seconds}s.");
                }
            }
        }

        // ── Llamada al proveedor externo ─────────────────────────────────────
        try
        {
            var resultado = await _inner.ConsultarScoreAsync(clienteId, montoSolicitado);

            lock (_lock)
            {
                // Llamada exitosa: cerrar el circuito y resetear el contador
                _estado = EstadoCircuito.Cerrado;
                _contadorFallos = 0;
                _abiertoEn = null;
            }

            return resultado;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _contadorFallos++;

                bool debeAbrir = _contadorFallos >= _umbralFallos
                              || _estado == EstadoCircuito.MedioAbierto;

                if (debeAbrir)
                {
                    _estado = EstadoCircuito.Abierto;
                    _abiertoEn = DateTime.UtcNow;
                }
            }

            return FallbackLocal(montoSolicitado,
                $"Fallo #{_contadorFallos} al contactar buró: {ex.Message}");
        }
    }

    // ── Lógica de scoring local ──────────────────────────────────────────────

    private static ResultadoBureau FallbackLocal(decimal monto, string contexto)
    {
        var (score, criterio) = monto switch
        {
            <= 5_000m  => (700, "Monto conservador — riesgo bajo, aprobable"),
            <= 20_000m => (580, "Monto moderado — riesgo medio, requiere revisión manual"),
            _          => (420, "Monto elevado — riesgo alto, probable rechazo")
        };

        return new ResultadoBureau(
            Score:   score,
            Fuente:  FuenteScore.FallbackLocal,
            Detalle: $"[FALLBACK LOCAL] {criterio}. Contexto: {contexto}"
        );
    }
}
