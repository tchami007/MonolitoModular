using MonolitoModular.Creditos.Domain.Entities;

namespace MonolitoModular.Creditos.Application.DTOs;

public record CreditoDto(
    Guid Id,
    Guid ClienteId,
    decimal Monto,
    decimal TasaInteres,
    int PlazoEnMeses,
    string Estado,
    DateTime FechaSolicitud,
    int ScoreCredito,
    string FuenteScore
);

public record SolicitarCreditoDto(
    Guid ClienteId,
    decimal Monto,
    decimal TasaInteres,
    int PlazoEnMeses
);
