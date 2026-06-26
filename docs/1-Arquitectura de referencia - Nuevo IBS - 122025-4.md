   Blueprint de Arquitectura del nuevo IBS

Versión 1.0

1

Términos y deﬁniciones (Glosario)
Sigla
KYC

AML

DTO

OLTP

ETL

ACL (An"-Corrup"on Layer)

API

REST

GraphQL

OIDC

DLP

Outbox

Saga

ACID

BI

p95

SLO

CI

Deﬁnición
Conozca a su cliente: procesos para veriﬁcar
la iden"dad y el perﬁl del cliente.
Prevención de lavado de dinero: monitoreo y
controles para detectar ac"vidades ilícitas.
Objeto de transferencia de datos: contrato de
entrada/salida entre capas/módulos.
Procesamiento de transacciones en línea:
sistemas transaccionales de alta
concurrencia.
Extracción, transformación y carga: procesos
de integración de datos y lotes.
Capa an"corrupción: adaptadores que aíslan
el dominio de proveedores externos.
Interfaz de programación de aplicaciones:
contratos expuestos a clientes y socios.
Es"lo de arquitectura para APIs basado en
recursos y operaciones estándar HTTP.
Lenguaje de consulta para APIs que permite
pedir exactamente los datos necesarios.
OpenID Connect: estándar de auten"cación
sobre OAuth 2.0.
Prevención de pérdida de datos: controles
para evitar fugas de información.
Patrón para publicar eventos externamente
de forma conﬁable dentro de la misma
transacción.
Patrón de orquestación con pasos y
compensaciones para mantener consistencia
entre módulos.
Propiedades de transacción: atomicidad,
consistencia, aislamiento y durabilidad.
Inteligencia de negocios: reportes, analí"ca y
proyecciones de lectura.
Percen"l 95 de latencia: el 95% de las
solicitudes es más rápido que este valor.
Obje"vo de nivel de servicio: meta
cuan"ta"va de desempeño (disponibilidad,
latencia, errores).
Integración con"nua: compilación y pruebas
automá"cas en cada cambio.

2

CD

Blue/Green

Canary

Feature Flags

PII

Idempotency-Key

Entrega/Despliegue con"nuo: automa"zación
para liberar cambios de forma frecuente y
segura.
Estrategia de despliegue con dos entornos
idén"cos para cambiar tráﬁco sin
interrupción.
Despliegue gradual a un subconjunto de
usuarios para reducir riesgo.
Interruptores para habilitar o deshabilitar
funcionalidades en "empo de ejecución.
Información de iden"ﬁcación personal: datos
que iden"ﬁcan a una persona.
Clave de idempotencia: iden"ﬁcador para
evitar ejecutar dos veces la misma operación.

3

Tabla de contenido

Contenido
Términos y deﬁniciones (Glosario)...................................................................................................2

Tabla de contenido...........................................................................................................................4

Deﬁnición general del es"lo monolito modular...............................................................................6

Caracterís"cas clave......................................................................................................................6

Modularidad interna.....................................................................................................................6

Interfaces deﬁnidas......................................................................................................................6

Ventajas........................................................................................................................................6

Mantenibilidad mejorada.............................................................................................................7

Desarrollo paralelo.......................................................................................................................7

Comparación con otras arquitecturas..........................................................................................7

Monolitos tradicionales................................................................................................................7

Microservicios...............................................................................................................................7

DesaFos del desarrollo de aplicaciones modulares.........................................................................7

Determinación de los límites de los módulos...............................................................................7

Complejidad de las interacciones entre módulos........................................................................8

Diseño de interfaces robustas......................................................................................................8

Op"mización del rendimiento......................................................................................................8

Pruebas y control de calidad.........................................................................................................8

Curva de aprendizaje y coordinación del equipo.........................................................................9

Blueprint general..............................................................................................................................9

1.1

Principios rectores...........................................................................................................9

1.2 Vista de alto nivel (diagrama textual)...................................................................................10

2) Componentes (módulos de dominio).........................................................................................12

3) Mecanismos de integración.......................................................................................................13

3.1 Interna (entre módulos).......................................................................................................13

