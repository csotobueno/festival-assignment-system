# Domain Glossary

Este documento define el lenguaje ubicuo inicial del sistema de asignación de ubicaciones del festival.

Las definiciones describen conceptos del negocio y no decisiones técnicas de implementación.

---

## Festival

Evento completo durante el cual se realizan las asignaciones de ubicaciones.

El festival está compuesto por varios días y mantiene el contexto general de asistentes, zonas y ubicaciones disponibles.

---

## FestivalDay

Día específico del festival en el que los asistentes pueden solicitar y recibir una ubicación.

Cada día constituye un contexto independiente de asignación. Un asistente puede recibir como máximo una ubicación durante un mismo FestivalDay.

Las asignaciones realizadas en un día no impiden que el asistente solicite una nueva ubicación en otro día.

---

## Attendee

Persona previamente registrada por la organización del festival y habilitada para recibir una ubicación.

Un Attendee se identifica mediante un AttendeeCode.

El proceso de inscripción ocurre fuera del sistema de asignación.

---

## AttendeeCode

Código único entregado por la organización para identificar a un Attendee.

Se utiliza para:

* buscar al asistente;
* incluirlo en una AssignmentRequest;
* consultar su Assignment.

Un AttendeeCode inválido, inexistente o duplicado dentro de una misma solicitud debe impedir que la solicitud continúe hasta ser corregida.

---

## Spot

Ubicación individual asignable dentro del recinto del festival.

Cada Spot pertenece a una Zone y se identifica mediante un SpotCode.

Un Spot puede asignarse una sola vez durante un FestivalDay, pero puede volver a utilizarse en días diferentes.

---

## SpotCode

Código único que identifica un Spot dentro del recinto.

Permite comunicar al asistente la ubicación exacta que le fue asignada.

---

## Zone

Sector del recinto que agrupa varios Spots con características similares de ubicación.

Todos los integrantes de un AssignmentGroup deben recibir Spots pertenecientes a la misma Zone.

Las Zones tendrán diferentes niveles de calidad para efectos de rotación y fairness. La clasificación exacta de esa calidad todavía debe definirse.

---

## AssignmentRequest

Solicitud presentada para obtener ubicaciones durante un FestivalDay.

Una AssignmentRequest contiene entre uno y diez AttendeeCodes.

Antes de procesarla, el sistema debe verificar que:

* los códigos existan;
* no haya códigos repetidos;
* los asistentes correspondan a personas registradas;
* ninguno de los asistentes tenga ya una Assignment para ese FestivalDay.

Si uno de los asistentes ya tiene una Assignment para ese día, la solicitud completa debe rechazarse con un mensaje explicativo.

Una AssignmentRequest representa la intención de obtener ubicaciones, no el resultado final.

---

## AssignmentGroup

Conjunto temporal de Attendees que debe ser procesado como una unidad indivisible durante una solicitud de asignación.

Un AssignmentGroup puede cambiar en cada FestivalDay.

Todos sus integrantes deben:

* ser asignados completamente o no ser asignados;
* recibir Spots de la misma Zone;
* recibir Spots físicamente contiguos.

Un AssignmentGroup no representa un grupo permanente de asistentes.

---

## Assignment

Resultado definitivo mediante el cual un Attendee recibe un Spot durante un FestivalDay.

Cada Assignment relaciona:

* un Attendee;
* un Spot;
* un FestivalDay.

Una Assignment completada no puede cancelarse ni modificarse durante ese mismo FestivalDay.

Un Attendee no puede tener más de una Assignment durante un FestivalDay.

Un Spot no puede formar parte de más de una Assignment durante un FestivalDay.

---

## GroupSize

Cantidad de Attendees incluidos en una AssignmentRequest o AssignmentGroup.

El valor permitido se encuentra entre uno y diez.

GroupSize influye en la búsqueda de Spots contiguos disponibles.

---

## Contiguous Spots

Conjunto de Spots físicamente adyacentes que permite mantener juntos a todos los integrantes de un AssignmentGroup.

La contigüidad se define de la siguiente manera:

* pertenecer a la misma fila;
* tener números consecutivos;
* formar un bloque completo igual al GroupSize.

---

## Assignment History

Conjunto de Assignments anteriores de un Attendee en diferentes FestivalDays.

El historial se utiliza como información de entrada para evaluar rotación y fairness.

No incluye múltiples asignaciones dentro de un mismo FestivalDay, porque esa situación no está permitida.

---

## RotationScore

Valor derivado del Assignment History de un Attendee.

Resume su experiencia previa de ubicación y ayuda a comparar su situación con la de otros asistentes al decidir futuras asignaciones.

Todavía deben definirse:

* su escala;
* su fórmula;
* los pesos aplicados;
* la forma de combinarlo dentro de un AssignmentGroup.

---

## Fairness

Propiedad del proceso de asignación mediante la cual las oportunidades de recibir ubicaciones de diferente calidad se distribuyen de manera equilibrada entre los asistentes.

Fairness considera el historial de ubicaciones y busca evitar que los mismos asistentes reciban sistemáticamente las mejores o peores Zones.

La definición matemática y los criterios de aceptación de fairness se establecerán antes de implementar el Assignment Engine completo.

---

# Important distinctions

## AssignmentRequest vs. AssignmentGroup

`AssignmentRequest` representa la intención enviada al sistema.

`AssignmentGroup` representa el conjunto temporal e indivisible de asistentes que debe mantenerse físicamente unido durante la asignación.

`AttendeeCode` es el identificador externo enviado en una `AssignmentRequest`.

`AttendeeId` es la identidad interna utilizada después de que los códigos han sido resueltos.

La protección contra duplicados se aplica en ambos niveles porque diferentes códigos no deben producir el mismo asistente más de una vez dentro de un `AssignmentGroup`.

---

## AssignmentGroup vs. permanent group

Un AssignmentGroup existe solamente para un FestivalDay y una solicitud determinada.

Los asistentes pueden formar grupos diferentes en otros días.

---

## Spot vs. Zone

Un Spot es una ubicación individual y exacta.

Una Zone es un sector que agrupa múltiples Spots y representa una dimensión relevante de la calidad de ubicación.

---

## AssignmentRequest vs. Assignment

Una AssignmentRequest puede ser inválida, rechazada o no producir resultado.

Una Assignment representa una ubicación definitiva ya otorgada a un Attendee.

---

# Deferred definitions

Los siguientes conceptos o decisiones se definirán cuando el proyecto los necesite:

* clasificación de calidad de las Zones;
* representación física de la contigüidad;
* fórmula de RotationScore;
* definición medible de Fairness;
* periodo diario habilitado para presentar AssignmentRequests;
* tratamiento detallado de errores durante la importación de asistentes.
