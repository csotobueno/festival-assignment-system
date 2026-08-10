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

El MVP técnico no tiene como objetivo entregar todavía una aplicación preparada para producción.

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

## 6. Definition of Done del MVP técnico

El MVP técnico estará terminado cuando exista evidencia de que:

* el motor central de asignación es funcional;
* las invariantes críticas permanecen protegidas;
* se pueden ejecutar escenarios representativos;
* se puede medir el fairness;
* se ha evaluado el comportamiento concurrente;
* existe una conclusión documentada sobre la viabilidad técnica.

## 7. Roadmap

1. **Stage 1 — Essential Domain: Completed.**
2. **Stage 2 — Executable Domain/Application: Completed.** Version milestone: `v0.1.0`.
3. **Stage 3 — Persistence, Global Invariants and Concurrency: Completed.** Version milestone: `v0.2.0`.
4. **Stage 4 — Assignment Engine + Fairness: Next.**
5. **Stage 5 — Simulation + Decision Gate: Pending.**

### Stage 4 — Assignment Engine + Fairness

Objective: build and refine the core assignment strategy and define measurable
fairness.

This stage will define Fairness v1 and `RotationScore`, introduce only the
assignment policy abstractions demonstrated to be necessary, and implement Zone
and contiguous Spot selection through a deterministic weighted MVP strategy.
The strategy must have deterministic tests and integrate with the completed
persistence foundation. Determinism is preferred during validation because it
makes tests reproducible, scenarios comparable and fairness analysis less
noisy. Determinism does not itself establish fairness.

### Stage 5 — Simulation + Decision Gate

Objective: evaluate whether the Stage 4 strategy is sufficiently fair and
technically viable to justify building the full operational system.

This stage will run deterministic simulation scenarios, measure fairness and
distribution, examine edge cases, compare strategies and record an MVP
viability conclusion with limitations and a next-step recommendation. Stage 5
evaluates the fairness strategy built in Stage 4; it does not defer the core
fairness implementation to Stage 5.

### Current technical MVP hypothesis

> Can the festival assign valid locations using a sufficiently fair assignment
> policy while preserving the required business invariants?

The technical MVP includes the Assignment Engine, Fairness Definition,
`RotationScore`, justified assignment policies, Zone selection, contiguous Spot
selection, a deterministic weighted strategy, persistence and concurrency
correctness, simulation, fairness evaluation and the decision gate.

The current technical MVP surface excludes the attendee-code validation
endpoint or workflow; Attendee, Spot, Zone and FestivalDay CRUD; administration
endpoints; assignment-query APIs not required for validation; Angular UI;
authentication and authorization; an operational dashboard; advanced
observability; and deployment hardening. Internal attendee-code resolution
remains part of the existing architecture.

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

MVP Success Criteria

1. Ninguna invariante rota.
2. Simulación de 5000 asistentes completada.
3. Tiempo promedio de asignación < 2 segundos.
4. Fairness Score aceptable.
5. Sin asignaciones duplicadas.
