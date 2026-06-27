# Critical Invariants

Este documento define las invariantes críticas del dominio del sistema de asignación de ubicaciones del festival.

Una invariante es una condición que debe mantenerse verdadera en todo momento. Si una invariante se rompe, el estado del sistema deja de ser válido.

Las validaciones de entrada y las políticas de selección se documentan por separado, ya que cumplen responsabilidades diferentes.

---

## INV-01 — Unique Spot per FestivalDay

### Rule

Un Spot puede pertenecer a una sola Assignment durante un mismo FestivalDay.

### Valid state

```text
FestivalDay 1
Spot A-001 → Attendee X
```

### Invalid state

```text
FestivalDay 1
Spot A-001 → Attendee X
Spot A-001 → Attendee Y
```

### Main risk scenarios

* Dos solicitudes concurrentes seleccionan el mismo Spot.
* Una operación se reintenta después de un error de comunicación.
* La misma solicitud se procesa más de una vez.
* La disponibilidad se consulta antes de que otra operación confirme.

### Expected behavior

La operación que intenta producir la segunda Assignment debe fallar sin modificar las Assignments ya confirmadas.

---

## INV-02 — Unique Attendee per FestivalDay

### Rule

Un Attendee puede tener una sola Assignment durante un mismo FestivalDay.

### Valid state

```text
FestivalDay 1
Attendee X → Spot A-001
```

### Invalid state

```text
FestivalDay 1
Attendee X → Spot A-001
Attendee X → Spot B-010
```

### Main risk scenarios

* El Attendee aparece en dos solicitudes simultáneas.
* Una solicitud ya procesada se envía nuevamente.
* Una nueva solicitud contiene a un Attendee previamente asignado.
* Dos operaciones validan disponibilidad antes de que alguna confirme.

### Expected behavior

La solicitud que intenta crear la segunda Assignment debe rechazarse completamente.

---

## INV-03 — Complete AssignmentGroup

### Rule

Todos los integrantes de un AssignmentGroup reciben una Assignment o ninguno la recibe.

### Valid states

```text
AssignmentGroup: A, B, C

A → Assigned
B → Assigned
C → Assigned
```

También es válido:

```text
A → Not assigned
B → Not assigned
C → Not assigned
```

### Invalid state

```text
A → Assigned
B → Assigned
C → Not assigned
```

### Main risk scenarios

* No existen suficientes Spots contiguos.
* Ocurre un error mientras se persisten las Assignments.
* Otro grupo ocupa uno de los Spots seleccionados.
* Las Assignments se procesan individualmente y no como una unidad.

### Expected behavior

Si no es posible completar todas las Assignments del grupo, no debe confirmarse ninguna.

---

## INV-04 — Single Zone per AssignmentGroup

### Rule

Todas las Assignments de un AssignmentGroup deben pertenecer a la misma Zone.

### Valid state

```text
Attendee A → Zone Front / Spot F-001
Attendee B → Zone Front / Spot F-002
Attendee C → Zone Front / Spot F-003
```

### Invalid state

```text
Attendee A → Zone Front
Attendee B → Zone Front
Attendee C → Zone Middle
```

### Main risk scenarios

* No existen suficientes Spots dentro de una sola Zone.
* El proceso intenta completar el grupo utilizando otra Zone.
* La disponibilidad se evalúa individualmente y no como bloque.

### Expected behavior

Una Zone que no pueda alojar al grupo completo debe descartarse para esa solicitud.

---

## INV-05 — Contiguous Spots

### Rule

Los Spots asignados a un AssignmentGroup deben:

* pertenecer a la misma fila;
* tener números consecutivos;
* formar un bloque completo igual al GroupSize.

### Valid state

```text
Row A
Spots 10, 11, 12, 13
```

### Invalid states

```text
Row A
Spots 10, 11, 14, 15
```

```text
Row A: Spots 10, 11
Row B: Spots 10, 11
```

Aunque los Spots pertenezcan a la misma Zone, dividir el grupo entre filas no está permitido para el MVP técnico.

### Main risk scenarios

* Solo se valida la cantidad total de Spots disponibles.
* Otro grupo ocupa un Spot intermedio.
* La posición física de los Spots no se representa correctamente.
* Se consideran contiguos Spots separados por pasillos u otras divisiones.

### Expected behavior

Un conjunto que no cumpla todas las condiciones de contigüidad debe descartarse.

---

## INV-06 — Final Assignment

### Rule

Una Assignment completada no puede cancelarse, reemplazarse ni modificarse durante el mismo FestivalDay.

### Valid state

```text
Attendee X → Spot A-001
```

La Assignment permanece sin cambios durante el FestivalDay.

### Invalid state

```text
Attendee X
A-001 → replaced by B-010
```

### Main risk scenarios

* Una nueva solicitud intenta obtener una mejor ubicación.
* Una operación elimina o reemplaza una Assignment existente.
* Una corrección manual modifica silenciosamente el resultado.

### Expected behavior

Toda operación de cancelación, reemplazo o modificación debe rechazarse.

Las excepciones administrativas quedan fuera del alcance del MVP técnico y podrán evaluarse en una etapa futura.

---

## INV-07 — Complete Assignment Identity

### Rule

Cada Assignment debe relacionar exactamente:

* un Attendee;
* un Spot;
* un FestivalDay.

### Invalid states

```text
Attendee X → Spot A-001 → FestivalDay undefined
```

```text
Attendee undefined → Spot A-001 → FestivalDay 1
```

```text
Attendee X → Spot undefined → FestivalDay 1
```

### Expected behavior

No puede existir una Assignment incompleta.

---

# Request and Use-Case Validations

Las siguientes reglas deben validarse antes de intentar realizar una asignación, pero no sustituyen la protección de las invariantes.

## VAL-01 — Valid GroupSize

Una AssignmentRequest debe contener entre uno y diez AttendeeCodes.

## VAL-02 — Existing Attendees

Todos los AttendeeCodes incluidos deben corresponder a Attendees registrados.

## VAL-03 — No Duplicate Codes

Una AssignmentRequest no puede contener el mismo AttendeeCode más de una vez.

## VAL-04 — Attendee Not Previously Assigned

Ningún Attendee incluido puede tener una Assignment para el FestivalDay solicitado.

Si al menos uno ya tiene una Assignment, la solicitud completa debe rechazarse con un mensaje explicativo.

## VAL-05 — Enabled Assignment Window

Una AssignmentRequest solo puede presentarse y procesarse dentro de la ventana diaria habilitada del FestivalDay.

---

# Invariants vs. Validations

Una validación detecta anticipadamente que una operación no debería continuar.

Una invariante protege la consistencia del sistema incluso cuando:

* existe concurrencia;
* se repite una solicitud;
* ocurre un error parcial;
* una validación previa queda desactualizada;
* varias operaciones compiten por los mismos recursos.

Por ejemplo, comprobar que un Spot está disponible antes de guardar no garantiza por sí solo `INV-01`, porque otra operación podría asignarlo entre la consulta y la confirmación.

---

# Deferred Technical Decisions

Durante las etapas de implementación se decidirá cómo proteger las invariantes mediante una combinación de mecanismos, que podría incluir:

* responsabilidades de Aggregates;
* transacciones;
* restricciones de unicidad;
* control de concurrencia;
* operaciones atómicas;
* pruebas automatizadas.

Este documento no prescribe todavía una solución técnica.
