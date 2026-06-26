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

El trabajo se integrará mediante Pull Requests.

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

1. Dominio esencial.
2. Backend base y dominio ejecutable.
3. Invariantes y reglas.
4. Assignment Engine y fairness.
5. Simulación y Decision Gate.

## 8. Principio Lean

Cada artefacto, tarea o decisión debe contribuir al menos a uno de estos objetivos:

* reducir incertidumbre;
* proteger la calidad;
* habilitar una validación;
* entregar una capacidad ejecutable;
* preservar una decisión importante;
* consolidar aprendizaje aplicable.

Lo que no contribuya a alguno de estos objetivos se pospondrá o eliminará.
