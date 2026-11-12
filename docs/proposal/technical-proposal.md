# Festival Assignment — Public Technical Proposal

**Public Technical Proposal: Drafted. Organization Review: Pending.**
**MVP Technical Feasibility Gate: Completed — Technically Feasible for Proposal.**
**Stage 5: In Progress.**

This document presents the current technical concept and feasibility evidence.
Organization-specific operating assumptions, architecture, costs, service levels
and production decisions require stakeholder validation.

## Executive summary

Festival Assignment explores a structured way to allocate limited festival
locations to registered Attendees and temporary groups across multiple days.
The proposed model checks whether a group can fit together, considers previous
location experience, and records each completed allocation for future decisions.
Its purpose is to balance experiences over time while respecting physical limits.

The implemented technical core has been evaluated through representative and
stress simulations. Representative results support the project's internal fairness
criterion, valid evaluated scenarios provide strong evidence of protected stored
assignments, and local reference processing is fast. Testing also identified
material risks: request order affects individual outcomes, simultaneous requests
can compete severely, and scattered free spaces may not accommodate a group.

The completed internal review concluded **Technically Feasible for Proposal**:
there is enough evidence to discuss and validate the concept with an organization.
It does not establish production readiness, organizational approval, guaranteed
response times, resolved concurrency or fragmentation, or a final approved architecture.

The proposed next conversation is a review of the model with organizers,
operations stakeholders and technical representatives. It should establish real
capacity, group rules, request timing, fairness expectations and operating needs
before selecting further validation or considering a limited pilot. Aggregate
counts and a non-personal venue layout are sufficient starting inputs.

## Problem and opportunity

A festival may need to accommodate many Attendees within a limited venue whose
areas offer different experiences. Groups may wish to remain together, while
availability and location quality change as requests are processed. Across
several days, consistently favorable or unfavorable experiences may also matter.

The project explores whether a daily assignment policy could make this process
more explicit and traceable, reducing uncertainty around obtaining a location
and reliance on physical queue position. These are opportunities to investigate,
not confirmed problems at any specific organization. No reduction in queue length
or waiting time has been measured.

## Proposed assignment model and fairness

A registered Attendee can request a location alone or as part of a temporary group
for a FestivalDay. A **Spot** is an individual assignable location; a **Zone** is
an area containing Spots. A successful group assignment uses consecutive Spots
in one row and one Zone. Registration remains separate from daily assignment.

The model first respects eligibility and physical feasibility. It places the
whole group or assigns nobody from that request; it does not automatically split
the group. When no valid contiguous block exists, the request may receive a
business rejection even though some individual Spots remain free. A technical
failure is a different outcome and must not be presented as a normal rejection.

Previous experience provides context for the next day's decision. Attendees with
less favorable prior experiences can receive greater priority for a better
experience later, subject to their group's history, eligibility and available
locations. The policy seeks balance across days; it does not give everyone the
same location each day or create an entitlement to a particular area. The meaning
of an acceptable balance requires discussion with the organization.

Completed assignments and their history are stored and can be traced to the
request that produced them. Each Attendee and Spot can have only one assignment
per FestivalDay. Existing completed assignments remain final for that day;
automatic replacement, cancellation and group reconfiguration are not part of
the validated model.

## How it could work

The following is an illustrative service flow, not a finished user interface:

1. An Attendee or group submits a daily assignment request using registered codes.
2. The system identifies the Attendees and validates the request and eligibility.
3. Available locations are checked for a complete contiguous group placement.
4. The assignment policy considers feasible options and previous experience.
5. A complete assignment is stored, or the request receives a clear non-success
   outcome that distinguishes business rejection from technical failure.
6. The resulting history contributes to decisions on later FestivalDays.

Actual request opening, user access, authentication, result presentation and
operational support need separate definition and implementation.

## Potential benefits

- Clearer allocation outcomes and less uncertainty about obtaining a location.
- More structured use of available capacity while preserving feasible groups.
- A way to consider experience across days rather than physical arrival position
  alone; request-order sensitivity remains a known limitation.
- Traceable decisions and recorded history for technical and operational review.
- Simulations that let stakeholders examine policy choices before live operation.

These are expected benefits of the concept, not guaranteed operational outcomes.

## Technical feasibility evidence

The [completed Gate assessment](../stage-5/mvp-technical-feasibility-gate-result.md)
evaluates four dimensions. Its scenarios use provisional inputs, not measured
festival traffic or organization-approved operating assumptions.

| Area | Current evidence | Current position |
| --- | --- | --- |
| Fairness | Representative tested scenarios exceeded the internal **Balanced + Acceptable > 85%** criterion for Attendees with enough recorded assignments to assess a multi-day path. | Supports proposal, with known request-order sensitivity. This is an internal MVP evaluation criterion, not an organization-approved fairness policy or a rate covering every registrant. |
| Reliability | Stored-assignment rules remained protected in valid evaluated scenarios, including severe stress cases. | Strong evidence for persisted integrity within audited cases. This does not establish reliable service under concurrent load; one stopped collection retains audit limitations. |
| Performance | Reference processing handled requests one at a time and comfortably met the internal **p95 < 1 second** hypothesis. Here, p95 describes the processing time within which at least 95% of measured requests finished. | Supports proposal in the tested local environment. It excludes the complete browser/network journey and is not a production service-level guarantee. |
| Capacity utilization | Representative scenarios placed their complete daily demand under the modeled layout, including the tested higher prevalence of large groups. Fragmentation testing showed rejection despite free eligible Spots. | Supports proposal with a material spatial-capacity risk; it does not prove final allocation under every group or reconfiguration rule. |
| Concurrency | Both synchronized stress and aggressive arrivals without synchronization caused severe degradation and reduced successful delivery. | Requires organization-informed validation and applicable mitigation evaluation before production acceptance. These experiments are not production traffic predictions. |

