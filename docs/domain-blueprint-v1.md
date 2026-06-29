# Domain Blueprint v1

## 1. Purpose

Este documento consolida el modelo inicial del dominio para el MVP técnico del sistema de asignación de ubicaciones del festival.

Su objetivo es establecer una base suficientemente clara para comenzar la implementación sin introducir decisiones técnicas prematuras.

Este Blueprint representa el entendimiento actual del dominio y podrá evolucionar a medida que la implementación y las simulaciones revelen nueva información.

---

## 2. Technical MVP Objective

Validar que es técnicamente viable realizar asignaciones de ubicaciones:

* justas;
* consistentes;
* completas para grupos;
* resistentes a solicitudes concurrentes;
* suficientemente rápidas para el volumen esperado.

El MVP técnico no busca entregar todavía una aplicación lista para producción.

---

## 3. Business Context

El registro de asistentes ocurre fuera del sistema.

La organización entrega un archivo con los asistentes registrados y sus códigos únicos.

Durante cada FestivalDay:

1. Se habilita una ventana diaria de asignación.
2. Una persona ingresa entre uno y diez AttendeeCodes.
3. El sistema identifica a los Attendees.
4. La persona confirma el grupo.
5. Se crea una AssignmentRequest.
6. El sistema valida la solicitud.
7. El Assignment Engine intenta encontrar un bloque válido de Spots.
8. La solicitud termina como Completed, Rejected o Failed.
9. Los resultados exitosos pueden consultarse mediante el AttendeeCode.

Los grupos pueden cambiar en cada FestivalDay.

---

## 4. Main Domain Flow

```text
Attendee data imported
        ↓
AttendeeCodes entered
        ↓
Attendees identified
        ↓
Group confirmed
        ↓
AssignmentRequest created
        ↓
Request validated
        ↓
AssignmentGroup formed
        ↓
Assignment Engine executed
        ↓
Assignments created or request rejected
        ↓
Result returned and persisted
```

---

## 5. Core Domain Concepts

### FestivalDay

Representa un día específico del festival.

Cada FestivalDay tiene:

* una ventana diaria de asignación;
* disponibilidad independiente;
* Assignments propias;
* reglas de unicidad por día.

### Attendee

Persona registrada y habilitada para recibir una ubicación.

Se identifica mediante AttendeeCode.

### Spot

Ubicación física individual dentro del recinto.

Cada Spot debe tener:

* SpotCode;
* Zone;
* RowCode;
* SpotNumber.

### Zone

Sector del recinto que agrupa Spots con una calidad de ubicación similar.

### AssignmentRequest

Intento concreto de obtener Spots para uno o varios Attendees durante un FestivalDay.

Tiene identidad, estado, momento de creación y resultado auditable.

### AssignmentGroup

Grupo temporal e indivisible formado para una AssignmentRequest.

No requiere por ahora identidad ni persistencia independiente.

### Assignment

Resultado definitivo que relaciona:

* un Attendee;
* un Spot;
* un FestivalDay;
* la AssignmentRequest que lo originó.

### RotationScore

Valor derivado del Assignment History de un Attendee.

Ayuda a comparar experiencias anteriores al tomar nuevas decisiones de asignación.

### Fairness

Propiedad del proceso mediante la cual la calidad de las ubicaciones se distribuye de forma equilibrada entre los asistentes a lo largo del festival.

---

## 6. Initial Concept Classification

### Entities

* FestivalDay
* Attendee
* Spot
* Zone
* AssignmentRequest
* Assignment

`Festival` es conceptualmente una Entity, pero no se implementará inicialmente porque el MVP representa una única edición específica.

### Value Objects

* AttendeeCode
* SpotCode
* RowCode
* SpotNumber
* GroupSize
* AssignmentWindow
* RotationScore

### Derived or Supporting Concepts

* Assignment History: colección o proyección de Assignments anteriores.
* Contiguous Spots: condición sobre una colección de Spots.
* Fairness: concepto de calidad expresado posteriormente mediante políticas y métricas.

### Policies and Services

* RotationPolicy: Policy candidata.
* AssignmentEngine: Domain Service candidato.

