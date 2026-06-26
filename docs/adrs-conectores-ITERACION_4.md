# Catálogo de Tácticas Arquitectónicas para Conectores Externos en Sistemas Financieros

Este documento sirve como un catálogo razonado de tácticas de diseño de software destinadas a estructurar los **Registros de Decisión de Arquitectura (ADRs)** en el ámbito de integraciones, pasarelas y conexiones con terceros dentro de un ecosistema financiero (Bancos, Fintechs, Pasarelas de Pago y Reguladores).

---

## 1. Modelo de Taxonomía Core

Para clasificar de manera unívoca cada táctica y facilitar la toma de decisiones en los ADRs, se establece una taxonomía multidimensional basada en cuatro ejes críticos:

* **Rol Dominante:** `[CONSUMIDOR | PROVEEDOR | BIDIRECCIONAL]` -> Define la dirección de la iniciativa de la petición o la naturaleza de la responsabilidad principal.
* **Granularidad del Dato:** `[INDIVIDUAL | LOTE | STREAMING]` -> Define el volumen y la frecuencia de la información transferida en una única unidad de trabajo.
* **Gobernanza del Contrato:** `[REGLAS_EXTERNAS | REGLAS_INTERNAS | REGLAS_ESTÁNDAR]` -> Define quién tiene la autoridad sobre el ciclo de vida, formato y evolución del contrato de la interfaz (API, Archivo, Evento).
* **Sincronía de Red:** `[SINCRÓNICO | ASINCRÓNICO]` -> Define el acoplamiento temporal entre el sistema emisor y el receptor.

---

## 2. Catálogo Detallado de las 14 Tácticas

### 1. Capa de Anticorrupción (Anti-Corruption Layer - ACL)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_EXTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Interpone un subsistema de traducción entre el modelo de dominio interno (limpio) y las semánticas o esquemas de datos del proveedor externo. Evita que conceptos desalineados o de tecnología obsoleta contaminen la arquitectura core.
* **Caso de Uso Financiero:** Consumo de un buró de crédito tradicional (ej. Equifax, Veraz) o sistemas gubernamentales de validación de identidad cuyos contratos devuelven estructuras profundamente anidadas, códigos de error ambiguos o formatos no estándar (XML/SOAP legados).
* **Compromisos (Trade-offs):**
    * *Pros:* Aislamiento total del dominio de negocio; facilidad para cambiar de proveedor externo en el futuro modificando solo el ACL.
    * *Contras:* Introduce una capa adicional de cómputo y latencia (mínima, pero existente); requiere mantenimiento doble si el externo cambia drásticamente.

### 2. Intercambio por Lotes Seguro (Secure Batch Ingestion / SFTP + ETL)
* **Taxonomía:** `[Rol: BIDIRECCIONAL]` | `[Granularidad: LOTE]` | `[Gobernanza: REGLAS_ESTÁNDAR]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Procesamiento diferido de grandes volúmenes de registros consolidados en archivos planos estructurados, los cuales se transfieren mediante canales cifrados seguros y se validan/procesan mediante herramientas de extracción, transformación y carga (ETL).
* **Caso de Uso Financiero:** Procesos de compensación bancaria nocturna (Clearing interbancario de la cámara compensadora), conciliación masiva diaria de movimientos con redes de tarjetas de crédito (Visa/Mastercard), o envío periódico de balances de saldos al Banco Central o ente regulador.
* **Compromisos (Trade-offs):**
    * *Pros:* Alta eficiencia para procesar millones de registros; desacoplamiento temporal completo; menor consumo de recursos de red en horas pico.
    * *Contras:* No provee visibilidad en tiempo real; la gestión de errores requiere reprocesamiento de archivos enteros o manejo complejo de registros rechazados.

### 3. Distribución por Suscripción de Eventos (Webhooks / Callbacks)
* **Taxonomía:** `[Rol: PROVEEDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_EXTERNAS]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Exposición de endpoints HTTP pasivos para permitir que un sistema externo empuje notificaciones en tiempo real inmediatamente después de que ocurra un cambio de estado en su perímetro, eliminando la necesidad de consultas recurrentes.
* **Caso de Uso Financiero:** Recepción de notificaciones de una pasarela de pagos internacional (ej. Stripe, dLocal) informando que un pago iniciado por el usuario en efectivo o transferencia fue finalmente liquidado en una ventanilla física.
* **Compromisos (Trade-offs):**
    * *Pros:* Uso eficiente de recursos (cero procesamiento desperdiciado en polling); comunicación casi inmediata.
    * *Contras:* Requiere que el endpoint sea público (exige alta seguridad); dependes de las políticas de reintento del tercero; propenso a problemas de ordenamiento de red.

