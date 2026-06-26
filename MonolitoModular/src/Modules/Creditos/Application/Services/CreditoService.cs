using MonolitoModular.Creditos.Application.DTOs;
using MonolitoModular.Creditos.Domain.Entities;
using MonolitoModular.Creditos.Domain.ExternalServices;
using MonolitoModular.Creditos.Domain.Repositories;

namespace MonolitoModular.Creditos.Application.Services;

public class CreditoService : ICreditoService
{
    private readonly ICreditoRepository _repository;
    private readonly IBureauCreditoService _bureauService;

    public CreditoService(ICreditoRepository repository, IBureauCreditoService bureauService)
    {
        _repository = repository;
        _bureauService = bureauService;
    }

    public async Task<CreditoDto?> ObtenerPorIdAsync(Guid id)
    {
        var credito = await _repository.ObtenerPorIdAsync(id);
        return credito is null ? null : MapToDto(credito);
    }

    public async Task<IEnumerable<CreditoDto>> ObtenerTodosAsync()
    {
        var creditos = await _repository.ObtenerTodosAsync();
        return creditos.Select(MapToDto);
    }

    public async Task<IEnumerable<CreditoDto>> ObtenerPorClienteAsync(Guid clienteId)
    {
        var creditos = await _repository.ObtenerPorClienteAsync(clienteId);
        return creditos.Select(MapToDto);
    }

    public async Task<CreditoDto> SolicitarAsync(SolicitarCreditoDto dto)
    {
        // Consulta el score crediticio al buró externo.
        // Si el buró está caído, el Circuit Breaker devuelve un score de fallback local
        // sin lanzar excepción — la solicitud nunca falla por culpa del externo.
        var resultado = await _bureauService.ConsultarScoreAsync(dto.ClienteId, dto.Monto);

        var credito = new Credito(
            id:           Guid.NewGuid(),
            clienteId:    dto.ClienteId,
            monto:        dto.Monto,
            tasaInteres:  dto.TasaInteres,
            plazoEnMeses: dto.PlazoEnMeses,
            scoreCredito: resultado.Score,
            fuenteScore:  resultado.Fuente.ToString()
        );

        await _repository.AgregarAsync(credito);
        return MapToDto(credito);
    }

    public async Task<CreditoDto?> AprobarAsync(Guid id)
    {
        var credito = await _repository.ObtenerPorIdAsync(id);
        if (credito is null) return null;
        credito.Aprobar();
        await _repository.ActualizarAsync(credito);
        return MapToDto(credito);
    }

    public async Task<CreditoDto?> RechazarAsync(Guid id)
    {
        var credito = await _repository.ObtenerPorIdAsync(id);
        if (credito is null) return null;
        credito.Rechazar();
        await _repository.ActualizarAsync(credito);
        return MapToDto(credito);
    }

    public async Task<CreditoDto?> ActivarAsync(Guid id)
    {
        var credito = await _repository.ObtenerPorIdAsync(id);
        if (credito is null) return null;
        credito.Activar();
        await _repository.ActualizarAsync(credito);
        return MapToDto(credito);
    }

    public async Task<CreditoDto?> CancelarAsync(Guid id)
    {
        var credito = await _repository.ObtenerPorIdAsync(id);
        if (credito is null) return null;
        credito.Cancelar();
        await _repository.ActualizarAsync(credito);
        return MapToDto(credito);
    }

    private static CreditoDto MapToDto(Credito c) =>
        new(c.Id, c.ClienteId, c.Monto, c.TasaInteres, c.PlazoEnMeses,
            c.Estado.ToString(), c.FechaSolicitud, c.ScoreCredito, c.FuenteScore);
}