Las políticas de selección de Zone y Spot se definirán cuando la implementación las necesite.

---

## 7. Responsibility Model

### AssignmentRequest

Aggregate Root candidato.

Responsabilidades:

* representar un intento de asignación;
* conservar los AttendeeCodes solicitados;
* registrar el FestivalDay;
* preservar su estado;
* registrar motivos de rechazo o fallo;
* controlar transiciones válidas;
* vincular el intento con las Assignments producidas.

Estados mínimos:

```text
Received
Completed
Rejected
Failed
```

Transiciones permitidas:

```text
Received → Completed
Received → Rejected
Received → Failed
```

Una nueva tentativa siempre crea una nueva AssignmentRequest.

### AssignmentGroup

Objeto interno del dominio.

Responsabilidades:

* representar a los integrantes confirmados;
* proteger GroupSize;
* evitar integrantes duplicados;
* exigir resultado completo;
* exigir una única Zone;
* exigir Spots de la misma fila;
* exigir SpotNumbers consecutivos.

No tiene por ahora:

* identidad independiente;
* repositorio propio;
* ciclo de vida separado;
* reutilización fuera de AssignmentRequest.

### Assignment

Entity de resultado.

Responsabilidades:

* relacionar un Attendee, un Spot y un FestivalDay;
* conservar la AssignmentRequest que la originó;
* representar un resultado definitivo;
* alimentar consultas, auditoría e historial.

### AssignmentEngine

Domain Service candidato.

Responsabilidad conceptual:

```text
AssignmentGroup
+ Available Spots
+ Assignment History
+ Assignment Policies
↓
Valid Assignments or no valid solution
```

No persiste directamente el resultado ni controla el ciclo de vida completo de AssignmentRequest.

---

## 8. Critical Invariants

### INV-01 — Unique Spot per FestivalDay

Un Spot puede pertenecer a una sola Assignment durante un FestivalDay.

### INV-02 — Unique Attendee per FestivalDay

Un Attendee puede tener una sola Assignment durante un FestivalDay.

### INV-03 — Complete AssignmentGroup

Todos los integrantes reciben una Assignment o ninguno la recibe.

### INV-04 — Single Zone per AssignmentGroup

Todos los integrantes deben recibir Spots de la misma Zone.

### INV-05 — Contiguous Spots

Los Spots del grupo deben:

* pertenecer a la misma fila;
* tener números consecutivos;
* formar un bloque igual al GroupSize.

### INV-06 — Final Assignment

Una Assignment completada no puede modificarse, cancelarse ni reemplazarse durante el mismo FestivalDay.

### INV-07 — Complete Assignment Identity

Cada Assignment debe relacionar exactamente un Attendee, un Spot y un FestivalDay.

### INV-08 — AssignmentRequest Outcome Consistency

Una AssignmentRequest debe mantener consistencia entre su estado y su resultado:

* Completed: exactamente una Assignment por Attendee solicitado.
* Rejected: ninguna Assignment.
* Failed: ninguna Assignment confirmada.
* Nunca deben persistirse Assignments parciales.

---

## 9. Request and Use-Case Validations

Antes de ejecutar el Assignment Engine:

* la solicitud debe contener entre uno y diez AttendeeCodes;
* todos los códigos deben existir;
* no debe haber códigos duplicados;
* ningún Attendee puede estar ya asignado ese día;
* la solicitud debe encontrarse dentro de la ventana habilitada;
* el FestivalDay debe ser válido.

Estas validaciones mejoran el flujo, pero no sustituyen la protección transaccional de las invariantes.

---

## 10. Request Outcomes

### Completed

La solicitud produjo todas las Assignments esperadas.

### Rejected

El sistema funcionó correctamente, pero una condición de negocio impidió asignar.

Ejemplos:

* ATTENDEE_ALREADY_ASSIGNED
* ATTENDEE_NOT_FOUND
* DUPLICATE_ATTENDEE_CODE
* OUTSIDE_ASSIGNMENT_WINDOW
* CONTIGUOUS_SPOTS_NOT_AVAILABLE

### Failed

El sistema no pudo confirmar un resultado debido a una causa técnica.

Ejemplos:

* DATABASE_UNAVAILABLE
* PROCESSING_TIMEOUT
* CONCURRENCY_CONFLICT
* UNEXPECTED_ERROR

Los rechazos y los fallos deben distinguirse porque representan evidencias diferentes para validar la viabilidad del proyecto.

---

## 11. Contiguous Availability Decision

Cuando no exista un bloque contiguo para todo el grupo:

* se rechaza la AssignmentRequest;
* no se crea ninguna Assignment;
* se registra el motivo `CONTIGUOUS_SPOTS_NOT_AVAILABLE`;
* se informa al usuario;
* el usuario decide si presenta una nueva solicitud con otro grupo.

El sistema no:

* divide automáticamente el grupo;
* mantiene una lista de espera;
* reserva capacidad;
* procesa nuevamente la solicitud en otro momento;
* modifica la composición enviada.

---

## 12. Audit Model

Todas las AssignmentRequests deben persistirse, incluyendo:

* Completed;
* Rejected;
* Failed.

Cada Assignment debe conservar el AssignmentRequestId que la originó.

La colección de Assignments producida por una solicitud puede reconstruirse mediante consulta y no necesita duplicarse como una copia dentro de AssignmentRequest.

---

## 13. Consistency Boundaries

### Local rules

Pueden protegerse dentro de objetos del dominio:

* tamaño válido del grupo;
* miembros no duplicados;
* transiciones de estado;
* resultado completo;
* misma Zone;
* contigüidad;
* identidad completa de Assignment.

### Global rules

Requieren coordinación con persistencia y concurrencia:

* Spot único por FestivalDay;
* Attendee único por FestivalDay;
* persistencia atómica de todas las Assignments;
* consistencia entre el resultado y el estado de AssignmentRequest.

La estrategia técnica se definirá durante las etapas de implementación.

---

## 14. Lean Decisions

El MVP técnico no incluirá inicialmente:

* múltiples ediciones del Festival;
* Entity Festival implementada;
* Entity Row;
* procesamiento asíncrono;
* colas o workers;
* lista de espera;
* división automática de grupos;
* modificaciones administrativas;
* recomendaciones automáticas de tamaño de grupo;
* PriorityScore;
* infraestructura de Domain Events;
* políticas separadas sin necesidad demostrada.

Estas capacidades se considerarán solo si la evidencia obtenida durante la implementación y simulación demuestra que son necesarias.

---

## 15. Operational Preconditions

Para ejecutar simulaciones realistas, la organización debe proporcionar o validar una estructura de Spots con:

* Zone;
* RowCode;
* SpotNumber;
* SpotCode;
* clasificación futura de calidad de Zone.

Sin esta información no es posible validar la contigüidad física ni medir correctamente la rotación entre zonas.

---

## 16. Open Decisions

Las siguientes decisiones permanecen abiertas:

* clasificación de calidad de las Zones;
* fórmula de RotationScore;
* definición medible de Fairness;
* estrategia de selección de Zones;
* estrategia de selección de bloques de Spots;
* mecanismo de concurrencia;
* restricciones de persistencia;
* comportamiento ante conflictos concurrentes;
* umbral para migrar de procesamiento síncrono a asíncrono;
* tratamiento futuro de excepciones administrativas.

Estas decisiones no bloquean el inicio del backend base, pero deberán resolverse antes de completar el Assignment Engine.

---

## 17. Current Architecture Hypothesis

```text
AssignmentRequest
= Aggregate Root candidate

AssignmentGroup
= Internal domain object

Assignment
= Persisted result Entity

AssignmentEngine
= Domain Service candidate

RotationPolicy
= Domain Policy candidate
```

Esta hipótesis será validada mediante código y pruebas durante las siguientes etapas.

---

## 18. Stage 1 Completion Statement

La Etapa 1 se considera terminada cuando:

* el lenguaje ubicuo es consistente;
* las invariantes críticas están documentadas;
* las responsabilidades principales están separadas;
* existe una hipótesis explícita de Aggregates;
* las incertidumbres técnicas están registradas;
* el modelo es suficiente para comenzar una implementación mínima.

Este Blueprint cumple esa función como versión inicial del modelo de dominio.
