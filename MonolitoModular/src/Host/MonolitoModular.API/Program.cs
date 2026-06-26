using System.Reflection;
using MonolitoModular.CajaDeAhorro;
using MonolitoModular.Clientes;
using MonolitoModular.Creditos;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// Lectura de configuración de módulos activos
// -----------------------------------------------------------------------
var modulesConfig = builder.Configuration.GetSection("Modules");
bool IsModuleEnabled(string name) =>
    modulesConfig.GetValue<bool>(name, defaultValue: false);

// -----------------------------------------------------------------------
// Registro de módulos en DI
// Cada módulo se auto-registra mediante su extension method.
// El host no conoce los detalles internos de cada módulo.
// -----------------------------------------------------------------------
var activeAssemblies = new List<Assembly>();

if (IsModuleEnabled("Clientes"))
{
    builder.Services.AddClientesModule();
    activeAssemblies.Add(typeof(MonolitoModular.Clientes.ClientesModule).Assembly);
}

if (IsModuleEnabled("Creditos"))
{
    builder.Services.AddCreditosModule();
    activeAssemblies.Add(typeof(MonolitoModular.Creditos.CreditosModule).Assembly);
}

if (IsModuleEnabled("CajaDeAhorro"))
{
    builder.Services.AddCajaDeAhorroModule();
    activeAssemblies.Add(typeof(MonolitoModular.CajaDeAhorro.CajaDeAhorroModule).Assembly);
}

// -----------------------------------------------------------------------
// MVC: los controllers viven en los módulos, no en el host.
// AddApplicationPart le indica a ASP.NET Core que busque controllers
// en los assemblies de cada módulo activo.
// -----------------------------------------------------------------------
var mvcBuilder = builder.Services.AddControllers();
foreach (var assembly in activeAssemblies)
{
    mvcBuilder.AddApplicationPart(assembly);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Monolito Modular API", Version = "v1" });
});

// -----------------------------------------------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Log de módulos activos al iniciar
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Módulos activos: {Modulos}",
    string.Join(", ", new[] { "Clientes", "Creditos", "CajaDeAhorro" }
        .Where(IsModuleEnabled)));

app.Run();
