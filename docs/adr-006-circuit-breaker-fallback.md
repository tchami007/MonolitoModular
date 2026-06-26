# ADR-006 — Conector Resiliente con Disyuntor (Circuit Breaker + Fallback)

## Ficha del ADR

| Atributo             | Valor                                          |
|----------------------|------------------------------------------------|
| Número               | ADR-006                                        |
| Táctica              | Conector Resiliente con Disyuntor y Fallback   |
| Catálogo de origen   | `docs/no_subir/adrs-conectores-ITERACION_4.md` |
| Rol Dominante        | CONSUMIDOR                                     |
| Granularidad         | INDIVIDUAL                                     |
| Gobernanza           | REGLAS_EXTERNAS                                |
| Sincronía            | SINCRÓNICO                                     |
| Módulo afectado      | `MonolitoModular.Creditos`                     |
| Fecha                | Junio 2026                                     |

---

## El Problema que Resuelve

Sin este patrón, cuando un sistema externo falla, **el fallo se propaga hacia adentro**.

Imaginá este escenario en el módulo `Creditos`: cada vez que un cliente solicita un crédito, el sistema llama a un buró de crédito externo (Equifax, Veraz, etc.) para obtener su score. Si el buró sufre una caída o degradación:

```
POST /api/creditos/solicitar
  └── CreditoService.SolicitarAsync()
        └── BureauCreditoHttpClient.ConsultarScoreAsync()
              └── [TIMEOUT 30s] HttpRequestException ──► 500 Internal Server Error
```

El usuario espera 30 segundos y recibe un error. Si son 100 usuarios simultáneos, el servidor acumula 100 threads esperando un timeout que nunca llega. El sistema completo se degrada aunque el problema sea exclusivamente del externo. Esto se llama **degradación en cascada**.

El Circuit Breaker corta este ciclo.

---

## Cómo Funciona el Patrón

El disyuntor es una **máquina de estados** que envuelve las llamadas al externo y decide si ejecutarlas o cortarlas.

```
                    N fallos >= umbral
     CERRADO ─────────────────────────────► ABIERTO
        ▲                                      │
        │   llamada exitosa                    │ cooldown expiró
        │                                      ▼
        └──────────────────────────── MEDIO_ABIERTO
                                   (1 llamada de prueba)
                                   fallo ──► ABIERTO
```

### Estado CERRADO (normal)
Las llamadas pasan directamente al proveedor externo. Si una llamada falla, se incrementa un contador interno. Cuando el contador alcanza el **umbral de fallos** (ej. 3), el circuito se abre.

### Estado ABIERTO (protegido)
Las llamadas ya **no llegan al externo**. El CB responde con el **fallback local** inmediatamente. Un timer interno mide el **tiempo de enfriamiento** (ej. 30 segundos). Durante este tiempo, el externo tiene oportunidad de recuperarse.

### Estado MEDIO_ABIERTO (prueba)
Cuando el tiempo de enfriamiento expira, el circuito pasa a estado de prueba. Deja pasar **una única llamada**:
- Si tiene **éxito** → el circuito vuelve a CERRADO (reset completo del contador).
- Si **falla** → el circuito vuelve a ABIERTO (reinicia el timer de enfriamiento).

---

## Cuándo Aplicarlo

Usá este patrón cuando se cumplan estas condiciones:

| Señal | Descripción |
|---|---|
| Hay una llamada síncrona a un externo | El sistema espera la respuesta del tercero para continuar |
| El externo puede fallar de forma intermitente | Caídas, timeouts, degradación del SLA |
| Tenés una alternativa parcial | Existe lógica local que puede sustituir el resultado externo (aunque sea aproximada) |
| La disponibilidad del sistema es crítica | No podés permitir que la caída del externo caiga todo tu sistema |

**No aplica cuando:**
- El resultado del externo es *imprescindible* y no hay fallback posible (firma digital notarial, por ejemplo).
- La llamada es asincrónica (usá Dead Letter Queue + reintentos en su lugar).
- El externo nunca falla en tu contexto (no hay costo, no vale la complejidad).

---

## Aplicación en MonolitoModular

### Escenario

Al solicitar un crédito, el módulo `Creditos` consulta un **buró de crédito externo** para obtener el score del cliente. Este score se guarda en la entidad `Credito` y acompaña la solicitud durante todo su ciclo de vida (aprobación/rechazo).

Si el buró falla:
- Sin CB: la solicitud falla con 500.
- **Con CB**: se calcula un **score local conservador** basado en el monto solicitado, el crédito se crea igual, y el campo `FuenteScore` informa que el score es un fallback. Un analista puede revisar después.

### Estructura de archivos

