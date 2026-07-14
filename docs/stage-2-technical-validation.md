# Stage 2 — Technical Validation Outcome

## 1. Objective

La Etapa 2 tuvo como objetivo convertir el modelo conceptual del festival en un dominio mínimo ejecutable y validar el flujo principal de asignación sin introducir todavía persistencia real, API HTTP, fairness ni concurrencia avanzada.

La etapa debía demostrar que las capas principales podían colaborar correctamente:

```text
Domain ejecuta las reglas
→ Application orquesta el caso de uso
→ Infrastructure implementa los ports
→ Dependency Injection compone el sistema
→ el flujo termina Completed o Rejected
```

---

## 2. Qué se construyó

### Solución y arquitectura

* Una solución .NET 8 organizada en:

  * `Festival.Domain`
  * `Festival.Application`
  * `Festival.Infrastructure`
  * `Festival.Api`
* Dependencias correctamente dirigidas:

  * `Festival.Domain` no depende de otros proyectos.
  * `Festival.Application` depende únicamente de `Festival.Domain`.
  * `Festival.Infrastructure` depende de Application y Domain.
  * `Festival.Api` funciona como composition root.

### Modelo de dominio ejecutable

Se implementaron los conceptos mínimos necesarios para ejecutar el flujo principal:

* `Attendee`
* `FestivalDay`
* `Zone`
* `Spot`
* `AssignmentRequest`
* `AssignmentGroup`
* `Assignment`
* `AssignmentEngine`
* `AssignmentEngineResult`

También se implementaron los identificadores y Value Objects necesarios, entre ellos:

* `AttendeeId`
* `AttendeeCode`
* `FestivalDayId`
* `ZoneId`
* `SpotCode`
* `RowCode`
* `SpotNumber`
* `GroupSize`
* `AssignmentRequestId`
* `AssignmentId`

### Comportamiento del dominio

El dominio puede:

* crear y resolver una `AssignmentRequest`;
* formar un `AssignmentGroup` válido;
* validar el resultado completo de un grupo;
* encontrar un bloque mínimo de Spots contiguos;
* crear una `Assignment` por asistente;
* distinguir entre solicitudes:

  * `Completed`;
  * `Rejected`;
  * `Failed`, aunque este último flujo aún no está integrado en el caso de uso principal.

### Capa Application

Se implementó `ProcessAssignmentRequestUseCase`, responsable de:

* recibir el input del proceso;
* crear la `AssignmentRequest`;
* resolver `AttendeeCodes` a `AttendeeIds`;
* crear el `AssignmentGroup`;
* obtener los Spots disponibles;
* ejecutar `AssignmentEngine`;
* completar o rechazar la solicitud;
* persistir el resultado mediante ports;
* devolver un resultado propio de Application.

Los ports definidos son:

* `IAttendeeCodeResolver`
* `IAvailableSpotProvider`
* `IAssignmentRequestRepository`
* `IAssignmentRepository`

### Infraestructura in-memory

Se implementaron adaptadores in-memory para los cuatro ports de Application.

Estos adaptadores permiten:

* resolver asistentes sembrados por código;
* obtener Spots asociados a un `FestivalDay`;
* almacenar solicitudes con su estado final;
* almacenar Assignments generadas.

También se configuró Dependency Injection para resolver el flujo completo:

* `ProcessAssignmentRequestUseCase`: `Scoped`;
* `AssignmentEngine`: `Singleton`, por ser stateless;
* adaptadores in-memory: `Singleton`, porque mantienen el estado local durante la ejecución.

### Dataset determinístico

Se creó un dataset in-memory compuesto por:

* 10 Attendees;
* 1 FestivalDay;
* 2 Zones;
* 2 Rows por Zone;
* 4 Spots por Row;
* 16 Spots en total.

Los identificadores, códigos y fechas del dataset son determinísticos para permitir pruebas repetibles.

---

## 3. Escenarios validados

### Pruebas unitarias

Se agregaron pruebas unitarias para validar el comportamiento central de:

* Value Objects;
* creación de entidades;
* ciclo de vida de `AssignmentRequest`;
* composición de `AssignmentGroup`;
* validación del resultado del grupo;
* creación de `Assignment`;
* selección de bloques mediante `AssignmentEngine`;
* resultados de Application;
* adaptadores in-memory.

Las pruebas cubren escenarios válidos e inválidos, incluyendo:

* identificadores vacíos;
* colecciones nulas;
* elementos nulos dentro de colecciones;
* grupos incompletos;
* asistentes duplicados;
* Zones diferentes;
* Rows diferentes;
* SpotNumbers no consecutivos;
* transiciones de estado inválidas.

### Prueba de integración del flujo in-memory

Se implementó una prueba de integración a nivel de Application que usa:

* `ProcessAssignmentRequestUseCase`;
* `AssignmentEngine`;
* ports reales de Application;
* adaptadores in-memory reales;
* Dependency Injection;
* dataset determinístico.

No se utilizan mocks en esta prueba.

#### Escenario exitoso

Entrada:

* solicitud de 3 Attendees existentes;
* FestivalDay sembrado;
* disponibilidad de un bloque de 3 Spots contiguos.

Resultado esperado y validado:

* `AssignmentRequest` termina en estado `Completed`;
* se generan exactamente 3 Assignments;
* las Assignments pertenecen a la misma solicitud;
* las Assignments pertenecen al mismo FestivalDay;
* las Assignments pertenecen a la misma Zone;
* las Assignments pertenecen a la misma Row;
* los SpotNumbers son consecutivos;
* la solicitud final queda almacenada;
* las Assignments quedan almacenadas.

#### Escenario rechazado

Entrada:

* solicitud de 5 Attendees existentes;
* cada Row contiene únicamente 4 Spots contiguos.

Resultado esperado y validado:

* no se divide el grupo;
* no se crea un resultado parcial;
* `AssignmentRequest` termina en estado `Rejected`;
* se registra el código de rechazo por falta de Spots contiguos;
* la solicitud rechazada queda almacenada;
* no se almacenan Assignments.

---

## 4. Invariantes protegidas

### Protegidas dentro del modelo actual

#### INV-03 — Complete AssignmentGroup

El resultado debe incluir exactamente una Assignment para cada integrante del grupo.

No se aceptan resultados:

* parciales;
* con asistentes adicionales;
* con asistentes duplicados.

#### INV-04 — Single Zone per AssignmentGroup

Todas las Assignments de un grupo deben pertenecer a la misma Zone.

#### INV-05 — Contiguous Spots

Para el MVP, los Spots contiguos están definidos como:

* misma Zone;
* misma Row;
* SpotNumbers consecutivos;
* cantidad igual al `GroupSize`.

No se permite dividir el grupo entre filas o zonas.

#### INV-07 — Complete Assignment Identity

Cada Assignment contiene:

* `AssignmentId`;
* `AssignmentRequestId`;
* `FestivalDayId`;
* `AttendeeId`;
* información completa del Spot asignado;
* `AssignedAt`.

#### INV-08 — AssignmentRequest Outcome Consistency

Actualmente se protege que:

* una solicitud `Completed` tenga Assignments completas;
* una solicitud `Rejected` no tenga Assignments;
* nunca se persistan Assignments parciales.

El flujo `Failed` existe en el modelo, pero todavía no se integra en la orquestación principal.

### Parcialmente protegida

#### INV-06 — Final Assignment

`Assignment` es inmutable después de su creación y no expone operaciones para modificarla o cancelarla.

Todavía falta respaldar esta regla mediante:

* persistencia real;
* casos de uso futuros;
* restricciones sobre actualizaciones o eliminaciones.

---

## 5. Invariantes pendientes

### INV-01 — Unique Spot per FestivalDay

Todavía no existe protección global para impedir que dos solicitudes concurrentes asignen el mismo Spot durante el mismo FestivalDay.

### INV-02 — Unique Attendee per FestivalDay

Todavía no existe protección global para impedir que un Attendee sea asignado dos veces durante el mismo FestivalDay.

### INV-06 — Final Assignment

Está protegida localmente mediante inmutabilidad, pero falta protección en persistencia y operaciones externas.

### INV-08 — AssignmentRequest Outcome Consistency

