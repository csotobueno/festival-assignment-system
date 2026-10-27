# Project Operating Model

## 1. Visión

Validar que es técnicamente viable realizar asignaciones de ubicaciones justas y consistentes para los asistentes del festival mediante un motor automatizado de asignación.

## 2. Alcance del MVP técnico

Implementar el flujo mínimo necesario para simular diferentes escenarios de asignación y evaluar:

* cumplimiento de las invariantes críticas;
* calidad de la distribución;
* fairness entre asistentes y grupos;
* comportamiento ante solicitudes concurrentes;
* rendimiento con una cantidad representativa de asistentes.

El MVP técnico busca comprobar la viabilidad y fiabilidad de asignar ubicaciones
a diferentes Attendees durante varios días. No tiene como objetivo entregar el
sistema operativo completo ni una aplicación preparada para producción.
Al terminar stage-5, la evidencia y sus limitaciones servirán para proponer el
sistema a la organización y validar con ella las consideraciones operativas y
de negocio pendientes.

## 3. Arquitectura objetivo

Las dependencias seguirán esta dirección:

```text
API → Application → Domain
Infrastructure → Application
Infrastructure → Domain
```

Principio principal:

> Domain no depende de ninguna otra capa.

La arquitectura se concretará progresivamente y solo cuando aparezca código que la necesite.

## 4. Flujo de trabajo Git

* `main`: estado estable e integrado del proyecto.
* `docs/*`: cambios exclusivamente documentales.
* `feature/*`: nuevas capacidades.
* `fix/*`: correcciones.
* `hotfix/*`: correcciones urgentes de producción.
* `chore/*`: mantenimiento de tooling y del repositorio.
* `test/*`: cambios exclusivamente de pruebas.
* `refactor/*`: mejoras internas que preservan el comportamiento.

Cada trabajo comienza desde el último `origin/main` con
`make start branch=<type>/<description>` y se integra mediante Pull Request.
El flujo completo y las convenciones están documentados en
[`CONTRIBUTING.md`](../CONTRIBUTING.md).

## 5. Documentación esencial

Durante el MVP técnico se crearán únicamente documentos necesarios para reducir incertidumbre, justificar decisiones relevantes o preservar conocimiento indispensable.

Documentación inicialmente prevista:

* `README.md`
* `docs/project-operating-model.md`
* `docs/glossary.md`
* `docs/critical-invariants.md`
* `docs/domain-blueprint-v1.md`

Otros documentos se crearán solo cuando exista una necesidad comprobada.

### Responsabilidad y evolución documental

* El glosario define los términos y enlaza sus reglas detalladas.
* Las invariantes definen las condiciones que ninguna política puede vulnerar.
* El blueprint y los informes de etapas anteriores conservan el contexto y las
  decisiones de su momento; una actualización posterior se identifica mediante
  notas y enlaces, sin reescribir retrospectivamente esa evidencia.
* El cierre de stage-3 y el modelo relacional describen la base persistente
  implementada.
* La [guía de stage-4](stage-4/README.md) enlaza las reglas provisionales y su plan
  de implementación. El [checkpoint de cierre](stage-4/completion.md) consolida
  el baseline implementado, la evidencia verificada y la decisión Lean de diferir
  la evaluación de cinco días a stage-5; es la fuente autoritativa del cierre.
* El [marco de evaluación de stage-5](stage-5/metrics-and-evaluation-criteria.md)
  es la fuente autoritativa de poblaciones, rúbrica analítica versionada,
  métricas, hipótesis y requisitos de reproducibilidad del Decision Gate.
* La [arquitectura de simulación de stage-5](stage-5/simulation-architecture.md)
  define la composición prevista, el ciclo de vida PostgreSQL, los manifiestos
  reproducibles y la trazabilidad de evidencia; su implementación sigue pendiente.

Cuando una decisión evolucione, se documentará qué cambia, por qué y dónde queda
la definición posterior. Las guías vigentes se actualizarán sin borrar el
registro histórico de decisiones.

## 6. Criterios de finalización del MVP técnico

El MVP técnico estará terminado cuando exista evidencia de que:

* el motor central de asignación es funcional;
* las invariantes críticas permanecen protegidas;
* se pueden ejecutar escenarios representativos;
* se puede medir el fairness;
* se ha evaluado el comportamiento concurrente;
* existe una conclusión documentada sobre la viabilidad técnica.

## 7. Hoja de ruta

1. **Etapa 1 — Dominio esencial: Completada.**
2. **Etapa 2 — Dominio y Application ejecutables: Completada.** Hito de versión: `v0.1.0`.
3. **Etapa 3 — Persistencia, invariantes globales y concurrencia: Completada.** Hito de versión: `v0.2.0`.
4. **Etapa 4 — Motor de asignación y fairness: Completada.** Véase el [cierre de stage-4](stage-4/completion.md).
5. **Etapa 5 — Simulación y evaluación para la toma de decisiones: Pendiente.**

### Etapa 4 — Motor de asignación y fairness

Objetivo: implementar una base mínima de asignación online determinista y
permitir la medición de sus decisiones y los historiales resultantes.

