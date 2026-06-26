using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// <summary>
/// Anti-Corruption Layer (ACL) del módulo Créditos para el buró de crédito externo.
///
/// Responsabilidad única: traducir BureauExternoResponse → ResultadoBureau.
/// Es el único punto del código que conoce simultáneamente el esquema externo
/// del buró y el modelo de dominio interno.
///
/// Principio de diseño: el ACL traduce, nunca decide.
/// Si el buró responde "NO_ENCONTRADO", el ACL devuelve Score=0 con contexto
/// en Detalle. La política de negocio —qué hacer con score=0— es responsabilidad
/// de CreditoService (capa Application), no del ACL (Infrastructure).
///
/// Esto respeta la regla de dependencias de DDD: Infrastructure no impone
/// políticas al dominio. El dominio permanece estable aunque el buró cambie
/// su API, su escala o su proveedor.
/// </summary>
public class BureauCreditoAcl
{
    /// <summary>
    /// Traduce la respuesta cruda del buró al modelo de dominio interno.
    /// Nunca lanza excepciones: siempre devuelve un ResultadoBureau válido.
    /// </summary>
    public ResultadoBureau Traducir(BureauExternoResponse respuesta)
    {
        if (respuesta.CodigoResultado == "APROBADO")
            return new ResultadoBureau(
                Score:   NormalizarPuntaje(respuesta.PuntajeRiesgo),
                Fuente:  FuenteScore.BureauExterno,
                Detalle: $"[ACL] Evaluación exitosa: {respuesta.DescripcionResultado}"
            );

        // Cualquier otro estado externo se traduce a Score=0.
        // El dominio entiende Score=0 como "sin puntaje válido del buró".
        // La decisión de negocio sobre qué hacer pertenece a CreditoService.
        return new ResultadoBureau(
            Score:   0,
            Fuente:  FuenteScore.BureauExterno,
            Detalle: $"[ACL] Estado no aprobatorio ({respuesta.CodigoResultado}): " +
                     $"{respuesta.DescripcionResultado}"
        );
    }

    /// <summary>
    /// Convierte la escala del buró (1–950) a la escala interna del dominio (0–1000).
    /// Si el buró cambia su escala en una versión futura de la API, solo cambia esta línea.
    /// </summary>
    private static int NormalizarPuntaje(int puntajeExterno)
        => (int)Math.Round(puntajeExterno / 950.0 * 1000);
}
