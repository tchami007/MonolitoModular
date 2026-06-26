# ADR-001 — Capa Anti-Corrupción (Anti-Corruption Layer)

## Ficha del ADR

| Atributo             | Valor                                          |
|----------------------|------------------------------------------------|
| Número               | ADR-001                                        |
| Táctica              | Anti-Corruption Layer (ACL)                    |
| Catálogo de origen   | `docs/no_subir/adrs-conectores-ITERACION_4.md` |
| Rol Dominante        | CONSUMIDOR                                     |
| Granularidad         | INDIVIDUAL                                     |
| Gobernanza           | REGLAS_EXTERNAS                                |
| Sincronía            | SINCRÓNICO                                     |
| Módulo afectado      | `MonolitoModular.Creditos`                     |
| Fecha                | Junio 2026                                     |

---

## El Problema que Resuelve

Cuando un sistema consume servicios externos —burós de crédito, procesadores de pagos,
registros regulatorios— el proveedor externo tiene su propio modelo de datos: sus propios
nombres de campos, sus propias escalas, sus propios códigos de estado. Si ese vocabulario
externo penetra el código del dominio, el dominio se **corrompe**.

Imaginá este escenario sin ACL explícito en el módulo `Creditos`:

```
POST /api/creditos/solicitar
  └── CreditoService.SolicitarAsync()
        └── BureauCreditoHttpClient.ConsultarScoreAsync()
              └── deserializa JSON del buró
                    └── usa PuntajeRiesgo (escala 1–950, nomenclatura del buró)
                          └── guarda directamente en Credito.ScoreCredito  ← CONTAMINACIÓN
```

El dominio ahora depende de que el buró siga usando esa escala y ese nombre de campo. Si el
buró migra a una API v2 con `score_normalizado` en escala 0–100, hay que tocar la entidad
`Credito`, `CreditoService`, y todos los tests de dominio. El cambio externo contamina
hacia adentro — esto es la **corrupción del modelo de dominio**.

Sin ACL explícito, el problema puede ser incluso más sutil: el código parece correcto
(compila, funciona), pero la separación de responsabilidades se erosiona silenciosamente.
En el ADR-006 (Circuit Breaker) quedó un ACL implícito: `BureauCreditoHttpClient`
traducía el esquema externo al modelo interno *inline*, mezclando dos responsabilidades
en el mismo método. Este ADR lo hace explícito y lo formaliza.

---

## Cómo Funciona el Patrón

El ACL establece una **frontera de traducción** entre el mundo externo y el dominio. Actúa
como un intérprete que conoce dos idiomas pero no los mezcla.

```
┌─────────────────────────────────────────────────────────┐
│                    DOMINIO INTERNO                      │
│                                                         │
│  CreditoService                                         │
│      └── IBureauCreditoService (puerto del dominio)     │
│              ResultadoBureau  ← modelo interno          │
│              (Score 0–1000, FuenteScore, Detalle)       │
└──────────────────────┬──────────────────────────────────┘
                       │  ▲ el dominio habla su propio idioma
     ╔═════════════════╪══╧═══════════════════════╗
     ║          CAPA ANTI-CORRUPCIÓN               ║
     ║                 │                           ║
     ║    BureauCreditoAcl.Traducir()              ║
     ║    BureauExternoResponse → ResultadoBureau  ║
     ║    (normaliza escala, mapea códigos)        ║
     ╚═════════════════╪═══════════════════════════╝
                       │  ▼ el externo habla su propio idioma
┌──────────────────────┴──────────────────────────────────┐
│                   MUNDO EXTERNO                         │
│                                                         │
│  BureauCreditoHttpClient (HTTP)                         │
│      BureauExternoResponse ← esquema externo            │
│      (PuntajeRiesgo 1–950, CodigoResultado, etc.)       │
└─────────────────────────────────────────────────────────┘
```

### Responsabilidad de cada pieza

