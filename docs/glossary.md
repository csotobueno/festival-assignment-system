# Domain Glossary

Este documento define el lenguaje ubicuo del sistema de asignación de ubicaciones
del festival. El [checkpoint de stage-4](stage-4/completion.md) distingue el
baseline implementado de la evaluación y calibración pendientes de stage-5.

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

Stage-4 deriva la calidad global de una Zone desde su ZoneType mediante
ZoneExperienceQualityPolicy. La clasificación es la misma para cada solicitud;
la elegibilidad solo determina si la Zone participa. El
[mapeo acordado](stage-4/assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping)
sustituye la hipótesis inicial de calidad relativa a las opciones elegibles.

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

Cada Assignment tiene una ExperienceQuality explícita, obligatoria, inmutable
y persistida que conserva la calidad real registrada al asignar.

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

Valor derivado del Assignment History que estima la necesidad actual de
recuperación de un Attendee. Un valor mayor representa mayor necesidad; un
asistente sin historial comienza en cero.

La fórmula y los pesos provisionales se definen en
[RotationScore v1](stage-4/rotation-score-v1.md#baseline-formula).
El score no incluye inventario ni determina por sí solo una Zone o Spot.

---

## GroupRotationScore

Promedio aritmético de los RotationScores individuales de todos los integrantes
de una AssignmentGroup. Un integrante sin historial aporta cero. Orienta la
calidad objetivo del grupo, sin reemplazar los historiales individuales.

La fórmula, ejemplos y limitaciones están en
[Group RotationScore](stage-4/rotation-score-v1.md#group-rotationscore).

---

## Experience Quality

Clasificación de la experiencia de una Assignment como `Good`, `Medium` o `Bad`,
derivada de la calidad global de la Zone seleccionada al asignar. La elegibilidad
no cambia esa clasificación. La calidad registrada permanece estable para el
cálculo histórico y no se reconstruye desde la clasificación actual de la Zone.

La [estrategia v1](stage-4/assignment-strategy-v1.md#6-eligibility-aware-experience-quality)
define el mapeo implementado y separa calidad de elegibilidad. La solicitud
completa comparte una RequestEligibility; la elegibilidad individual mixta no
forma parte del baseline.

---

## Target Quality

Calidad objetivo estimada a partir del RotationScore individual o grupal.
Es una referencia para seleccionar una opción disponible, no una garantía ni
un límite máximo. La decisión considera además elegibilidad, factibilidad
e inventario. El estado global de cantidades asignadas no es una entrada de
selección de stage-4.

La política inicial está implementada y definida en
[Assignment Strategy v1](stage-4/assignment-strategy-v1.md#3-determine-target-quality).

---

## Fairness

Propiedad del proceso de asignación mediante la cual las oportunidades de recibir ubicaciones de diferente calidad se distribuyen de manera equilibrada entre los asistentes.

Fairness considera el historial de ubicaciones y busca evitar que los mismos asistentes reciban sistemáticamente las mejores o peores Zones.

[Fairness Definition v1](stage-4/fairness-definition-v1.md) establece la hipótesis
inicial de 19 reglas. Stage-4 implementa reglas ejecutables y escenarios
verificables, con el estado de cierre documentado en el checkpoint.
El [marco de evaluación de stage-5](stage-5/metrics-and-evaluation-criteria.md)
define las poblaciones, PathClassificationRubric v1 y las métricas analíticas
iniciales. Balanced, Acceptable, Not Balanced e Insufficient Evidence son
resultados de evaluación; no modifican las 19 reglas ni las políticas de producción.
La simulación y la calibración con stakeholders permanecen pendientes.

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

La fórmula inicial de RotationScore, contigüidad, mapeo global de calidad,
Target Quality, inventario y selección ya están documentados e implementados.
Permanecen pendientes, cuando el incremento correspondiente las requiera:

* elegibilidad individual mixta y políticas operativas adicionales, si existe una necesidad concreta;
* calibración de los criterios iniciales de stage-5 y resolución versionada de casos límite de su rúbrica, según el [marco de evaluación](stage-5/metrics-and-evaluation-criteria.md);
* horarios operativos concretos de la ventana diaria de asignación;
* tratamiento detallado de errores durante la importación de asistentes.