3.2 Consistencia y transacciones................................................................................................13

3.3 Integración externa...............................................................................................................14

4

3.4 Más sobre Integración Interna.............................................................................................14

4) Modelo de datos y gobierno......................................................................................................14

5) Atributos de calidad....................................................................................................................15

6) Flujos de referencia, ejemplo.....................................................................................................15

6.1 Posteo de transferencia (camino feliz):............................................................................15

6.2 Compensación ante falla externa:....................................................................................15

7) Siguientes Iteraciones.................................................................................................................15

5

Arquitectura de Referencia — Monolito
Modular para nuevo IBS

Obje(cid:25)vo: El siguiente documento con"ene el diseño de arquitectura de referencia para la
modernización de IBS. Este documento es el resultado de la Iteración de diseño ejecutada entre
el 9 y 10 de diciembre de 2025. En los contenidos se sinte"zan en un conjunto de decisiones
relacionadas con el producto y su visión de futuro, al mismo "empo que se resuelven algunos
problemas actuales de la aplicación.

El contenido incluye una referencia conceptual al es"lo arquitectónico elegido, un conjunto de
principios arquitecturales que regirán todo el resto del diseño y desarrollo del nuevos
productos, los componentes y mecanismos de integración para un monolito modular evolu"vo,
compa"ble para ser instalado y operado en la nube y cumpliendo los obje"vos de negocios de
Censys.

Deﬁnición general del es(cid:25)lo monolito modular
La arquitectura monolí"ca modular es un es"lo arquitectónico que estructura una aplicación
como una unidad cohesiva, dividiéndola internamente en un can"dad dada de módulos que
"ene poco acoplamiento entre si. Cada uno de ellos encapsula funcionalidades especíﬁcas e
interactúa con otros a través de interfaces bien deﬁnidas, lo que promueve una alta cohesión y
un bajo acoplamiento como caracterís"ca central de la aplicación.

Caracterís(cid:25)cas clave

(cid:1) Unidad de implementación única
(cid:1)

Toda la aplicación se implementa como una sola unidad, lo que simpliﬁca los procesos
de implementación y reduce la complejidad opera"va.

Modularidad interna

(cid:1)

Aunque se trata de una única aplicación, está organizada en módulos independientes,
cada uno responsable de un dominio o funcionalidad especíﬁca.

Interfaces deﬁnidas

(cid:1)

Los módulos se comunican a través de interfaces explícitas, lo que garan"za límites
claros y minimiza las interdependencias.

Ventajas

(cid:1) Desarrollo e implementación simpliﬁcados
(cid:1) Mantener una base de código y un ﬂujo de implementación únicos reduce la

complejidad en comparación con las arquitecturas de microservicios.

6

Mantenibilidad mejorada

(cid:1)

(cid:1)

Los límites claros de los módulos facilitan las actualizaciones y el mantenimiento, ya que
se pueden realizar cambios dentro de un módulo sin afectar a los demás. Potencial de
escalabilidad
Aunque comienza como un monolito, la estructura modular permite extraer módulos
individuales a microservicios si es necesario escalar.

Desarrollo paralelo

(cid:1)

Varios equipos y desarrolladores pueden trabajar en diferentes módulos en paralelo sin
afectarse entre sí.

Comparación con otras arquitecturas

Monolitos tradicionales
En las arquitecturas monolí"cas tradicionales, todos los componentes están estrechamente
interconectados, lo que genera desaFos de escalabilidad y mantenimiento. Los monolitos
modulares abordan estos problemas introduciendo modularidad interna.

Microservicios
La arquitectura de microservicios implica la creación de aplicaciones como un conjunto de
servicios implementables de forma independiente. Si bien ofrece escalabilidad y ﬂexibilidad,
introduce una complejidad signiﬁca"va en la implementación, la comunicación y la consistencia
de los datos. Los monolitos modulares ofrecen una solución intermedia, ofreciendo modularidad
interna sin la sobrecarga que supone ges"onar múl"ples servicios.

En resumen, la arquitectura monolí"ca modular combina la simplicidad de la implementación
monolí"ca con las ventajas del diseño modular, lo que la convierte en una opción prác"ca para
aplicaciones que requieren una estructura clara y facilidad de mantenimiento sin las
complejidades asociadas a los microservicios.