**`BureauExternoResponse`** — la "lengua extranjera"
Representa el esquema de datos exactamente como el buró lo devuelve: su nomenclatura,
su escala de puntaje (1–950), sus códigos de resultado (`"APROBADO"`, `"NO_ENCONTRADO"`).
Esta clase **no pertenece al dominio**; pertenece a Infrastructure porque describe el
contrato de un sistema externo.

**`BureauCreditoAcl`** — el intérprete
Conoce ambos idiomas y hace la traducción. Es el único punto del código que sabe
simultáneamente cómo habla el buró y cómo habla el dominio. Si el buró cambia su API,
solo cambia esta clase.

**`ResultadoBureau`** — el modelo interno
El dominio define qué forma tiene el resultado que necesita: escala 0–1000, enum
`FuenteScore`, campo `Detalle`. Esta definición no cambia cuando el buró cambia.

### El ACL como traductor honesto

Un principio de diseño central de este ADR: **el ACL traduce, nunca decide**.

Si el buró responde `"NO_ENCONTRADO"`, el ACL no lanza una excepción de dominio ni
rechaza el crédito. Traduce ese estado al lenguaje del dominio (`Score = 0`, con el
contexto en `Detalle`) y devuelve un `ResultadoBureau` válido. La **decisión de negocio**
—qué hacer con un score 0— es responsabilidad de `CreditoService` (capa Application),
no del ACL.

Esta separación es crítica en DDD: mezclar la traducción con la política de negocio en
el ACL convertiría una clase de Infrastructure en tomadora de decisiones de dominio,
violando la regla de dependencias.

---

## Cuándo Aplicarlo

Usá este patrón cuando se cumplan estas condiciones:

| Señal | Descripción |
|---|---|
| El externo tiene su propio modelo de datos | Sus campos, escalas y nomenclatura difieren del dominio |
| El externo puede cambiar de versión | API v1 → v2, cambio de proveedor (Equifax → Veraz) |
| El dominio tiene invariantes que proteger | Escalas fijas, enums controlados, semántica específica |
| Múltiples partes del sistema consumen el externo | Sin ACL, la traducción se duplica y diverge |

**No aplica cuando:**
- El externo ya habla exactamente el mismo modelo que el dominio (raro pero posible).
- La integración es tan pequeña y estable que el overhead del ACL no se justifica.
- La llamada externa es de escritura pura (fire-and-forget) sin respuesta a modelar.

---

## Aplicación en MonolitoModular

### Escenario

Al solicitar un crédito, el módulo `Creditos` consulta un buró de crédito externo. El buró
devuelve su respuesta en **su propio esquema**: puntaje en escala 1–950, código de resultado
como string, timestamp propio. El dominio necesita un `ResultadoBureau` con score en escala
0–1000 y un enum `FuenteScore` controlado.

Antes de este ADR, `BureauCreditoHttpClient` hacía las dos cosas en el mismo método:
llamada HTTP y traducción inline. El ADR-001 separa esas responsabilidades creando un
traductor explícito.

### Estructura de archivos

```
MonolitoModular.Creditos/
├── Domain/
│   └── ExternalServices/
│       ├── IBureauCreditoService.cs     ← SIN CAMBIOS: puerto del dominio
│       └── ResultadoBureau.cs           ← SIN CAMBIOS: modelo interno del dominio
├── Infrastructure/
│   └── ExternalServices/
│       ├── BureauExternoResponse.cs     ← NUEVO: esquema externo del buró
│       ├── BureauCreditoAcl.cs          ← NUEVO: traductor puro (ACL explícito)
│       ├── BureauCreditoHttpClient.cs   ← MODIFICADO: solo HTTP, delega traducción al ACL
│       └── CircuitBreakerBureauCreditoService.cs  ← SIN CAMBIOS
└── CreditosModule.cs                    ← MODIFICADO: registra BureauCreditoAcl
```

**Regla de dependencias respetada:**

