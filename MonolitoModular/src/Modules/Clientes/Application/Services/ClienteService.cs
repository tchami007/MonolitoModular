using MonolitoModular.Clientes.Application.DTOs;
using MonolitoModular.Clientes.Domain.Entities;
using MonolitoModular.Clientes.Domain.Repositories;

namespace MonolitoModular.Clientes.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClienteDto?> ObtenerPorIdAsync(Guid id)
    {
        var cliente = await _repository.ObtenerPorIdAsync(id);
        return cliente is null ? null : MapToDto(cliente);
    }

    public async Task<IEnumerable<ClienteDto>> ObtenerTodosAsync()
    {
        var clientes = await _repository.ObtenerTodosAsync();
        return clientes.Select(MapToDto);
    }

    public async Task<ClienteDto> CrearAsync(CrearClienteDto dto)
    {
        var cliente = new Cliente(Guid.NewGuid(), dto.Nombre, dto.Apellido, dto.Dni, dto.Email);
        await _repository.AgregarAsync(cliente);
        return MapToDto(cliente);
    }

    public async Task<ClienteDto?> ActualizarAsync(Guid id, ActualizarClienteDto dto)
    {
        var cliente = await _repository.ObtenerPorIdAsync(id);
        if (cliente is null) return null;

        cliente.ActualizarDatos(dto.Nombre, dto.Apellido, dto.Email);
        await _repository.ActualizarAsync(cliente);
        return MapToDto(cliente);
    }

    public async Task<bool> EliminarAsync(Guid id)
    {
        var cliente = await _repository.ObtenerPorIdAsync(id);
        if (cliente is null) return false;

        await _repository.EliminarAsync(id);
        return true;
    }

    private static ClienteDto MapToDto(Cliente c) =>
        new(c.Id, c.Nombre, c.Apellido, c.Dni, c.Email, c.FechaAlta);
}