The [technical appendix](technical-appendix.md) explains the evidence boundaries
and links to the authoritative reports without reproducing the full experiment set.

## Known risks and boundaries

| Risk | Meaning for the organization discussion |
| --- | --- |
| Request-order sensitivity | Aggregate fairness may stay similar while the particular people receiving better or worse experiences change substantially with request order. Its acceptability needs policy discussion. |
| Concurrency | Simultaneous requests can compete for the same locations. Stress tests demonstrated severe contention and technical failures under high-rate load. The final coordination strategy has not been selected. |
| Fragmentation | Free individual Spots may be scattered so that no contiguous block fits a group. Reservations or externally occupied areas may affect this; actual operating rules are unknown. |
| Large groups and reconfiguration | The tested large-group mix remained feasible, but late large groups followed by splitting or resubmission have not been validated. These policies cannot be assumed. |

The stored-state audits do not prove every possible execution or operating
condition. The original stopped stress collection remains excluded from the clean
comparison; its observation/audit limitation is retained rather than declared resolved.

Technically plausible responses include coordinating request processing,
database-level coordination, controlled retry, queue-based processing, permitted
group reconfiguration or inventory-selection improvements. **A plausible response
is not a validated solution.** The final strategy should follow validation of the
organization's real operating model; no technology or production design is chosen here.

## What we need from the organization

These are open questions, not assumed answers or a request to publish private data.

| Topic | Information to establish together |
| --- | --- |
| Population and capacity | How many Attendees are registered, how many request each day, and what is the usable daily capacity? |
| Venue and experience | What are the Zones, Rows and Spots? Which areas differ in quality or preference, and what counts as physically contiguous? |
| Daily process and timing | How are requests submitted, when do they open/close, and are they opened gradually or simultaneously? What arrival/request timing is actually observed? |
| Groups | What group sizes are typical, what practical maximum is needed, may groups divide, and may unsuccessful groups resubmit? Would split groups be acceptable? |
| Reserved inventory | Which areas are reserved or externally occupied, including staff and volunteer allocations? |
| Manual operations | Are manual assignments or reassignments expected? How should these needs be reconciled with the current rule that completed assignments remain final? |
| Eligibility | What restrictions determine which Attendees or requests may use each area? |
| Fairness | Which experience differences, recovery patterns and request-order effects are acceptable? How should competing group needs be treated? |
| Service and fallback | What response times and failure handling are acceptable? What operational support and fallback procedures would be needed? |

## Proposed validation approach

Progression should depend on evidence and organization decisions rather than a
delivery date or an assumed production launch.

| Phase | Purpose |
| --- | --- |
| 1. Validate the operating model | Confirm demand, usable inventory, timing, group behavior, eligibility and operational constraints. |
| 2. Calibrate policy | Agree how experience is evaluated, what fairness means, acceptable rejection/failure handling and the questions later tests must answer. |
| 3. Run relevant targeted validation | Use validated inputs to select useful simulations. Plausible arrival concurrency and late-group reconfiguration remain conditional; neither has been executed. |
| 4. Evaluate engineering responses | Compare and validate applicable concurrency/capacity mitigations; repeat affected checks after changes. |
| 5. Define an implementation basis | Use confirmed requirements to evaluate production/cloud architecture, costs and responsibilities in a separate organization-specific process. Finalize and evaluate data-informed stress/worst-plausible scenarios before formal review. |
| 6. Review readiness for a limited pilot | Apply the formal stakeholder/production Decision Gate to the agreed scope, evidence, risks, support and fallback arrangements before authorizing any pilot. |
| 7. Conduct a limited pilot if authorized | Observe real operation within a jointly defined scope and review the results. |
| 8. Make a production decision | Use pilot findings and any further required validation to decide whether and how to proceed. |

## Possible pilot

A pilot could examine real request flow, group behavior, acceptance of assignments,
operational support, response times, capacity use and exception handling. Its
size, duration, participants, success criteria, stop conditions and fallback
arrangements would be agreed with the organization after the preceding validation
and formal review. No pilot is scheduled, authorized or executed by this proposal.

## Public technology overview

| Component | Current position |
| --- | --- |
| Backend | A .NET assignment core and ASP.NET Core host exist. The validated execution uses the real assignment use case; a complete operational web API is not yet delivered. |
| Persistence and validation | PostgreSQL persistence, automated tests and a simulation framework support technical evaluation of assignments and history. |
| Attendee interface | Angular is the planned frontend direction, not an implemented or validated attendee interface. |

This is a public overview of the current project, not a final deployment design.
Cloud/provider selection, authentication and operational tooling remain later work.

## Public and private document boundary

This public technical proposal describes the generic problem, model, evidence,
risks and validation approach. It contains no real attendee records, private
contacts, confidential procedures, credentials, private infrastructure details,
commercial quotations, delivery commitments or organization-specific agreements.

A future **private organization-specific proposal** could record validated
organization data, venue constraints, business rules, operating policies,
architecture/provider choices, costs, schedule, responsibilities and commercial
terms. Such material belongs in a separate private document, not in public
repository discussions or evidence packages. That private proposal is not created here.

## Next step and supporting material

The public proposal is **Drafted** and organization review is **Pending**. The
next preparation task is **Prepare Organization-Facing Proposal Package**: adapt
this material for an initial conversation, potentially including a Spanish
document, presentation/PDF, meeting narrative and stakeholder questions. Those
artifacts and any organization contact remain separate future work.

The repository records the progression from domain modeling and implementation
through simulation, risk discovery, an evidence-based feasibility decision and
this proposal. See the [technical appendix](technical-appendix.md) for that
progression, architecture boundaries and the evidence index.