```
CreditoService (Application)
    └── depende de IBureauCreditoService (Domain/ExternalServices)
                          ▲
                          │ implementa
           CircuitBreakerBureauCreditoService (Infrastructure)
                          │ envuelve
               BureauCreditoHttpClient (Infrastructure)
                          │ delega traducción a
                   BureauCreditoAcl (Infrastructure)
                          │ produce
               ResultadoBureau (Domain/ExternalServices)
```

El dominio no sabe que existe un ACL, un HTTP client ni un Circuit Breaker.
Solo conoce `IBureauCreditoService` y `ResultadoBureau`.

---

### Código paso a paso

#### Paso 1 — El puerto del dominio (sin cambios)

`Domain/ExternalServices/IBureauCreditoService.cs`

```csharp
namespace MonolitoModular.Creditos.Domain.ExternalServices;

public interface IBureauCreditoService
{
    Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado);
}
```

Esta interfaz ya existía desde ADR-006. No cambia. Es el contrato que define el dominio
en su propio idioma: "dame un `ResultadoBureau`". No sabe —ni le importa— que detrás hay
un buró externo con su propia escala y sus propios códigos.

---

#### Paso 2 — El modelo interno del dominio (sin cambios)

`Domain/ExternalServices/ResultadoBureau.cs`

```csharp
public enum FuenteScore
{
    BureauExterno,   // Score real del buró, traducido por el ACL
    FallbackLocal    // Score calculado localmente por el Circuit Breaker
}

public record ResultadoBureau(
    int Score,          // 0–1000  ← escala del dominio, no del buró
    FuenteScore Fuente, // Origen del score
    string Detalle      // Mensaje descriptivo
);
```

El dominio define su propia escala (0–1000) y su propio enum. Si el buró usa escala
1–950, eso es problema del ACL, no del dominio.

---

#### Paso 3 — El esquema externo (nuevo)

`Infrastructure/ExternalServices/BureauExternoResponse.cs`

```csharp
namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// Esquema de la respuesta cruda del buró de crédito externo.
/// Representa los datos tal como el buró los devuelve — su nomenclatura,
/// su escala, sus códigos. Esta clase NO pertenece al dominio.
///
/// Si el buró migra de API, solo cambia esta clase (y el ACL que la traduce).
/// El dominio (ResultadoBureau) permanece intacto.
public class BureauExternoResponse
{
    /// Identificador del cliente en el sistema del buró.
    public string CodigoCliente { get; init; } = string.Empty;

    /// Puntaje de riesgo en la escala propia del buró: 1–950.
    /// Distinto a la escala del dominio (0–1000); la normalización ocurre en el ACL.
    public int PuntajeRiesgo { get; init; }

    /// Resultado de la consulta según los códigos del buró externo.
    /// Valores posibles: "APROBADO", "NO_ENCONTRADO", "ERROR_TIMEOUT", "DATOS_INSUFICIENTES".
    public string CodigoResultado { get; init; } = string.Empty;

    /// Descripción textual del resultado en el formato del buró.
    public string DescripcionResultado { get; init; } = string.Empty;

    /// Timestamp de la consulta según el servidor del buró (ISO 8601).
    public string FechaConsulta { get; init; } = string.Empty;
}
```

Esta clase es la "lengua extranjera". Existe exclusivamente en Infrastructure porque
describe el contrato de un sistema externo, no un concepto del dominio.

---

#### Paso 4 — El traductor ACL (nuevo)

`Infrastructure/ExternalServices/BureauCreditoAcl.cs`

