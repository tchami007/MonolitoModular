namespace MonolitoModular.Creditos.Domain.ExternalServices;

/// <summary>
/// Indica si el score fue obtenido del buró externo real
/// o calculado localmente por el mecanismo de fallback del Circuit Breaker.
/// </summary>
public enum FuenteScore
{
    BureauExterno,
    FallbackLocal
}

/// <summary>
/// Resultado de una consulta de score crediticio.
/// Encapsula el puntaje, su origen y un mensaje descriptivo.
/// </summary>
/// <param name="Score">Puntaje crediticio en escala 0–1000.</param>
/// <param name="Fuente">Indica si el score proviene del buró o del fallback local.</param>
/// <param name="Detalle">Mensaje explicativo del resultado.</param>
public record ResultadoBureau(
    int Score,
    FuenteScore Fuente,
    string Detalle
);
