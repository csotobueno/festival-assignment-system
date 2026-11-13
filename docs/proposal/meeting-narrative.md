# Festival Assignment — Meeting Narrative

**Communication guide: Drafted. Organization Review: Pending.**

Use with the [Organization-Facing Proposal](organization-facing-proposal.md).
Aim for approximately **12 minutes**, within a **10–15 minute** conversation;
adjust emphasis to the audience. These are prompts, not a full speech or meeting
record. Use the [Stakeholder Questions](stakeholder-questions.md) selectively and
leave detailed discovery for a follow-up discussion.

## 1. Why This Problem Matters — 1 minute

**Objective:** Establish the operational question before introducing technology.

**Key message:** How can daily festival capacity be assigned in a more structured
and fair way, keeping groups together where feasible and balancing Attendee
experience across days?

**Suggested talking points:**

- Capacity is limited and different locations offer different experiences.
- Groups and multi-day attendance add allocation choices beyond a single person
  obtaining a location on one day.
- Treat uncertainty and physical queues as opportunities to explore; ask the
  organization to describe its actual situation without assuming a problem.

## 2. What the Proposed Model Changes — 1 minute

**Objective:** Explain the central policy idea in business language.

**Key message:** Daily allocation could consider both feasible group placement
and previous experience.

**Suggested talking points:**

- A registered Attendee requests alone or as part of a temporary daily group.
- Physical feasibility and eligibility come first; the whole group fits together
  or receives no Assignment from that request.
- Less favorable prior experience can increase later priority. The aim is
  balance over days, not a promise of a particular location.

## 3. How the Assignment Experience Works — 1 minute

**Objective:** Make the concept concrete without implying a finished service.

**Key message:** Request → validate → find feasible contiguous locations →
consider history → confirm a complete Assignment or explain non-success.

**Suggested talking points:**

- Successful groups occupy consecutive Spots in one row and one Zone.
- Recorded Assignments inform later days and can be traced to their requests.
- Business rejection and technical failure are different outcomes. User access,
  result presentation and notifications still require definition.

## 4. What Has Already Been Technically Validated — 2 minutes

**Objective:** Explain why the concept merits evaluation by the organization.

**Key message:** The internal Gate concluded **Technically Feasible for Proposal**:
sufficient technical evidence exists to justify organization-level evaluation.

**Suggested talking points:**

- Simulations examined the model under multiple representative and adverse conditions.
- Fairness behavior was evaluated against an internal MVP criterion; this is not
  an organization-approved definition of fairness.
- Integrity protections were tested: valid audited cases preserved stored rules.
  One stopped stress collection retains audit limitations.
- Local reference performance was strong; stress limits and risks were identified.
  Complete production response times have not been established.

Use the proposal's four-area evidence table. Keep scenario identifiers, metrics
and engineering details in the [Technical Appendix](technical-appendix.md) and
[Gate result](../stage-5/mvp-technical-feasibility-gate-result.md) for follow-up.

## 5. What We Deliberately Have Not Assumed — 1 minute

**Objective:** Separate modeled inputs from actual festival operations.

**Key message:** The organization must validate the operating assumptions.

**Suggested talking points:**

- Actual attendance, usable capacity, venue geometry and reserved areas are unknown.
- Request timing, group behavior, fairness expectations and service needs are open.
- The concept does not establish a final interface, authentication model,
  deployment architecture or operating workflow.

## 6. Known Risks — 2 minutes

**Objective:** Make the limitations part of the decision.

**Key message:** Technical feasibility for proposal leaves material risks unresolved.

**Suggested talking points:**

- Request order can change individual outcomes despite similar aggregate fairness.
- Concurrent requests compete for the same inventory. Aggressive concurrent and
  high-rate stress caused severe degradation; real festival behavior is unvalidated.
- Free Spots may be scattered so no contiguous block fits a group.
- Real group sizes and split/resubmission rules are unknown; late-group
  reconfiguration has not been validated. Completed Assignments remain final.
- Technical mitigation paths exist, but none is selected or validated as the solution.

## 7. What We Need From the Organization — 3 minutes

**Objective:** Identify the most useful discovery inputs and policy questions.

**Key message:** Real operating information determines which further validation
would be useful.

**Suggested talking points:**

- Start with aggregate daily demand/capacity and a non-personal layout showing
  assignable, reserved and blocked areas.
- Discuss simultaneous versus staged request opening and known peaks; these
  inputs determine the relevance of further concurrency validation.
- Clarify typical and largest groups, togetherness requirements, splitting,
  resubmission and staff involvement; these inform late-group validation.
- Explore what fairness means, acceptable response times and handling of exceptions.
  Select a few prompts from the discovery guide rather than completing it live.
- Keep organization-specific answers and meeting records private.

## 8. Suggested Next Step — 1 minute

**Objective:** Close with a bounded request for discovery.

**Key message:** Validate the operating assumptions with the organization and
determine whether the concept merits a data-informed pilot/design phase.

**Suggested talking points:**

- If there is interest, use confirmed inputs to scope relevant validation and
  evaluate applicable mitigations before formal readiness review.
- A pilot would need separate scope, evidence and authorization after that review.
- **Technically Feasible for Proposal ≠ Production Ready ≠ Organization Approved
  ≠ Ready to Go Live.** Organization Review remains **Pending**.

This public proposal package is maintained in English. Future Spanish meeting
material may be translated and adapted privately from this package for the actual meeting
context; it belongs outside the public repository. This guide records no meeting
outcomes, organization commitments or delivery schedule.
