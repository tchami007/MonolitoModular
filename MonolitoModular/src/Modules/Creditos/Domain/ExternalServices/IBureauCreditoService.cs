namespace MonolitoModular.Creditos.Domain.ExternalServices;

/// <summary>
/// Puerto (interfaz) que define el contrato para consultar el score crediticio
/// de un cliente ante un buró de crédito externo.
///
/// El dominio solo conoce esta interfaz. No sabe si detrás hay HTTP, un
/// Circuit Breaker, un fallback local o un mock de tests.
/// </summary>
public interface IBureauCreditoService
{
    Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado);
}