Desa&os del desarrollo de aplicaciones modulares
Si bien la modularidad ofrece ventajas, como se explicó en las secciones anteriores, también
presenta varios desaFos que deben considerarse durante el diseño, desarrollo, prueba e
implementación de la solución.

Determinación de los límites de los módulos
Determinar los límites de los módulos es uno de los aspectos más desaﬁantes del enfoque
modular. Comprender qué funcionalidad debe implementarse en cada módulo puede llevar
"empo. Una vez que comprenda mejor el sistema, estará listo para migrar algunas en"dades o
servicios, total o parcialmente, a otro módulo. En ocasiones, esto también puede ocurrir cuando
se modiﬁcan los requisitos del negocio. Realizar estos cambios puede ser diFcil, especialmente si
el proyecto ya se ha implementado y u"lizado en producción. En el caso de IBS, que dispone de

7

muy buenos expertos de dominio que deberán integrarse al equipo de arquitectura al momento
de hacer la distribución de funcionalidad entre módulos.

Complejidad de las interacciones entre módulos
Ges"onar las interacciones entre numerosos módulos puede resultar complejo. Garan"zar que
los módulos se comuniquen eﬁcazmente sin una conexión rígida requiere una planiﬁcación
cuidadosa. Pueden surgir dependencias imprevistas, creando una maraña de módulos diFciles
de ges"onar.

Diseño de interfaces robustas
Crear interfaces claras y consistentes es crucial. Las interfaces mal diseñadas pueden provocar
malentendidos y errores en la comunicación, (sincrónica versus asincrónica).

Actualizar las interfaces sin interrumpir los módulos dependientes requiere un me"culoso
control de versiones y comunicación entre los equipos de desarrollo. Ver detalles de la Iteración
2 del plan de arquitectura.