```
MonolitoModular.Creditos/
├── Domain/
│   ├── Entities/
│   │   └── Credito.cs                        ← MODIFICADO: agrega ScoreCredito + FuenteScore
│   └── ExternalServices/                     ← NUEVO: puerto hacia el externo
│       ├── IBureauCreditoService.cs           ← interfaz (el dominio solo conoce esto)
│       └── ResultadoBureau.cs                ← value object del resultado
├── Application/
│   ├── DTOs/
│   │   └── CreditoDto.cs                     ← MODIFICADO: expone ScoreCredito + FuenteScore
│   └── Services/
│       └── CreditoService.cs                 ← MODIFICADO: inyecta IBureauCreditoService
├── Infrastructure/
│   └── ExternalServices/                     ← NUEVO: implementaciones concretas
│       ├── BureauCreditoHttpClient.cs         ← cliente HTTP real (simula el externo)
│       └── CircuitBreakerBureauCreditoService.cs ← el disyuntor (Decorator sobre el HTTP)
└── CreditosModule.cs                         ← MODIFICADO: registra los servicios nuevos
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
```

El dominio nunca sabe que existe un Circuit Breaker ni un HttpClient. Solo conoce la interfaz.

---

### Código paso a paso

#### Paso 1 — El puerto del dominio

`Domain/ExternalServices/IBureauCreditoService.cs`

```csharp
namespace MonolitoModular.Creditos.Domain.ExternalServices;

public interface IBureauCreditoService
{
    Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado);
}
```

El dominio define **qué necesita**, no **cómo se obtiene**. Esta interfaz es la única pieza que `CreditoService` conoce del mundo exterior.

---

#### Paso 2 — El resultado del buró

`Domain/ExternalServices/ResultadoBureau.cs`

```csharp
public enum FuenteScore
{
    BureauExterno,   // Score real del buró
    FallbackLocal    // Score calculado localmente por el Circuit Breaker
}

public record ResultadoBureau(
    int Score,          // 0–1000
    FuenteScore Fuente, // Origen del score
    string Detalle      // Mensaje descriptivo
);
```

El enum `FuenteScore` es clave para la trazabilidad. Cuando el sistema opera bajo fallback, los analistas de riesgo pueden filtrar las solicitudes con `FuenteScore = FallbackLocal` para revisión manual.

---

#### Paso 3 — La entidad de dominio actualizada

`Domain/Entities/Credito.cs` — constructor relevante:

```csharp
public int ScoreCredito { get; private set; }
public string FuenteScore { get; private set; } = "SinEvaluar";

public Credito(Guid id, Guid clienteId, decimal monto, decimal tasaInteres, int plazoEnMeses,
               int scoreCredito = 0, string fuenteScore = "SinEvaluar")
    : base(id)
{
    // ... campos base ...
    ScoreCredito = scoreCredito;
    FuenteScore = fuenteScore;
}
```

Los parámetros `scoreCredito` y `fuenteScore` tienen valores por defecto. Esto permite que el seeder de datos (que crea créditos históricos sin consultar el buró) siga funcionando sin cambios.

---

#### Paso 4 — El cliente HTTP al buró externo

`Infrastructure/ExternalServices/BureauCreditoHttpClient.cs`

```csharp
public class BureauCreditoHttpClient : IBureauCreditoService
{
    private static readonly Random _rng = new();

    public Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal montoSolicitado)
    {
        // Simula caída del buró ~40% del tiempo — activa el Circuit Breaker en pocos intentos
        if (_rng.NextDouble() < 0.4)
            throw new HttpRequestException("Buró no disponible — timeout simulado.");

        var score = _rng.Next(300, 951);
        return Task.FromResult(new ResultadoBureau(
            Score:   score,
            Fuente:  FuenteScore.BureauExterno,
            Detalle: $"Score real del buró para cliente {clienteId}."
        ));
    }
}
```

En producción este método haría un `HttpClient.GetAsync(...)` real con deserialización JSON, headers de autenticación y timeout configurado. La simulación de fallos al 40% permite ver el Circuit Breaker en acción durante la demo sin necesidad de un servidor externo real.

---

#### Paso 5 — El Circuit Breaker (el núcleo del patrón)

`Infrastructure/ExternalServices/CircuitBreakerBureauCreditoService.cs`

