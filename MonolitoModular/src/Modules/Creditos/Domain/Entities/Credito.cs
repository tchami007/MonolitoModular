using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.Creditos.Domain.Entities;

public enum EstadoCredito
{
    Solicitado,
    Aprobado,
    Rechazado,
    Vigente,
    Cancelado
}

public class Credito : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public decimal Monto { get; private set; }
    public decimal TasaInteres { get; private set; }
    public int PlazoEnMeses { get; private set; }
    public EstadoCredito Estado { get; private set; }
    public DateTime FechaSolicitud { get; private set; }

    private Credito() : base()
    {
        // Para deserialización
    }

    public Credito(Guid id, Guid clienteId, decimal monto, decimal tasaInteres, int plazoEnMeses)
        : base(id)
    {
        ClienteId = clienteId;
        Monto = monto;
        TasaInteres = tasaInteres;
        PlazoEnMeses = plazoEnMeses;
        Estado = EstadoCredito.Solicitado;
        FechaSolicitud = DateTime.UtcNow;
    }

    public void Aprobar()
    {
        if (Estado != EstadoCredito.Solicitado)
            throw new InvalidOperationException("Solo se pueden aprobar créditos en estado Solicitado.");
        Estado = EstadoCredito.Aprobado;
    }

    public void Rechazar()
    {
        if (Estado != EstadoCredito.Solicitado)
            throw new InvalidOperationException("Solo se pueden rechazar créditos en estado Solicitado.");
        Estado = EstadoCredito.Rechazado;
    }

    public void Activar()
    {
        if (Estado != EstadoCredito.Aprobado)
            throw new InvalidOperationException("Solo se pueden activar créditos aprobados.");
        Estado = EstadoCredito.Vigente;
    }

    public void Cancelar()
    {
        if (Estado == EstadoCredito.Cancelado || Estado == EstadoCredito.Rechazado)
            throw new InvalidOperationException("El crédito ya está finalizado.");
        Estado = EstadoCredito.Cancelado;
    }
}