Op(cid:25)mización del rendimiento
En una aplicación modular, aislar los datos y los detalles de implementación de un módulo de
los demás es una buena prác"ca. De esta manera, los cambios internos de un módulo (incluidos
los cambios en la base de datos) no afectan a otros módulos, siempre que se mantengan los
contratos (la interfaz de un módulo u"lizada por otros módulos) sin cambios o se hagan
compa"bles con versiones anteriores. Incluso si los contratos cambian, actualizar el código del
cliente es trivial si se implementa una buena abstracción.

En una capa de datos uniﬁcada, se puede realizar una única consulta SQL JOIN que funciona en
varias tablas y está bien op"mizada a nivel de base de datos. Sin embargo, al aislar los datos de
otros módulos, estos siempre necesitan consultar los datos del módulo cuando lo necesitan.
Una simple consulta a la base de datos puede conver"rse en varias llamadas a la API y muchas
consultas a la base de datos en una aplicación modular, lo que puede reducir drás"camente el
rendimiento de la aplicación y consumir recursos del sistema.

Por lo tanto, es necesario cuidar el rendimiento en cada interacción entre módulos y op"mizar
estas comunicaciones. Considere usar el almacenamiento en caché en lugar de realizar consultas
cada vez. Incluso puede considerar implementar la duplicación de datos (copiar los datos de
otros módulos en la base de datos de un módulo para reducir la comunicación y op"mizar el
rendimiento de las consultas), la desnormalización y la sincronización (al cambiar los datos del
módulo de des"no).

Pruebas y control de calidad
Si bien los módulos se pueden probar de forma aislada, garan"zar que funcionen juntos sin
problemas es un desaFo. Se necesitan pruebas de integración exhaus"vas, que pueden ser
largas y complejas, para validar las interacciones de los módulos.

8

Curva de aprendizaje y coordinación del equipo
Los desarrolladores pueden necesitar "empo para comprender la arquitectura modular y los
principios y estándares necesarios para desarrollos robustos y de calidad. Se necesita una
coordinación eﬁcaz para garan"zar que todos los equipos estén alineados con las interfaces de
los módulos y los puntos de integración.

Todos los desaFos comentados "enen un componente de solución común, que es el diseño y
decisiones de arquitectura. No solo en lo que hace al sistema en si mismo, sino también en la
forma de estructurar a los equipos de desarrollo, su coordinación y los equipos de soporte.

Blueprint general

1.1 Principios rectores
La siguiente es una lista de principios de arquitectura que rigen las decisiones de diseño, la
selección de tecnologías y la evolución del sistema.

1 Dominio antes que tecnología: módulos alineados a dominios del negocio, (clientes,

2

3

4

5

cuentas, productos, etc.).
Encapsulamiento fuerte; contratos internos (fachadas) y polí"cas de acceso. Nada accede a
un módulo sino a través de sus interfaces
Consistencia fuerte donde importa (contabilidad, saldos, créditos); eventual para
proyecciones y algunos reportes. Este principio deﬁne mucho de lo que hace al rendimiento
del nuevo IBS.
Integración por diseño; proveedores externos, socios de negocios y otras en"dades, solo
acceden a IBS a través de los mecanismos de integración autorizados, (APIs y conectores).
Seguridad, cumplimiento y auditoría por diseño: El diseño del nuevo IBS, debe contemplar
la seguridad y el cumplimiento de los estándares y normas ﬁnancieras locales e
internacionales, facilitando los controles de auditoría a todos los niveles.

6 Observabilidad integral: En sistemas modulares la capacidad de hacer seguimiento de la

7

8

operación en "empo real, es fundamental, de ahí que generar logs completos que puedan
proveer de datos para la trazabilidad de inicio a ﬁn de transacciones y operaciones es
fundamental.
Evolución: IBS, en su nueva versión, debe asegurar una evolución dinámica y robusta que
permita adaptar el producto a nuevas realidades de mercado ﬁnanciero y de sus clientes,
asegurando que el diseño arquitectural pueda evolucionar en forma disciplinada y
manteniendo esta lista de principios.
Inteligencia Ar(cid:25)ﬁcial: el diseño del nuevo IBS debe incluir IA entre sus componentes, pero
solo en aquellos módulos/funcionalidad que agreguen verdadero valor al producto y que el
ROI de la incorporación sea obje"vo y posi"vo. También el desarrollo del producto deberá
considerar IA para la codiﬁcación de componentes, tes"ng automa"zado y documentación,
pero siempre preservando el juicio humano y crí"co como responsable de veriﬁcar lo
generado por la IA.

9

9 Data Driven: Para que el nuevo IBS este alineado a las exigencias del mercado, es necesario
que los datos estén en el centro del desarrollo, asegurando integridad, consistencia y
veracidad. La única forma de asegurar el principio 8, es que los datos sean ciudadanos de
primero orden.

1.2 Vista de alto nivel (diagrama textual)
El siguiente diagrama general muestra la estructura del nuevo IBS, bajo el es"lo arquitectónico
de Monolito Modular con las caracterís"cas descriptas en el apartado conceptual

Este diagrama responde, en principio, a varias de las preguntas formuladas en la primera
iteración tales como:

(cid:1) Que es el core
(cid:1)

Cloud compu"ng, On premise o hibrido y otras que están en el plan de arquitectura

10

11

2) Componentes (módulos de dominio)
Módulo

Responsabilidades clave

Aplicaciones externas

Integraciones externas

Dentro de este componente se incluyen todas las
aplicaciones externas a IBS, tanto sean propias
de Censys como externas.

Son los mecanismos de vinculación de IBS con las
aplicaciones externas. Pueden ser de dos "pos: a
través de conectores o través de un API Gateway
con múl"ples funciones.

Monolito Modular – Nuevo
IBS

Con"ene todos los módulos del core, productos
y transversales a toda la aplicación

Core de IBS

Son los módulos de Contabilidad, Clientes,
Seguridad y Cálculo y Conﬁguración

Integraciones Internas

Productos

Con"ene módulos para la ges"ón de las
integraciones entre los módulos dentro del
monolito modular. Los patrones de integración
previstos: Eventos/mensajería; Servicios y API
Gateway

Son los módulos de productos bancarios
especíﬁcos: Caja de Ahorros, Cuenta Corriente,
Créditos, Plazo Fijo y todos los que puedan surgir
a futuro

Analí"ca de Datos

Son módulos especializados para la explotación
de información: IBS Data Lake y Repor"ng + IA

Módulos Transversales

Son módulos que impactan en el resto y deben
atender funcionalidad relacionada con
Observabilidad y Auditoría:
Auditoría inmutable y DLP.
Observabilidad (métricas, logs, trazas) y
correlación.
Programación de tareas (jobs) es necesario?.
Hay algún otro en esta línea?

12

Esta distribución de componentes queda sujeta a revisión, por ejemplo dominios como
Tesorería deberían formar parte del core?. Cuáles otros se deben considerar?.

3) Mecanismos de integración