Falta validar el comportamiento completo del estado `Failed`, incluyendo errores técnicos y consistencia transaccional.

---

## 6. Limitaciones actuales

La implementación actual:

* utiliza almacenamiento in-memory;
* pierde el estado cuando finaliza la aplicación;
* no cuenta con transacciones reales;
* no protege concurrencia;
* no representa el rendimiento de una base de datos;
* no incluye constraints únicos persistentes;
* no implementa fairness;
* no considera Assignment History;
* no calcula `RotationScore`;
* selecciona el primer bloque contiguo válido;
* no expone todavía endpoints HTTP;
* no incluye procesamiento asíncrono mediante colas;
* no está preparada para producción.

Los adaptadores in-memory son una herramienta de validación técnica y no una alternativa de persistencia productiva.

---

## 7. Decisiones diferidas

### Persistencia

Se difirió la selección e implementación de la base de datos real.

Durante la siguiente etapa se evaluará y definirá el uso de:

* SQL Server;
* PostgreSQL;

considerando experiencia del equipo, despliegue, costos, herramientas y necesidades operativas.

### Estrategia de concurrencia

Se difirió la definición de:

* transacciones;
* constraints únicos;
* niveles de aislamiento;
* optimistic concurrency;
* locking;
* manejo de conflictos;
* reintentos.

Estas decisiones deben tomarse junto con el modelo de persistencia.

### Fairness

Se difirió la definición precisa de:

* calidad de zonas;
* Assignment History;
* RotationScore;
* comportamiento para asistentes sin historial;
* agregación del score de un grupo;
* política determinística ponderada.

### Lifetime de AssignmentEngine

`AssignmentEngine` se registra actualmente como `Singleton` porque no mantiene estado mutable.

La incorporación de fairness no obliga automáticamente a cambiar el lifetime. Se reevaluará únicamente si:

* el motor empieza a conservar estado;
* alguna policy mantiene estado mutable;
* se incorporan dependencias con un lifetime incompatible.

### Estado Failed

El dominio permite marcar una solicitud como `Failed`, pero falta definir:

* qué errores técnicos producen ese estado;
* cómo se persiste;
* qué ocurre con una transacción parcialmente ejecutada;
* qué respuesta recibe la capa externa.

### API y frontend

Se difirió la creación de:

* endpoints HTTP;
* DTOs HTTP;
* consulta de asignaciones;
* Angular;
* flujo de confirmación desde interfaz.

Estas capacidades no eran necesarias para validar el dominio ejecutable de la Etapa 2.

---

## 8. Por qué estamos preparados para iniciar la Etapa 3

La Etapa 2 demuestra que el modelo conceptual puede convertirse en un flujo ejecutable:

```text
Domain protege las reglas locales
→ Application orquesta el proceso
→ Infrastructure implementa los ports
→ Dependency Injection compone las dependencias
→ un escenario válido termina Completed
→ un escenario sin solución termina Rejected
```

Se ha comprobado que:

* las responsabilidades entre capas están separadas;
* los contratos de Application son implementables;
* el dominio puede producir Assignments válidas;
* el sistema puede representar un rechazo de negocio;
* el resultado puede almacenarse mediante adaptadores concretos;
* el flujo completo puede ejecutarse sin base de datos;
* las decisiones pendientes están identificadas.

Por tanto, el siguiente riesgo técnico relevante ya no es si el flujo puede ejecutarse, sino si sus invariantes pueden protegerse con estado persistente y solicitudes concurrentes.

La Etapa 3 deberá enfocarse en:

* modelo de persistencia;
* límites transaccionales;
* constraints únicos;
* implementación con base de datos;
* protección de `INV-01` e `INV-02`;
* consistencia del grupo bajo fallos;
* pruebas de integración con persistencia real;
* pruebas de concurrencia.

---

## 9. Resultado de la Etapa 2

**Resultado: Validación técnica satisfactoria.**

El flujo mínimo de asignación puede ejecutarse de extremo a extremo a nivel de Application utilizando el dominio y adaptadores in-memory.

La solución está preparada para avanzar a la Etapa 3, donde se validará la consistencia global mediante persistencia y concurrencia.
