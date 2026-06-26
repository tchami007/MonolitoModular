namespace MonolitoModular.SharedKernel.Domain;

/// <summary>
/// Contrato para seeders de datos por módulo.
/// El host los descubre por DI y los ejecuta en orden.
/// </summary>
public interface IDataSeeder
{
    /// <summary>Nombre del módulo — aparece en el log y en la respuesta del endpoint.</summary>
    string ModuleName { get; }

    /// <summary>Orden de ejecución. Menor número = primero (Clientes antes que Créditos).</summary>
    int Order { get; }

    /// <summary>
    /// Siembra datos. Debe ser idempotente:
    /// si los datos ya existen, no duplica ni lanza error.
    /// </summary>
    Task SeedAsync();
}