3.1 Interna (entre módulos)
Fachadas (servicios de aplicación) con contratos de entrada/salida.

Manejo de eventos/mensajes entre módulos. Persistencia en memoria para casos de alto
rendimiento.

API Gateway

Polí(cid:25)cas de acceso: repositorios privados por módulo. Cada módulo posee su propia base de
datos y/o conjunto de tablas, exclusivamente controladas por el mismo.

3.2 Consistencia y transacciones
Transacción local por operación de dominio (alta de cuenta, movimientos entre diferentes
cuentas, asientos, cancelación de operación dentro del mismo módulo).

Sagas cuando una transacción toca más de un módulo. A con"nuación se da un detalle de lo que
implica el patrón Sagas:

Para varias operaciones dentro de IBS es necesario involucrar varias en"dades y servicios (o
ru"nas) que afectan a dichas en"dades. Adicionalmente, deben asegurarse de que dichas
transacciones respeten mayormente las propiedades ACID. Los patrones sugeridos
anteriormente "enden a distribuir la lógica de la aplicación en servicios que deben ser
coordinados para implementar estas transacciones.

Si bien es posible implementar bloqueos que permitan asegurar propiedades ACID, en muchos
casos esto genera un conﬂicto con los requerimientos de performance de la aplicación.

Cuando se trabaja con microservicios o con servicios de granularidad ﬁna, es importante
mantener dichos servicios lo más autónomos y desacoplados del resto. No obstante, a la hora de
implementar una funcionalidad, dichos servicios deben ser combinados. La estrategia
denominada Orquestación, que es a menudo la elección tradicional, se basa en proveer un
componente que invoca y coordina las entradas y salidas de un conjunto de microservicios, y
que posee la lógica para dicha coordinación, a menudo en base a invocaciones sincrónicas. Por
ejemplo, si se usa el patrón Saga, las transacciones pueden coordinarse a través de un
manejador centralizado de sagas. Esta estrategia suele ser más sencilla de implementar y
testear, y también de asegurar cierta consistencia de datos, si bien suele recargar la mayoría de
la responsabilidad (es decir, la lógica) en el componente orquestador.

13

Una alterna"va más distribuida es la estrategia denominada CoreograFa, donde cada servicio o
componente que par"cipa se comunica con el resto de los componentes mediante la
publicación y consumo de eventos. De esta manera, la coordinación de las dis"ntas
funcionalidades (por ej., transacciones en sagas) emerge de la interacción entre las partes. Si
bien este esquema es más ﬂexible, permite ensamblar sistemas de manera rápida, y no posee
un punto central de falla o coordinación, suele traer aparejada una mayor complejidad a la hora
de testear la funcionalidad y de resolver escenarios transaccionales (con requerimientos de alta
consistencia) ante fallas parciales.

Idempotencia por clave.

3.3 Integración externa
Persis"r el evento en la misma transacción y publicarlo de forma conﬁable.

Capa an"corrupción para redes de pago, burós, reguladores; DTO internos aislados.

Sincrónico (REST/GraphQL) para consultas/comandos inmediatos; asíncrono (colas/streams)
para no"ﬁcaciones/liquidaciones/ETL.

3.4 Más sobre Integración Interna
API Gateway para auten"cación, rate limi"ng, auditoría.

Contratos basados en servicios y con versionados (v1/v2); compa"bilidad hacia atrás por dos
releases.

Ges"ón de eventos en forma asincrónica