```csharp
public class CircuitBreakerBureauCreditoService : IBureauCreditoService
{
    private readonly IBureauCreditoService _inner;  // BureauCreditoHttpClient
    private readonly int _umbralFallos;
    private readonly TimeSpan _tiempoEnfriamiento;

    private EstadoCircuito _estado = EstadoCircuito.Cerrado;
    private int _contadorFallos = 0;
    private DateTime? _abiertoEn = null;
    private readonly object _lock = new();          // thread-safe (es Singleton)

    public async Task<ResultadoBureau> ConsultarScoreAsync(Guid clienteId, decimal monto)
    {
        // ── Evaluación del estado ANTES de llamar ────────────────
        lock (_lock)
        {
            if (_estado == EstadoCircuito.Abierto)
            {
                if (DateTime.UtcNow - _abiertoEn >= _tiempoEnfriamiento)
                    _estado = EstadoCircuito.MedioAbierto;   // probar una vez
                else
                    return FallbackLocal(monto, "Circuito ABIERTO.");  // cortar aquí
            }
        }

        // ── Llamada al externo ───────────────────────────────────
        try
        {
            var resultado = await _inner.ConsultarScoreAsync(clienteId, monto);
            lock (_lock) { _estado = EstadoCircuito.Cerrado; _contadorFallos = 0; }
            return resultado;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _contadorFallos++;
                if (_contadorFallos >= _umbralFallos || _estado == EstadoCircuito.MedioAbierto)
                {
                    _estado = EstadoCircuito.Abierto;
                    _abiertoEn = DateTime.UtcNow;
                }
            }
            return FallbackLocal(monto, $"Fallo #{_contadorFallos}: {ex.Message}");
        }
    }

    // Scoring local conservador basado en monto (sin buró)
    private static ResultadoBureau FallbackLocal(decimal monto, string contexto)
    {
        var (score, criterio) = monto switch
        {
            <= 5_000m  => (700, "Monto conservador — riesgo bajo, aprobable"),
            <= 20_000m => (580, "Monto moderado — riesgo medio, revisión manual"),
            _          => (420, "Monto elevado — riesgo alto, probable rechazo")
        };
        return new ResultadoBureau(score, FuenteScore.FallbackLocal,
            $"[FALLBACK LOCAL] {criterio}. {contexto}");
    }
}
```

**Por qué el `lock`:** El CB es registrado como `Singleton` en el contenedor DI. Múltiples requests HTTP llegan en paralelo y acceden al mismo objeto. Sin lock, dos threads podrían leer `_contadorFallos = 2` al mismo tiempo, ambos incrementarlo a 3 simultáneamente, y ambos abrir el circuito — condición de carrera clásica.

**Por qué `Singleton` y no `Scoped`:** El estado del circuito (`_estado`, `_contadorFallos`, `_abiertoEn`) debe sobrevivir entre requests. Con `Scoped`, cada request crearía una instancia nueva, el contador se resetearía en cada llamada, y el Circuit Breaker nunca llegaría a abrirse.

---

#### Paso 6 — El servicio de aplicación integra el buró

`Application/Services/CreditoService.cs` — método `SolicitarAsync`:

```csharp
public async Task<CreditoDto> SolicitarAsync(SolicitarCreditoDto dto)
{
    // El CB decide internamente si llama al externo o usa el fallback.
    // CreditoService no sabe (ni le importa) cuál de los dos ocurrió.
    var resultado = await _bureauService.ConsultarScoreAsync(dto.ClienteId, dto.Monto);

    var credito = new Credito(
        id:           Guid.NewGuid(),
        clienteId:    dto.ClienteId,
        monto:        dto.Monto,
        tasaInteres:  dto.TasaInteres,
        plazoEnMeses: dto.PlazoEnMeses,
        scoreCredito: resultado.Score,
        fuenteScore:  resultado.Fuente.ToString()  // "BureauExterno" o "FallbackLocal"
    );

    await _repository.AgregarAsync(credito);
    return MapToDto(credito);
}
```

El `CreditoService` nunca lanza excepción por culpa del buró. Si el externo falla, el CB absorbe el error y `CreditoService` recibe un `ResultadoBureau` con `Fuente = FallbackLocal`. La solicitud se procesa siempre.

---

#### Paso 7 — Registro en el contenedor DI

`CreditosModule.cs`:

```csharp
// El cliente HTTP: implementación concreta (sin CB)
services.AddSingleton<BureauCreditoHttpClient>();

// El Circuit Breaker: Decorator que envuelve el cliente HTTP
// Singleton obligatorio: el estado del circuito debe sobrevivir entre requests
services.AddSingleton<IBureauCreditoService>(sp =>
    new CircuitBreakerBureauCreditoService(
        inner:              sp.GetRequiredService<BureauCreditoHttpClient>(),
        umbralFallos:       3,     // abre después de 3 fallos consecutivos
        tiempoEnfriamiento: TimeSpan.FromSeconds(30)  // espera 30s antes de probar
    ));
```

Cuando `CreditoService` (registrado como `Scoped`) recibe `IBureauCreditoService` por inyección, el contenedor le entrega el `CircuitBreakerBureauCreditoService` — que a su vez envuelve al `BureauCreditoHttpClient`. El servicio de aplicación nunca sabe nada de esto.