```csharp
using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// Anti-Corruption Layer del módulo Créditos para el buró externo.
///
/// Responsabilidad única: traducir BureauExternoResponse → ResultadoBureau.
/// Es el único punto del código que conoce simultáneamente ambos modelos.
///
/// Principio de diseño: el ACL traduce, nunca decide.
/// Si el buró responde "NO_ENCONTRADO", el ACL devuelve Score=0 con contexto
/// en Detalle. La política de negocio (qué hacer con score=0) es responsabilidad
/// de CreditoService (Application), no del ACL (Infrastructure).
public class BureauCreditoAcl
{
    /// Traduce la respuesta cruda del buró al modelo de dominio interno.
    /// Nunca lanza excepciones: siempre devuelve un ResultadoBureau válido.
    public ResultadoBureau Traducir(BureauExternoResponse respuesta)
    {
        if (respuesta.CodigoResultado == "APROBADO")
            return new ResultadoBureau(
                Score:   NormalizarPuntaje(respuesta.PuntajeRiesgo),
                Fuente:  FuenteScore.BureauExterno,
                Detalle: $"[ACL] Evaluación exitosa: {respuesta.DescripcionResultado}"
            );

        // Cualquier otro estado externo se traduce a Score=0.
        // El dominio entiende Score=0 como "sin puntaje válido".
        // Qué hacer con ese resultado es una decisión de CreditoService.
        return new ResultadoBureau(
            Score:   0,
            Fuente:  FuenteScore.BureauExterno,
            Detalle: $"[ACL] Estado no aprobatorio ({respuesta.CodigoResultado}): " +
                     $"{respuesta.DescripcionResultado}"
        );
    }

    /// Convierte la escala del buró (1–950) a la escala interna del dominio (0–1000).
    /// Si el buró cambia su escala en una versión futura de la API, solo cambia esta línea.
    private static int NormalizarPuntaje(int puntajeExterno)
        => (int)Math.Round(puntajeExterno / 950.0 * 1000);
}
```

**Por qué el ACL no lanza excepciones de dominio:** si `BureauCreditoAcl` lanzara
`ClienteNoEncontradoEnBureauException` cuando `CodigoResultado == "NO_ENCONTRADO"`,
estaría tomando una decisión de negocio desde Infrastructure. Eso viola la regla de
dependencias de DDD: Infrastructure no debe imponer políticas al dominio. El ACL traduce
fielmente y entrega el resultado; la capa de Aplicación decide qué significa.

---

#### Paso 5 — El adaptador HTTP refactorizado

`Infrastructure/ExternalServices/BureauCreditoHttpClient.cs`

```csharp
using MonolitoModular.Creditos.Domain.ExternalServices;

namespace MonolitoModular.Creditos.Infrastructure.ExternalServices;

/// Adaptador HTTP al buró de crédito externo.
///
/// Responsabilidad: comunicación de red — obtener la respuesta cruda del buró.
/// La traducción al modelo de dominio es responsabilidad del BureauCreditoAcl.
///
/// Antes de ADR-001: mezclaba HTTP + traducción en el mismo método.
/// Después de ADR-001: dos pasos explícitos y separados.
public class BureauCreditoHttpClient : IBureauCreditoService
{
    private static readonly Random _rng = new();
    private readonly BureauCreditoAcl _acl;

    public BureauCreditoHttpClient(BureauCreditoAcl acl)
    {
        _acl = acl;
    }

    public Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado)
    {
        // Simula degradación o caída del buró externo ~40% del tiempo.
        if (_rng.NextDouble() < 0.4)
            throw new HttpRequestException(
                "Buró de crédito no disponible — timeout de conexión simulado.");

        // Paso 1: obtener respuesta cruda en el esquema externo del buró.
        var respuestaExterna = SimularRespuestaExterna(clienteId);

        // Paso 2: traducir al modelo de dominio a través del ACL.
        var resultado = _acl.Traducir(respuestaExterna);

        return Task.FromResult(resultado);
    }

    /// Simula la respuesta raw del buró en su propio esquema (escala 1–950).
    /// En producción: HttpClient.GetAsync(...) + JsonSerializer.Deserialize<BureauExternoResponse>()
    private static BureauExternoResponse SimularRespuestaExterna(Guid clienteId)
    {
        var puntaje = _rng.Next(285, 903);  // escala real del buró: 1–950
        return new BureauExternoResponse
        {
            CodigoCliente       = clienteId.ToString(),
            PuntajeRiesgo       = puntaje,
            CodigoResultado     = "APROBADO",
            DescripcionResultado = $"Evaluación crediticia completada para cliente {clienteId}.",
            FechaConsulta       = DateTime.UtcNow.ToString("o")
        };
    }
}
```