4) Modelo de datos y gobierno
Propiedad por módulo; consultas cruzadas vía vistas o servicios.

Mul"moneda y calendarios bancarios; cut-oﬀ.

Auditoría append-only en eventos crí"cos.

Clasiﬁcación y privacidad; tokenización/cifrado de campos sensibles.

Réplica de solo lectura para reportes/consultas analí"cas, (patrón CQRS).

Más allá de estas tác"cas generales, la segunda versión de este Blueprint deberá contener un
conjunto de vistas y decisiones relacionadas con el modelo de datos del nuevo IBS, tomando
como base el modelo actual.

Deﬁnir una polí"ca y un estándar para controlar el uso de Store Procedures. Deben ser
excepciones al principio de separar lógica funcional de los datos almacenados.

14

5) Atributos de calidad
La siguiente es una lista de los atributos de calidad que son drivers del diseño del nuevo IBS:

- Performance, (latencia y "empo de respuesta)

- Disponibilidad

- Escalabilidad

- Seguridad y auditoría

- Flexibilidad

- Mantenibilidad

La segunda versión de este Blueprint, debería contener, al menos, tres escenarios por atributo,
los cuáles sirvan como veriﬁcadores de las decisiones de diseño de mayor detalle.

6) Flujos de referencia, ejemplo

6.1 Posteo de transferencia (camino feliz):
API recibe POST /pagos/transferencias con Idempotency-Key.

Orquestación valida Cliente y Lavado de dinero y límites posibles.

Pagos registra orden y solicita débito a Cuentas.

Cuentas debita y publica MovimientoRegistrado; contabilidad registra asientos.

Outbox guarda PagoEjecutado y Relay publica a red de pagos/no"ﬁcaciones.

Reportes actualiza su proyección por evento.

6.2 Compensación ante falla externa:
Si falla red de pagos tras el débito, saga marca PendienteCompensacion y dispara tarea
compensatoria con reintentos y alerta.

7) Siguientes Iteraciones
El siguiente es un plan para profundizar las decisiones de este Blueprint y obtener una
arquitectura detallada del nuevo IBS.

Estas iteraciones son complementarias a las dos ya ejecutadas y/o en ejecución, (la 2 está en
curso).

15

Iteración

Iteración 3

Tiempo es(cid:25)mado
1 semana

Alcance

Elaboración de los escenarios globales para validar la
arquitectura

Iteración 4

2 semanas

Iteración 5

2 semanas

Iteración 6

2 semanas

Diseño de los componentes de Integración externa e
interna incluyendo la estrategia de conectores para
vincular IBS con otro "po de aplicaciones.

Diseño de los componentes del core, (si se va a requerir
re diseñar). La excepción es el conﬁgurador y el de
cálculos que deben ser gestados nuevamente.

Diseño genérico para los módulos de producto: La idea
es obtener un modelo de referencia que pueda ser
aplicado a los productos actuales y nuevos que surjan
en un futuro. El diseño de transacciones se resuelve en
esta iteración

Iteración 7

2 semanas

Diseño del data lake de IBS y el modulo de reportes y de
IA como modelos de referencia

Iteración 8

2 semanas

Diseño de la convivencia e implantación del nuevo IBS
con el legado.

Iteración 9

Consume esfuerzo de
las demás iteraciones

Diseño de la estrategia de observabilidad y auditoría.
Esta iteración es transversal a todas las demás. Deﬁnir
que hacer con la ejecución de Jobs en modalidad batch

Iteración 10

Idem 9

Esta iteración es transversal a todas las demás y en
forma incremental, va diseñando el nuevo modelo de
datos de IBS y un estándar para el uso de store
procedures

TOTAL DE
ESFUERZO

(cid:1)

11 semanas

Esta ac"vidad implica un esfuerzo de 11 semanas e incluye la decisión sobre las
tecnologías para implantar el nuevo IBS.

(cid:1) Hay que deﬁnir el equipo para estas ac"vidades, al menos de tres arquitectos y

(cid:1)

eventuales consultas sobre aspectos funcionales.
Este documento puede tomarse como base para ir ampliando las decisiones de
diseño de cada uno de los componentes/módulos.

16