---

### Observación del comportamiento en ejecución

Con la aplicación corriendo, podés observar las tres fases del circuito llamando repetidamente a `POST /api/creditos/solicitar`:

| Llamadas | Comportamiento esperado | `FuenteScore` en respuesta |
|---|---|---|
| 1–N (buró ok ~60%) | Score real del buró | `BureauExterno` |
| Tras 3 fallos seguidos | Circuito ABIERTO — fallback inmediato | `FallbackLocal` |
| Durante 30s | Todas las respuestas son fallback local | `FallbackLocal` |
| Tras 30s (MedioAbierto) | Una llamada de prueba al buró real | `BureauExterno` o `FallbackLocal` |
| Prueba exitosa | Circuito CERRADO — vuelve a llamar al buró | `BureauExterno` |

---

## Referencia rápida con Polly

En producción, la implementación manual del Circuit Breaker se reemplaza por **Polly** — la biblioteca estándar de resiliencia en el ecosistema .NET.

```bash
dotnet add package Polly.Extensions.Http
```

```csharp
// En CreditosModule.cs con Polly:
services.AddHttpClient<IBureauCreditoService, BureauCreditoHttpClient>()
    .AddPolicyHandler(
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak:    (_, __) => Console.WriteLine("[CB] Circuito ABIERTO"),
                onReset:    ()      => Console.WriteLine("[CB] Circuito CERRADO"),
                onHalfOpen: ()      => Console.WriteLine("[CB] Circuito MEDIO-ABIERTO")
            )
    );
```

Polly agrega además:
- **Retry con backoff exponencial**: reintentar N veces antes de abrir el circuito.
- **Bulkhead isolation**: limitar el número de llamadas concurrentes al externo.
- **Timeout policy**: definir timeout independiente del HttpClient global.
- **Métricas y telemetría**: integración con OpenTelemetry para observabilidad.

La implementación manual en este ADR existe para entender el **mecanismo** por dentro. En proyectos reales, usar Polly es la decisión correcta.

---

## Trade-offs

### Pros

| Beneficio | Detalle |
|---|---|
| Protección ante degradación en cascada | El fallo del buró no arrastra al sistema completo |
| Respuesta inmediata en estado ABIERTO | No hay espera de timeouts; el usuario recibe respuesta en milisegundos |
| Libera threads de ejecución | Sin CB, 100 requests esperando timeout consumen 100 threads bloqueados |
| Trazabilidad del fallback | `FuenteScore = FallbackLocal` permite identificar créditos evaluados sin buró |
| Transparente para el cliente HTTP | El consumidor de la API recibe 200 OK siempre (no 500) |

### Contras

| Costo | Detalle |
|---|---|
| El fallback financiero es una decisión de riesgo | Otorgar crédito con score estimado puede implicar pérdidas si el score local es inexacto |
| Complejidad de concurrencia | El CB como Singleton requiere sincronización thread-safe (`lock`) |
| Estado en memoria | El estado del circuito se pierde si la aplicación se reinicia (en producción usar Redis para CB distribuido) |
| Fallo silencioso | El servicio "funciona" aunque el buró esté caído — los operadores deben monitorear activamente `FuenteScore` |

---

## Alternativas Descartadas

### Retry simple sin Circuit Breaker
Reintentar N veces cuando el buró falla. **Descartado** porque bajo caída del buró, los reintentos multiplican la carga sobre el externo ya degradado, acelerando su colapso total (thundering herd).

### Rechazar la solicitud cuando el buró falla
Devolver HTTP 503 si el buró no responde. **Descartado** en este contexto porque degrada la disponibilidad del sistema ante fallos de terceros — un módulo interno no debería depender de la disponibilidad de un externo para operar.

### ADR-003 Webhooks / Callbacks en lugar de llamada síncrona
Convertir la consulta al buró en un flujo asincrónico. **Descartado** para la solicitud inicial porque el usuario espera un score inmediato al momento de la solicitud. Válido para re-evaluaciones posteriores.

### ADR-009 Stateful Polling
Consultar el estado del buró en segundo plano. **Descartado** por la misma razón: el flujo de solicitud de crédito es síncrono e interactivo.

---

## Referencias

- Catálogo de tácticas: `docs/no_subir/adrs-conectores-ITERACION_4.md` — Táctica #6
- Implementación: `MonolitoModular/src/Modules/Creditos/`
- Leccion de arquitectura base: `docs/leccion1.md`
- Michael Nygard — *Release It!* (2007): capítulo original del patrón Circuit Breaker
- Polly documentation: [https://github.com/App-vNext/Polly](https://github.com/App-vNext/Polly)

---

*Fecha: Junio 2026 — Stack: .NET 8, C#, MonolitoModular*
