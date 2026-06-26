# Lección 1 — Decisiones de Arquitectura: Monolito Modular

## Contexto

Esta lección documenta la discusión arquitectónica previa a la construcción de una aplicación de práctica en .NET 8. El objetivo es dejar registro de las decisiones tomadas, las alternativas descartadas y el razonamiento detrás de cada elección.

La aplicación es **demostrativa y educativa**, con tres módulos simulados: **Clientes**, **Créditos** y **Caja de Ahorro**.

---

## El escenario real del negocio

El equipo es **proveedor de software on-premise**. Las características del contexto son:

- El cliente recibe la aplicación y la corre en su propia infraestructura.
- Existen actualmente **5 clientes**, cada uno con una versión diferente del producto.
- Cada cliente puede tener **diferentes módulos activos**.
- Las actualizaciones son **frecuentes** (varios envíos por mes).
- Cada cliente **evoluciona el sistema en forma independiente** — puede haber funcionalidad compartida pero también cambios exclusivos por cliente.
- El proveedor instala las personalizaciones usando una **herramienta propia de deployment**.

Este escenario corresponde a lo que en ingeniería de software se conoce como **Software Product Line (SPL)**: un producto base con variantes controladas por cliente.

---

## Conceptos clave discutidos

### Monorepo vs. Monolito Modular

Son conceptos **ortogonales** que frecuentemente se confunden.

| Concepto | Qué es | Ejemplo |
|---|---|---|
| **Monorepo** | Estrategia de repositorio | Frontend + Backend + Mobile en un solo repo Git |
| **Monolito Modular** | Patrón arquitectónico | Una sola aplicación deployable, dividida internamente en módulos |

La aplicación que se construye en esta lección es:
- **Un solo repositorio Git**
- **Una sola solución .NET** (`.sln`)
- **Un solo artefacto deployable**
- Con **arquitectura interna modular**

No es un monorepo.

---

### Deployment granularity: la pregunta clave

La pregunta que disparó la discusión fue:

> "Cuando hay modificaciones en un módulo, ¿tengo que enviar todo el paquete al cliente o puedo mandar solo ese módulo?"

La respuesta depende de la arquitectura elegida:

| Arquitectura | Deploy independiente por módulo | Complejidad |
|---|---|---|
| Monolito clásico | No — se deploya todo junto | Baja |
| Monolito modular estándar | No — un solo artefacto | Baja-Media |
| Monolito modular con DLLs separadas | Sí — se puede enviar solo la DLL del módulo | Media-Alta |
| Microservicios | Sí — cada servicio es independiente | Alta |

---

## Alternativas arquitectónicas evaluadas

### Opción 1: Plugin / Assembly Loading

```
/app
├── host.exe                ← núcleo estable, rara vez cambia
├── modules/
│   ├── Creditos.dll        ← se reemplaza solo este al actualizar
│   └── Clientes.dll
└── migrations/
    ├── Creditos/           ← scripts de migración por módulo
    └── Clientes/
```

**Pros:**
- Se envía solo la DLL + scripts de migración del módulo que cambió.
- Máxima granularidad de deployment.

**Contras:**
- Complejidad alta: versioning de assemblies, compatibilidad entre módulos.
- Las migraciones de base de datos siguen requiriendo coordinación.
- Requiere un mecanismo de carga dinámica de assemblies (ej. `AssemblyLoadContext` en .NET).

---

### Opción 2: Microservicios on-premise

```
cliente-servidor/
├── Clientes.exe   :5001    ← proceso independiente
├── Creditos.exe   :5002    ← proceso independiente
└── CajaDeAhorro.exe :5003  ← proceso independiente
```

**Pros:**
- Verdadera independencia de deployment.
- Cada módulo tiene su propio ciclo de vida.

**Contras:**
- El cliente necesita correr y gestionar múltiples procesos.
- La comunicación entre módulos se vuelve compleja (HTTP, mensajería).
- Infraestructura on-premise más pesada y difícil de operar.
- **Descartado** por complejidad operacional excesiva para el escenario actual.

---

### Opción 3: Paquete completo con módulos activables por configuración

```
app.exe  ← contiene todos los módulos compilados

appsettings.json:
  "Modules": {
    "Creditos": true,
    "CajaDeAhorro": false
  }
```

