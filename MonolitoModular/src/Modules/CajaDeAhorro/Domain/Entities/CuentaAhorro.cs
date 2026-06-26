using MonolitoModular.SharedKernel.Domain;

namespace MonolitoModular.CajaDeAhorro.Domain.Entities;

public class CuentaAhorro : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public string NumeroCuenta { get; private set; }
    public decimal Saldo { get; private set; }
    public DateTime FechaApertura { get; private set; }
    public bool Activa { get; private set; }

    private CuentaAhorro() : base()
    {
        NumeroCuenta = string.Empty;
    }

    public CuentaAhorro(Guid id, Guid clienteId, string numeroCuenta)
        : base(id)
    {
        ClienteId = clienteId;
        NumeroCuenta = numeroCuenta;
        Saldo = 0;
        FechaApertura = DateTime.UtcNow;
        Activa = true;
    }

    public void Depositar(decimal monto)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto del depósito debe ser mayor a cero.");
        if (!Activa)
            throw new InvalidOperationException("No se puede operar sobre una cuenta inactiva.");
        Saldo += monto;
    }

    public void Extraer(decimal monto)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto de extracción debe ser mayor a cero.");
        if (!Activa)
            throw new InvalidOperationException("No se puede operar sobre una cuenta inactiva.");
        if (monto > Saldo)
            throw new InvalidOperationException("Saldo insuficiente.");
        Saldo -= monto;
    }

    public void Cerrar()
    {
        if (!Activa)
            throw new InvalidOperationException("La cuenta ya está cerrada.");
        Activa = false;
    }
}
