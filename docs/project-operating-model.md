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
* La [guía de stage-4](stage-4/README.md) contiene la propuesta inicial de fairness,
  su fórmula y su plan de implementación. No constituye evidencia de entrega.

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
4. **Etapa 4 — Motor de asignación y fairness: Próxima.**
5. **Etapa 5 — Simulación y evaluación para la toma de decisiones: Pendiente.**

### Etapa 4 — Motor de asignación y fairness

Objetivo: implementar una base mínima de asignación online determinista y
permitir la medición de sus decisiones y los historiales resultantes.

Los [documentos de stage-4](stage-4/README.md) son una guía inicial. La fórmula
del score está suficientemente concretada para comenzar; el mapeo de calidad y
las reglas de selección se harán explícitos cuando su incremento de
implementación los requiera. La etapa 4 debe terminar con reglas provisionales
reproducibles y escenarios de referencia, no solo con una intención conceptual.
La calibración final y la evaluación cuantitativa de fairness corresponden a la
etapa 5.

Esta etapa definirá Fairness v1 y `RotationScore`, introducirá únicamente las
abstracciones de políticas de asignación cuya necesidad esté demostrada e
implementará la selección de Zone y Spots contiguos mediante una estrategia
ponderada determinista para el MVP. La estrategia debe contar con pruebas
deterministas e integrarse con la base de persistencia ya completada. Se prefiere
el determinismo durante la validación porque permite reproducir las pruebas,
comparar los escenarios y reducir el ruido en el análisis de fairness.
El determinismo no garantiza por sí mismo el fairness.

### Etapa 5 — Simulación y evaluación para la toma de decisiones

Objetivo: evaluar si la estrategia de la etapa 4 es suficientemente justa y
técnicamente viable para justificar la construcción del sistema operativo completo.

Esta etapa ejecutará escenarios de simulación deterministas, medirá el fairness
y la distribución, examinará casos límite, comparará estrategias y registrará
una conclusión sobre la viabilidad del MVP, con sus limitaciones y una
recomendación sobre el siguiente paso. La etapa 5 evalúa la estrategia de
fairness construida en la etapa 4; la implementación central de fairness no se
pospone hasta la etapa 5. Los criterios provisionales y medibles para clasificar
las trayectorias deben registrarse antes de evaluar los resultados de las
simulaciones. El resultado es evidencia para una propuesta a la organización,
incluidas las limitaciones y las decisiones de negocio pendientes; no constituye
aprobación de la organización ni preparación para producción.

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

Criterios de éxito del MVP

1. Ninguna invariante rota.
2. Simulación de 5000 asistentes completada.
3. Tiempo promedio de asignación < 2 segundos.
4. Fairness Score aceptable.
5. Sin asignaciones duplicadas.

Estos objetivos orientan la evaluación de stage-5. “Fairness Score aceptable” es
la formulación inicial del objetivo; no implica que ya exista una métrica escalar
única. Stage-5 concretará los criterios provisionales antes de evaluar resultados,
usando las dimensiones de [Fairness Definition v1](stage-4/fairness-definition-v1.md#stage-5-measurement-dimensions).
La presimulación de stage-4 verifica escenarios y detecta problemas evidentes;
no demuestra por sí sola el cumplimiento de estas metas.