**Pros:**
- Un solo artefacto. Simple de deployar on-premise.
- La configuración determina qué módulos están activos por cliente.

**Contras:**
- Todos los clientes reciben todo el código (incluso módulos que no usan).
- Cada entrega es el paquete completo aunque cambie un solo módulo.

---

## Decisión adoptada

**Monolito modular donde cada módulo es un proyecto `.csproj` separado**, con activación por configuración.

```
MonolitoModular.sln
└── src/
    ├── Host/
    │   └── MonolitoModular.API/           ← entry point, composición de DI
    ├── Modules/
    │   ├── MonolitoModular.Clientes/      ← class library (.csproj separado)
    │   ├── MonolitoModular.Creditos/      ← class library (.csproj separado)
    │   └── MonolitoModular.CajaDeAhorro/  ← class library (.csproj separado)
    └── Shared/
        └── MonolitoModular.SharedKernel/  ← contratos comunes
```

### Por qué esta decisión

| Decisión | Razonamiento |
|---|---|
| Módulo = `.csproj` separado | Enforcea los boundaries reales. Permite a la herramienta de deployment enviar solo la DLL del módulo que cambió. |
| Activación por configuración | Soporta el escenario de clientes con diferentes módulos activos sin cambiar el código. |
| Cada módulo registra su propio DI | El host no conoce los detalles internos de cada módulo. Bajo acoplamiento. |
| Interfaces en SharedKernel | Contratos estables entre módulos y host. Protege contra cambios breaking entre versiones. |
| Estructura de migrations por módulo | Deja la puerta abierta a correr solo las migraciones del módulo actualizado. |

---

## Estructura interna de cada módulo

Cada módulo sigue una arquitectura de **4 capas** implementadas como carpetas dentro del `.csproj`:

```
MonolitoModular.Clientes/
├── Domain/
│   ├── Entities/           ← Cliente.cs (objeto de dominio)
│   └── Repositories/       ← IClienteRepository.cs (interfaz, no implementación)
├── Application/
│   ├── DTOs/               ← ClienteDto.cs
│   └── Services/           ← IClienteService.cs, ClienteService.cs
├── Infrastructure/
│   └── Repositories/       ← ClienteRepository.cs (implementación in-memory)
└── Presentation/
    └── Controllers/        ← ClientesController.cs
```

**Regla de dependencias (Dependency Rule):**

```
Presentation → Application → Domain
Infrastructure → Domain (implementa interfaces del Domain)
```

- `Domain` no depende de nadie.
- `Application` solo depende de `Domain`.
- `Infrastructure` implementa las interfaces definidas en `Domain`.
- `Presentation` orquesta usando `Application`.

---

## Alcance de la aplicación de práctica

### Incluido

- Estructura de solución multi-proyecto.
- 3 módulos: Clientes, Créditos, Caja de Ahorro.
- Persistencia in-memory (listas/diccionarios) — foco en arquitectura, no en infraestructura.
- Activación de módulos por configuración (`appsettings.json`).
- Cada módulo registra su propio DI mediante extension methods.
- Controllers dentro de cada módulo, descubiertos por el host via `AddApplicationPart`.
- SharedKernel con clase base `Entity<TId>`.

### Fuera de scope (próximos pasos documentados)

| Tema | Descripción |
|---|---|
| Assembly loading dinámico | Plugin architecture completa con `AssemblyLoadContext` |
| Versionado semántico por módulo | SemVer independiente por `.csproj` |
| Runner de migraciones por módulo | Correr solo las migraciones del módulo actualizado |
| Comunicación entre módulos | Contratos via interfaces del SharedKernel o eventos de dominio |
| CQRS / Mediator | `MediatR` para separar commands y queries |
| Autenticación / Autorización | No incluida en esta iteración |

---

## Reflexión final

> La arquitectura no resuelve sola el problema de versioning por cliente. La herramienta de deployment es la que hoy hace el trabajo pesado de eso. La arquitectura modular le da a esa herramienta la **granularidad necesaria** para operar a nivel de módulo.

El código bien estructurado en módulos independientes es una condición necesaria pero no suficiente. El proceso de release management, el versionado por cliente y la gestión de migraciones son problemas que deben resolverse en conjunto con la arquitectura.

---

*Fecha de discusión: junio 2026*  
*Stack: .NET 8, C#, arquitectura en capas, monolito modular*