Los **dos pasos ahora son visibles** como código separado. Un nuevo desarrollador que
lee este método sabe de inmediato que hay dos responsabilidades distintas: obtener el dato
externo y traducirlo al modelo del dominio.

---

#### Paso 6 — El servicio de aplicación no cambia (por diseño)

`Application/Services/CreditoService.cs` — método `SolicitarAsync`:

```csharp
public async Task<CreditoDto> SolicitarAsync(SolicitarCreditoDto dto)
{
    // CreditoService sigue ignorando todo lo que está debajo de IBureauCreditoService.
    // No sabe que existe un ACL, un HTTP client ni un Circuit Breaker.
    var resultado = await _bureauService.ConsultarScoreAsync(dto.ClienteId, dto.Monto);

    // La política de negocio sobre Score=0 vive aquí, en Application.
    // Por ahora: crear el crédito con score=0; FuenteScore="BureauExterno"
    // identifica que fue una respuesta real (aunque no aprobatoria).
    // Una extensión futura podría rechazar automáticamente si Score==0 y
    // Fuente==BureauExterno (cliente no encontrado en el buró).
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
```

Que `CreditoService` no cambie no es un accidente —  es el objetivo del patrón. El dominio
permanece estable aunque el buró cambie su API, su escala o su proveedor. El ACL absorbe
ese cambio.

---

#### Paso 7 — Registro en el contenedor DI

`CreditosModule.cs`:

```csharp
// ── ACL: traductor entre el buró externo y el dominio ────────────────────
// Singleton: no tiene estado mutable; reutilizable entre requests.
services.AddSingleton<BureauCreditoAcl>();

// ── Circuit Breaker para el Buró de Crédito ──────────────────────────────
// BureauCreditoHttpClient recibe el ACL por constructor.
services.AddSingleton<BureauCreditoHttpClient>();
services.AddSingleton<IBureauCreditoService>(sp =>
    new CircuitBreakerBureauCreditoService(
        inner:              sp.GetRequiredService<BureauCreditoHttpClient>(),
        umbralFallos:       3,
        tiempoEnfriamiento: TimeSpan.FromSeconds(30)
    ));
```

El `BureauCreditoAcl` se registra como `Singleton` porque no tiene estado mutable: es
una función de traducción pura. Puede ser reutilizado sin riesgo entre requests concurrentes.

---

### Dónde NO vive la lógica de negocio

Esta sección contrasta ADR-001 y ADR-006 para dejar clara la separación de
responsabilidades:

| Capa | Clase | Responsabilidad | ¿Toma decisiones de negocio? |
|---|---|---|---|
| Domain | `IBureauCreditoService` | Define el contrato que el dominio necesita | — |
| Domain | `ResultadoBureau` | Modelo interno del resultado | — |
| Infrastructure | `BureauExternoResponse` | Esquema externo del buró | No |
| Infrastructure | `BureauCreditoAcl` | **Traduce** externo → interno | **No** — solo traduce |
| Infrastructure | `BureauCreditoHttpClient` | Comunicación HTTP con el buró | No |
| Infrastructure | `CircuitBreakerBureauCreditoService` | Resiliencia (CB + fallback) | No |
| **Application** | **`CreditoService`** | **Orquesta la solicitud de crédito** | **Sí** |

