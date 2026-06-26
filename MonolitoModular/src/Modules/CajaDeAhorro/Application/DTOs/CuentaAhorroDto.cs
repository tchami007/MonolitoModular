namespace MonolitoModular.CajaDeAhorro.Application.DTOs;

public record CuentaAhorroDto(
    Guid Id,
    Guid ClienteId,
    string NumeroCuenta,
    decimal Saldo,
    DateTime FechaApertura,
    bool Activa
);

public record AbrirCuentaDto(
    Guid ClienteId,
    string NumeroCuenta
);

public record OperacionDto(decimal Monto);
