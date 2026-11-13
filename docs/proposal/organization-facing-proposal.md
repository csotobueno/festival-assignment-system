# Festival Assignment — Organization-Facing Proposal

**Organization-Facing Proposal Package: Drafted. Organization Review: Pending.**
**Public Technical Proposal: Drafted. MVP Technical Feasibility Gate: Completed.**
**Stage 5: In Progress.**

## Executive Summary

Festival Assignment explores how limited daily festival capacity could be
allocated more fairly to registered Attendees and temporary groups. The model
first checks whether a whole group can fit together, then considers previous
location experience to seek a better balance across FestivalDays.

Technical feasibility has been evaluated through simulations and tests. The
internal Gate concluded **Technically Feasible for Proposal**: sufficient
technical evidence exists to justify organization-level evaluation. Known risks
remain open. The next step is to validate operating assumptions with the
organization, whose actual demand, venue rules and fairness expectations are
still unknown.

## Problem / Opportunity

Daily capacity is limited, locations offer different experiences, and groups may
want to stay together. The project explores whether structured allocation could
reduce uncertainty about obtaining a location and dependence on physical queues
or arrival order, while balancing experience over several days. These are
opportunities to investigate, not confirmed problems at a particular festival.

## Proposed Model

An Attendee requests a location alone or with a temporary group for one
FestivalDay. A **Spot** is an individual assignable location; a **Zone** is an area
containing Spots. The system checks eligibility and available contiguous Spots
before considering experience preferences.

A successful group receives consecutive Spots in one row and one Zone. The
whole group is assigned or nobody from that request is assigned; the model does
not automatically split groups. Each Attendee and Spot can have only one
Assignment per FestivalDay. Completed Assignments are stored, linked to their
request for audit, and remain final for that day.

Fairness means seeking balance across days: less favorable previous experiences
can increase priority for a better later experience, taking the group's histories
into account. This does not guarantee a particular location or equal outcomes.
Eligibility and physical availability remain constraints, and the organization
must help define what an acceptable balance means.

## How the Experience Could Work

1. An Attendee or group submits a daily request.
2. The system validates the request and eligibility.
3. Available contiguous locations are evaluated for the whole group.
4. The policy considers previous experience among the feasible options.
5. A complete Assignment is stored, or a clear non-success result distinguishes
   a business rejection from a technical failure.
6. Recorded experience informs decisions on later FestivalDays.

This is a conceptual flow. The attendee interface, authentication, notification
channel and operational workflow have not been validated as a complete service.

## Potential Benefits

- Clearer outcomes and reduced uncertainty about obtaining a location.
- Less reliance on physical arrival order, with request-order risk still open.
- Structured capacity allocation that supports keeping groups together.
- More balanced multi-day experience and traceable decisions.
- A way to test allocation policies before live operation.

These are potential benefits. Queue reduction has not been measured, and fairness
is not guaranteed.

## What Has Already Been Validated

The [completed Gate assessment](../stage-5/mvp-technical-feasibility-gate-result.md)
reviews four dimensions using modeled inputs, not confirmed organization data.

| Area | Current Position |
| --- | --- |
| Fairness | Representative scenarios support the internal MVP criterion for Attendees with enough recorded experience to assess balance across days. Organizational acceptance and individual request-order effects remain open. |
| Reliability | Stored-assignment rules remained protected in valid audited scenarios. This supports integrity within tested cases, not reliable service under all operating conditions. |
| Performance | Strong local reference performance when requests were processed one at a time. Complete attendee response times and production capacity are unvalidated. |
| Capacity Utilization | Representative demand was fully placed in the modeled layout, including a tested larger-group mix. Scattered free Spots caused rejection in fragmentation tests; final allocation after group reconfiguration is untested. |

Concurrency stress also demonstrated severe service degradation. The evidence
does not predict real festival traffic. One stopped stress collection retains
audit limitations; it is not treated as a clean result. Detailed findings remain
in the [technical appendix](technical-appendix.md) and its source reports.

## Known Risks and Boundaries

| Risk | What remains open |
| --- | --- |
| Request-order sensitivity | Specific Attendees can receive different outcomes depending on request order even when aggregate fairness remains relatively stable. Acceptability requires stakeholder discussion. |
| Shared-inventory concurrency | Concurrent requests compete for the same inventory. Aggressive concurrent and high-rate stress caused severe degradation, including technical failures. Plausible festival concurrency is unvalidated; the final coordination strategy is not selected. |
| Fragmentation | Remaining capacity may contain no contiguous block large enough for a group. Reservations and external occupancy may matter; actual venue rules are unknown. |
| Large groups / reconfiguration | Real group-size behavior and split/resubmission rules are unknown. Tested large-group placement does not validate late groups or final allocation after reconfiguration. |

None of these risks is solved by this proposal. Current completed Assignments
cannot be cancelled or replaced; any requested operating exceptions require
separate review.

## What We Need to Learn From the Organization

Establish actual daily demand and usable capacity, venue layout and reservations,
request opening and peak timing, group behavior, eligibility, fairness expectations,
manual exceptions, and acceptable response and fallback behavior. Current modeled
numbers are not organization values.

The [stakeholder questions](stakeholder-questions.md) guide this discovery.
Aggregate counts and a non-personal venue layout are sufficient starting inputs;
organization-specific answers belong outside the public repository.

## Recommended Next Step

Validate the operating assumptions with the organization and determine whether
the concept merits a data-informed pilot/design phase. Interest would guide
targeted validation and evaluation of applicable mitigations. Plausible arrival
concurrency and late-group reconfiguration remain conditional, unexecuted work.
The later formal review and data-informed stress/worst-plausible evaluation remain
pending before any pilot or production decision. This package authorizes neither.

## Decision Boundary

> **Technically Feasible for Proposal ≠ Production Ready ≠ Organization Approved
> ≠ Ready to Go Live**

Technical mitigation paths exist; that does not mean a mitigation has been
selected or validated. This package supports discussion and decisions; it is not
a production specification, commercial proposal, contract, final architecture
document or go-live plan.

## Reading Guide and Public Boundary

| Document | Role |
| --- | --- |
| This organization-facing proposal | Executive stakeholder document |
| [Meeting Narrative](meeting-narrative.md) | Communication guide for a 10–15 minute conversation |
| [Stakeholder Questions](stakeholder-questions.md) | Operating-assumption discovery guide |
| [Technical Proposal](technical-proposal.md) | Deeper public technical proposal |
| [Technical Appendix](technical-appendix.md) | Engineering evidence and references |

This public proposal package is maintained in English. Organization-specific
meeting material may later be translated and adapted privately for the relevant
stakeholders.
Any future Spanish adaptation belongs outside the public repository. This generic
package contains no confidential organization data or commercial commitments.
