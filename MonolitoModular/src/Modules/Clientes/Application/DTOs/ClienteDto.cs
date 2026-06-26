namespace MonolitoModular.Clientes.Application.DTOs;

public record ClienteDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Dni,
    string Email,
    DateTime FechaAlta
);

public record CrearClienteDto(
    string Nombre,
    string Apellido,
    string Dni,
    string Email
);

public record ActualizarClienteDto(
    string Nombre,
    string Apellido,
    string Email
);