Las políticas de negocio ("si score=0 rechazar automáticamente", "si fuente=FallbackLocal
requiere revisión manual") viven en `CreditoService`. Ni el ACL ni el Circuit Breaker
deciden si un crédito se aprueba o rechaza.

---

## Relación con ADR-006 (Circuit Breaker)

ADR-001 y ADR-006 resuelven problemas distintos y se componen sin solapamiento:

| | ADR-001 ACL | ADR-006 Circuit Breaker |
|---|---|---|
| **Problema que resuelve** | Contaminación del modelo de dominio por vocabulario externo | Degradación en cascada por fallo del externo |
| **Cuándo actúa** | Siempre (en cada llamada exitosa) | Solo cuando el externo falla |
| **Qué produce** | Traducción del esquema externo al modelo del dominio | Resiliencia: fallback cuando el externo no responde |
| **Responsabilidad** | Semántica (qué significan los datos) | Disponibilidad (si la llamada se hace o no) |

En la cadena de ejecución, el CB actúa **antes** de que el ACL entre en juego:

```
CircuitBreakerBureauCreditoService
    │
    ├── [circuito ABIERTO]  → FallbackLocal (el ACL no interviene)
    │
    └── [circuito CERRADO]  → BureauCreditoHttpClient
                                  │
                                  ├── Paso 1: HTTP → BureauExternoResponse
                                  └── Paso 2: ACL  → ResultadoBureau
```

El fallback del CB produce un `ResultadoBureau` directamente con `FuenteScore.FallbackLocal`,
sin pasar por el ACL. Esto es correcto: el fallback no viene del externo, no hay nada que
traducir.

---

## Trade-offs

### Pros

| Beneficio | Detalle |
|---|---|
| Aislamiento ante cambios del proveedor | Si el buró cambia su API, solo cambia `BureauExternoResponse` + `BureauCreditoAcl` |
| Dominio estable | `ResultadoBureau`, `Credito`, `CreditoService` no se tocan si cambia el externo |
| Responsabilidad única visible en el código | `BureauCreditoHttpClient` hace HTTP; `BureauCreditoAcl` traduce |
| Facilita el testing | El ACL puede testearse con datos de prueba sin HTTP; el HTTP client puede testearse sin lógica de dominio |
| Sustitución de proveedor localizada | Cambiar de Equifax a Veraz implica una nueva `BureauExternoResponse` y un ACL nuevo — sin tocar el dominio |

### Contras

| Costo | Detalle |
|---|---|
| Más archivos | Se agregan `BureauExternoResponse` y `BureauCreditoAcl` — dos clases más para mantener |
| Overhead de traducción | En llamadas de alto volumen, la conversión de tipos agrega un costo mínimo pero no nulo |
| Puede ser sobrediseño en integraciones simples | Si el externo solo devuelve un número entero, un ACL completo es excesivo |

---

## Alternativas Descartadas

### Usar el esquema externo directamente en el dominio
Guardar `PuntajeRiesgo` (escala 1–950 del buró) en `Credito.ScoreCredito` sin normalizar.
**Descartado** porque acopla el dominio a la escala del proveedor. Si el buró cambia su
escala, hay que migrar datos históricos y modificar reglas de negocio que dependen del
rango del score.

### Traducción inline en `BureauCreditoHttpClient`
Era el estado antes de este ADR: la conversión ocurría dentro de `ConsultarScoreAsync`,
mezclada con la lógica HTTP. **Descartado** porque viola el principio de responsabilidad
única: el cliente HTTP no debería conocer el modelo de dominio. Esto también dificulta
el testing aislado de cada responsabilidad.

### Traducción en `CreditoService` (Application)
Recibir el `BureauExternoResponse` directamente en la capa de aplicación y traducirlo allí.
**Descartado** porque expone el vocabulario externo hacia arriba en la jerarquía: la capa
de aplicación termina sabiendo que existe un campo `PuntajeRiesgo` con escala 1–950 —
exactamente la corrupción que el patrón previene.

---

## Referencias

- Catálogo de tácticas: `docs/no_subir/adrs-conectores-ITERACION_4.md` — Táctica #1
- ADR relacionado: `docs/adr-006-circuit-breaker-fallback.md` — CB + Fallback (precondición)
- Implementación: `MonolitoModular/src/Modules/Creditos/`
- Leccion de arquitectura base: `docs/leccion1.md`
- Eric Evans — *Domain-Driven Design* (2003): capítulo 14, "Maintaining Model Integrity"
- Martin Fowler — *Patterns of Enterprise Application Architecture* (2002): "Anticorruption Layer"

---

*Fecha: Junio 2026 — Stack: .NET 8, C#, MonolitoModular*