Los [documentos de stage-4](stage-4/README.md) describen el baseline online
determinista integrado en Application: Fairness v1, `RotationScore`, calidad
global de Zone, elegibilidad, inventario, selección de bloques contiguos y
calidad histórica persistida. El [checkpoint](stage-4/completion.md) registra
los siete escenarios de aceptación integrados y las suites verificadas. Todos
los criterios de salida vigentes están satisfechos. La presimulación de cinco
días antes exigida por el paso 26 se difiere explícitamente a la etapa 5 para
evitar duplicar preparación, escenarios e interpretación. La etapa 4 está
Completada y la etapa 5 permanece Pendiente.

La calibración final y la evaluación cuantitativa de fairness corresponden a la
etapa 5. El determinismo permite reproducir pruebas y comparar escenarios;
no garantiza por sí mismo el fairness.

### Etapa 5 — Simulación y evaluación para la toma de decisiones

Objetivo: evaluar si la estrategia de la etapa 4 es suficientemente justa y
técnicamente viable para justificar la construcción del sistema operativo completo.

Esta etapa ejecutará escenarios de simulación deterministas, medirá el fairness
y la distribución, examinará casos límite, comparará estrategias y registrará
una conclusión sobre la viabilidad del MVP, con sus limitaciones y una
recomendación sobre el siguiente paso. La etapa 5 evalúa la estrategia de
fairness construida en la etapa 4; la implementación central de fairness no se
pospone hasta la etapa 5. El [marco de evaluación](stage-5/metrics-and-evaluation-criteria.md)
registra los criterios analíticos iniciales; cada ejecución debe identificar la
versión de la rúbrica y resolver o informar sus casos límite antes de emitir
una conclusión cuantitativa de fairness. El resultado es evidencia para una
propuesta a la organización, incluidas las limitaciones y las decisiones de
negocio pendientes; no constituye
aprobación de la organización ni preparación para producción.

La simulación de cinco días de esta etapa incorpora las preocupaciones antes
asignadas al paso 26 de stage-4: trayectorias Bad persistentes, asistentes que
nunca reciben Good, capacidad Good sin utilizar o consumida demasiado pronto,
comportamiento patológico de grupos entre poblaciones, sensibilidad al orden de
llegada y comportamiento inesperado del score entre días y poblaciones. También
evalúa fairness estadístico, presión de capacidad, rendimiento y concurrencia/
estrés antes del Decision Gate final. La [decisión Lean de cierre](stage-4/completion.md#lean-decision-on-five-day-evaluation)
separa esta evaluación empírica de las pruebas funcionales que permanecen como
evidencia de stage-4.

### Hipótesis actual del MVP técnico

> ¿Puede el festival asignar ubicaciones válidas mediante una política de
> asignación suficientemente justa, preservando las invariantes de negocio
> requeridas?

El MVP técnico incluye el motor de asignación, la definición de fairness,
`RotationScore`, políticas de asignación justificadas, selección de Zone,
selección de Spots contiguos, una estrategia ponderada determinista, corrección
de la persistencia y la concurrencia, simulación, evaluación de fairness y la
evaluación para la toma de decisiones.

El alcance actual del MVP técnico excluye el endpoint o flujo de validación de
códigos de asistentes; el CRUD de Attendee, Spot, Zone y FestivalDay; los endpoints
de administración; las APIs de consulta de asignaciones que no sean necesarias
para la validación; la interfaz Angular; la autenticación y autorización; un
panel operativo; la observabilidad avanzada; y el endurecimiento del despliegue.
La resolución interna de códigos de asistentes sigue formando parte de la
arquitectura existente.

## 8. Principio Lean

Cada artefacto, tarea o decisión debe contribuir al menos a uno de estos objetivos:

* reducir incertidumbre;
* proteger la calidad;
* habilitar una validación;
* entregar una capacidad ejecutable;
* preservar una decisión importante;
* consolidar aprendizaje aplicable.

Lo que no contribuya a alguno de estos objetivos se pospondrá o eliminará.

## 9. Métricas de éxito

Objetivos iniciales de planificación del MVP, conservados como contexto histórico:

1. Ninguna invariante rota.
2. Simulación de 5000 asistentes completada.
3. Tiempo promedio de asignación < 2 segundos.
4. Fairness Score aceptable.
5. Sin asignaciones duplicadas.

El [marco de evaluación de stage-5](stage-5/metrics-and-evaluation-criteria.md)
refina estos objetivos y es la autoridad vigente para métricas y criterios;
el promedio de dos segundos y “Fairness Score aceptable” no constituyen puertas
de aceptación adicionales ni una métrica escalar implementada.

La referencia física actual del recinto es aproximadamente 5,000 personas.
Una población registrada de aproximadamente 5,500 corresponde a una variante
de crecimiento / sobresuscripción, no a capacidad física confirmada. La demanda
diaria depende además de la participación; cada manifiesto registra población
y capacidad por separado.

Los siete escenarios de aceptación de stage-4 verifican comportamiento funcional
determinista y conservan su evidencia histórica. La evaluación de cinco días y
el Decision Gate completan la evaluación del MVP técnico; una conclusión Continue
apoya la siguiente fase de propuesta o implementación, sin iniciarla automáticamente.
