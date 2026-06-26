namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// <summary>
/// Esquema de la respuesta cruda del buró de crédito externo.
///
/// Representa los datos tal como el buró los devuelve: su propia nomenclatura,
/// su propia escala de puntaje (1–950) y sus propios códigos de resultado.
///
/// Esta clase NO pertenece al dominio. Es la "lengua extranjera" que la
/// Anti-Corruption Layer (BureauCreditoAcl) traduce al modelo interno.
///
/// Si el buró migra a una API v2 con campos distintos, solo cambian esta
/// clase y el ACL que la traduce. El dominio (ResultadoBureau) permanece intacto.
/// </summary>
public class BureauExternoResponse
{
    /// <summary>
    /// Identificador del cliente en el sistema del buró.
    /// El buró usa strings (CUIT, DNI formateado), no Guid.
    /// </summary>
    public string CodigoCliente { get; init; } = string.Empty;

    /// <summary>
    /// Puntaje de riesgo en la escala propia del buró: 1–950.
    /// El dominio usa escala 0–1000; la normalización ocurre en BureauCreditoAcl.
    /// </summary>
    public int PuntajeRiesgo { get; init; }

    /// <summary>
    /// Código de resultado según la nomenclatura del buró externo.
    /// Valores posibles: "APROBADO", "NO_ENCONTRADO", "ERROR_TIMEOUT", "DATOS_INSUFICIENTES".
    /// </summary>
    public string CodigoResultado { get; init; } = string.Empty;

    /// <summary>
    /// Descripción textual del resultado en el formato del buró.
    /// </summary>
    public string DescripcionResultado { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp de la consulta según el servidor del buró (ISO 8601).
    /// </summary>
    public string FechaConsulta { get; init; } = string.Empty;
}