### 4. Fachada de API Segura (API Gateway con Inyección de Políticas)
* **Taxonomía:** `[Rol: PROVEEDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_INTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Centralización perimetral de la exposición de servicios internos mediante una compuerta que aplica políticas transversales estrictas de seguridad (OAuth2, mTLS, validación de firmas de mensajes), cuotas de consumo, auditoría inalterable y abstracción de la topología interna.
* **Caso de Uso Financiero:** Implementación de estrategias de *Open Banking* o servicios de *Banking as a Service (BaaS)*, donde el banco expone la capacidad de abrir cuentas de ahorro o consultar saldos directamente a Fintechs o comercios aliados autorizados.
* **Compromisos (Trade-offs):**
    * *Pros:* Control centralizado y homogéneo de la seguridad; protege los microservicios internos de la exposición directa; facilita la monetización de APIs.
    * *Contras:* Se convierte en un punto único de falla (Single Point of Failure) si no está altamente disponible; puede agregar sobrecostos de infraestructura y latencia de red.

### 5. Ingesta de Flujos de Datos en Tiempo Real (Event Streaming Consumer)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: STREAMING]` | `[Gobernanza: REGLAS_ESTÁNDAR]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Conexión continua a un bus de eventos distribuidos de alta velocidad, donde el sistema lee y procesa mensajes de forma secuencial e ininterrumpida a medida que son emitidos por la fuente externa.
* **Caso de Uso Financiero:** Integración con proveedores de Market Data o feeds de la bolsa de valores en tiempo real (precios de acciones, commodities, criptomonedas o divisas FX) para actualizar tableros de trading o alimentar motores analíticos de prevención de fraude transaccional.
* **Compromisos (Trade-offs):**
    * *Pros:* Latencia extremadamente baja (milisegundos); procesamiento continuo; excelente escalabilidad horizontal.
    * *Contras:* Complejidad en la gestión de punteros (offsets) y consistencia eventual; requiere infraestructura especializada (ej. Apache Kafka, AWS Kinesis).

### 6. Conector Resiliente con Disyuntor (Circuit Breaker) y Fallback
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_EXTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Mecanismo de protección que monitorea la tasa de fallos de las llamadas al conector externo. Si supera un umbral, el circuito "se abre", impidiendo que las solicitudes sigan golpeando al proveedor caído y desviando el tráfico inmediatamente a un mecanismo alternativo (fallback).
* **Caso de Uso Financiero:** Al solicitar una autorización de cobro a un procesador externo de tarjetas. Si el procesador experimenta degradación o caída, el disyuntor se abre y el *fallback* aprueba de forma autónoma transacciones inferiores a $15 USD basándose en un scoring predictivo local de riesgo aceptable.
* **Compromisos (Trade-offs):**
    * *Pros:* Evita la degradación en cascada de todo tu ecosistema; libera hilos de ejecución de inmediato; mejora la experiencia de usuario bajo fallos del tercero.
    * *Contras:* Diseñar un *fallback* financiero seguro es complejo y puede requerir asumir pérdidas u operaciones asincrónicas en diferido.

### 7. Receptor Idempotente (Idempotency Proxy / Gateway)
* **Taxonomía:** `[Rol: PROVEEDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_INTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Componente lógico intermedio que intercepta las solicitudes entrantes y comprueba si un identificador único global (Idempotency Key) ya ha sido procesado previamente. Si se detecta un duplicado, devuelve la respuesta guardada de la primera transacción sin volver a ejecutar la lógica de negocio.
* **Caso de Uso Financiero:** Evitar el doble impacto transaccional. Si una pasarela externa reintenta una petición HTTP POST de débito de fondos debido a un *timeout* de red justo cuando el banco ya había descontado el saldo pero no había alcanzado a responder.
* **Compromisos (Trade-offs):**
    * *Pros:* Seguridad matemática contra transacciones duplicadas; resiliencia ante fallos de red del lado del cliente/proveedor.
    * *Contras:* Exige almacenamiento de alto rendimiento y baja latencia (ej. Redis) con TTL estricto para almacenar temporalmente los estados y respuestas de las claves.

### 8. Limitador de Tasa Saliente (Egress Rate Limiter / Throttling)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_EXTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Mecanismo de control de tráfico saliente que encola, retrasa o rechaza solicitudes generadas internamente para asegurar que el volumen total de llamadas enviadas a una API externa se mantenga estrictamente por debajo de los límites acordados en el SLA del proveedor.
* **Caso de Uso Financiero:** Conexión con un proveedor externo de envío de notificaciones WhatsApp/SMS o un motor de KYC (Know Your Customer) que penaliza económicamente o bloquea la dirección IP de la Fintech si se superan las 100 peticiones por segundo (TPS).
* **Compromisos (Trade-offs):**
    * *Pros:* Protege la reputación del sistema frente a terceros; evita bloqueos reactivos de IPs en producción; controla costes variables si la API externa cobra penalizaciones por picos.
    * *Contras:* Genera acumulación de peticiones en memoria o colas internas durante picos de tráfico; puede aumentar el tiempo de respuesta percibido internamente.

### 9. Consulta Periódica Basada en Estado (Stateful Polling)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL o LOTE]` | `[Gobernanza: REGLAS_EXTERNAS]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Proceso en segundo plano (daemon, worker o cron-job) de naturaleza proactiva que interroga con una frecuencia predefinida a un sistema externo para verificar cambios de estado en transacciones específicas, manteniendo registro estricto en la base de datos local del último cursor, fecha o estado procesado.
* **Caso de Uso Financiero:** Integración con bancos tradicionales o redes heredadas que carecen de mecanismos modernos de empuje (Webhooks/Eventos). Se consulta cada 5 minutos una API de estado para verificar si una transferencia internacional SWIFT pasó de "Pendiente" a "Liquidada en Corresponsal".
* **Compromisos (Trade-offs):**
    * *Pros:* Única alternativa viable para integrarse con sistemas legados o cerrados que no exponen webhooks ni streaming de datos.
    * *Contras:* Altamente ineficiente (la mayoría de las consultas devuelven "sin cambios"); sobrecarga la red y las bases de datos tanto propias como del tercero.

### 10. Orquestador de Saga con Compensación Externa (Saga Pattern)
* **Taxonomía:** `[Rol: BIDIRECCIONAL]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_INTERNAS]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Patrón de diseño arquitectónico que gestiona transacciones de negocio distribuidas a través de múltiples servicios de terceros independientes mediante una secuencia de pasos locales. Si un paso intermedio falla, el orquestador es responsable de disparar en reversa "transacciones de compensación" explícitas para deshacer los efectos de los pasos exitosos previos.
* **Caso de Uso Financiero:** Contratación de un seguro de hogar desde la app bancaria. Paso 1: Retener saldo en el Core Bancario. Paso 2: Emitir póliza en la aseguradora externa A. Paso 3: Registrar el alta en el regulador de seguros. Si el Paso 3 falla, el orquestador ejecuta la compensación: revoca la póliza en la aseguradora A y libera el saldo retenido en el Core.
* **Compromisos (Trade-offs):**
    * *Pros:* Mantiene consistencia de datos a nivel de negocio sin requerir protocolos pesados y bloqueantes como Two-Phase Commit (2PC) sobre APIs HTTP.
    * *Contras:* Complejidad de desarrollo extrema; requiere diseñar e implementar manualmente un flujo inverso de cancelación/reversa por cada operación feliz externa.

### 11. Aislamiento de Red por Canal Seguro (mTLS + VPN / Direct Connect)
* **Taxonomía:** `[Rol: BIDIRECCIONAL]` | `[Granularidad: INDIVIDUAL o LOTE]` | `[Gobernanza: REGLAS_ESTÁNDAR]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Táctica puramente orientada a la infraestructura y seguridad del transporte donde se restringe el acceso al conector externo a nivel de red física o lógica mediante túneles privados virtuales (VPN) de sitio a sitio o enlaces dedicados de fibra (AWS Direct Connect / Azure ExpressRoute), complementado con autenticación mutua TLS (mTLS) basada en certificados criptográficos validados en ambas puntas.
* **Caso de Uso Financiero:** Interconexión troncal obligatoria de un nuevo core financiero con las redes nacionales de cajeros automáticos (Link, Banelco, Redbanc) o el backbone de enrutamiento de transacciones de tarjetas de crédito.
* **Compromisos (Trade-offs):**
    * *Pros:* Máxima seguridad perimetral de la industria; mitiga ataques Man-in-the-Middle y de suplantación de identidad a nivel de red básica.
    * *Contras:* Altos costes de configuración y mantenimiento de infraestructura; lentitud burocrática y operativa para renovar certificados vencidos o alterar topologías de red.

### 12. Patrón Outbox Transaccional (Transactional Outbox)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_INTERNAS]` | `[Sincronía: ASINCRÓNICO]`
* **Descripción:** Garantiza la consistencia atómica entre el estado del almacenamiento interno y la notificación al conector externo. En lugar de disparar el conector en medio de la transacción de la Base de Datos, el evento se escribe en una tabla local llamada "Outbox" compartiendo la misma transacción atómica. Un proceso independiente (Message Relay) lee la tabla y asegura la entrega asíncrona ("at least once") hacia el conector externo.
* **Caso de Uso Financiero:** Al otorgar un crédito en el Core Bancario. Es crítico guardar el saldo a favor en la Base de Datos interna y notificar de forma garantizada a la plataforma externa del fondo de inversión colectivo que fondea el dinero, impidiendo que una caída momentánea de la red rompa la consistencia de los libros contables.
* **Compromisos (Trade-offs):**
    * *Pros:* Consistencia garantizada entre base de datos y mensajería externa; elimina escenarios de fallos parciales catastróficos; desacopla el rendimiento de la BD interna del estado del conector externo.
    * *Contras:* Introduce consistencia eventual; requiere configurar y monitorear el componente intermedio que lee la tabla Outbox (ej. Debezium o un worker personalizado).

### 13. Bóveda de Tokenización Perimetral (PCI-DSS / PII Edge Vault)
* **Taxonomía:** `[Rol: BIDIRECCIONAL]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_ESTÁNDAR]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Interceptación perimetral de tráfico entrante y saliente mediante un proxy o micro-bóveda altamente protegida y aislada. Su único rol es capturar datos altamente sensibles (números de tarjeta PAN, datos biométricos, identificadores fiscales), almacenarlos en un entorno restringido encriptado y reemplazarlos por un token opaco no reversible antes de reenviar el tráfico a los microservicios internos.
* **Caso de Uso Financiero:** Cumplimiento normativo estricto de estándares globales de seguridad física y lógica como **PCI-DSS**. Toda la mensajería proveniente de la pasarela de pagos pasa primero por este componente; hacia el core bancario interno solo viaja el token `tkn-5542-9918-0283`, reduciendo el alcance de las auditorías de seguridad costosas sobre el resto del código del sistema.
* **Compromisos (Trade-offs):**
    * *Pros:* Reduce masivamente el alcance (*scope*) de auditorías normativas; centraliza las políticas criptográficas más duras en un único componente ultra seguro; protege datos de clientes ante filtraciones en capas internas.
    * *Contras:* Añade un paso crítico e indivisible en la ruta de ejecución; si la bóveda se degrada o pierde sus llaves criptográficas primarias, se detiene la operación de cobros por completo.

### 14. Enrutamiento Dinámico de Proveedores (Dynamic Provider Routing)
* **Taxonomía:** `[Rol: CONSUMIDOR]` | `[Granularidad: INDIVIDUAL]` | `[Gobernanza: REGLAS_INTERNAS]` | `[Sincronía: SINCRÓNICO]`
* **Descripción:** Capa lógica inteligente construida sobre conectores con interfaces de negocio polimórficas equivalentes. El enrutador evalúa reglas de negocio dinámicas parametrizables en tiempo real (costo transaccional actual, latencia observada en los últimos 2 minutos, tasa de éxito histórica del mes, país del usuario) para decidir heurísticamente a qué proveedor externo específico despachar la transacción.
* **Caso de Uso Financiero:** Optimización de costes y disponibilidad en pasarelas de envío de One-Time Passwords (OTP) por SMS o motores de Onboarding Digital. Si el Proveedor A (SMS económico) empieza a rebotar mensajes o retrasarlos, el enrutador conmuta en tiempo real el tráfico hacia el Proveedor B (SMS premium, más costoso pero con 99.9% de entrega garantizada).
* **Compromisos (Trade-offs):**
    * *Pros:* Alta disponibilidad transparente de cara al cliente; optimización financiera directa en infraestructuras de consumo masivo; mitiga el bloqueo (vendor lock-in).
    * *Contras:* Exige mantener y desarrollar abstracciones de código idénticas para proveedores competitivos; requiere lógicas complejas de observabilidad continua para tomar las decisiones de ruteo.

---

## 3. Matriz de Selección Rápida para Diseñadores

Esta matriz permite filtrar y seleccionar la táctica adecuada al iniciar la redacción de un ADR basándose en los componentes de la taxonomía:

| Táctica | Rol Principal | Sincronía | Granularidad | Gobernanza típica | Propósito Principal del ADR |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1. ACL** | Consumidor | Sincrónico | Individual | REGLAS_EXTERNAS | Proteger el dominio interno del desorden semántico externo. |
| **2. SFTP + ETL** | Bidireccional | Asincrónico | Lote | REGLAS_ESTÁNDAR | Procesar volúmenes masivos consolidados en diferido. |
| **3. Webhooks** | Proveedor | Asincrónico | Individual | REGLAS_EXTERNAS | Reaccionar a eventos externos en tiempo real sin polling. |
| **4. API Gateway** | Proveedor | Sincrónico | Individual | REGLAS_INTERNAS | Exponer capacidades internas de forma segura y auditada. |
| **5. Event Streaming** | Consumidor | Asincrónico | Streaming | REGLAS_ESTÁNDAR | Procesar datos continuos e infinitos a alta velocidad. |
| **6. Circuit Breaker** | Consumidor | Sincrónico | Individual | REGLAS_EXTERNAS | Evitar degradación en cascada cuando el externo falla. |
| **7. Idempotency Proxy** | Proveedor | Sincrónico | Individual | REGLAS_INTERNAS | Garantizar procesamiento único y prevenir cobros dobles. |
| **8. Egress Rate Limiter** | Consumidor | Sincrónico | Individual | REGLAS_EXTERNAS | Respetar las cuotas de consumo externas y evitar bloqueos. |
| **9. Stateful Polling** | Consumidor | Asincrónico | Individual/Lote | REGLAS_EXTERNAS | Simular asincronía sobre sistemas legados sin eventos. |
| **10. Saga Pattern** | Bidireccional | Asincrónico | Individual | REGLAS_INTERNAS | Coordinar flujos distribuidos multi-proveedor con reversa. |
| **11. mTLS + VPN** | Bidireccional | Sincrónico | Ambos | REGLAS_ESTÁNDAR | Blindar el canal de transporte bajo normas bancarias duras. |
| **12. Transactional Outbox**| Consumidor | Asincrónico | Individual | REGLAS_INTERNAS | Consistencia atómica entre cambios de BD local y avisos externos. |
| **13. Tokenization Vault** | Bidireccional | Sincrónico | Individual | REGLAS_ESTÁNDAR | Aislar datos de tarjetas (PAN) fuera del core para cumplir PCI. |
| **14. Dynamic Routing** | Consumidor | Sincrónico | Individual | REGLAS_INTERNAS | Pivotar entre proveedores homólogos para ahorrar costo o ganar SLA. |

---

## 4. Apéndice: Plantilla Sugerida para el uso de este Catálogo en un ADR

Al redactar el ADR final en tu repositorio Git, puedes enlazar este catálogo y estructurarlo así: