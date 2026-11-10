using System.Collections.Immutable;
using Festival.Domain.Assignments;

namespace Festival.Simulation;

// Read-only final-state correlation. Never retries execution or changes an
// attempt's recorded outcome, timing, diagnostics or confirmed persisted identity.
public static class PostCommitObservationResolver
{
    public static ImmutableDictionary<Guid, AssignmentRequestId> Resolve(ScenarioManifest manifest, SimulationResult result)
    {
        var plans = manifest.DailyPlans.SelectMany(d => d.Requests).ToDictionary(p => p.SimulationRequestId);
        var requests = result.Requests.ToDictionary(r => r.AssignmentRequestId);
        var resolved = ImmutableDictionary.CreateBuilder<Guid, AssignmentRequestId>();
        foreach (var attempt in result.Attempts.Where(a => a.Outcome == SimulationAttemptOutcome.UnexpectedTechnicalFailure
            && a.DiagnosticCode == "RECONCILIATION_FAILED" && a.FeasibleContiguousBlockFound == true))
        {
            // This explicit diagnostic is emitted only after the real use case
            // returned. Require the independently loaded complete durable graph.
            var members = attempt.AttendeeIds.ToHashSet();
            var rows = result.Assignments.Where(a => a.FestivalDayId == attempt.FestivalDayId && members.Contains(a.AttendeeId)).ToArray();
            if (rows.Length != attempt.GroupSize || rows.Length == 0 || rows.Select(a => a.AssignmentRequestId).Distinct().Count() != 1)
                continue;
            var id = rows[0].AssignmentRequestId;
            var allRows = result.Assignments.Where(a => a.AssignmentRequestId == id).ToArray();
            var plan = plans[attempt.SimulationRequestId];
            if (!requests.TryGetValue(id.Value, out var request) || request.Status != "Completed"
                || request.FestivalDayId != attempt.FestivalDayId.Value
                || (request.RequestedAt - plan.RequestedAt).Duration() >= TimeSpan.FromMicroseconds(1)
                || allRows.Any(a => (a.AssignedAt - plan.AssignedAt).Duration() >= TimeSpan.FromMicroseconds(1)))
                continue;
            try { AssignmentGroup.Create(id, attempt.FestivalDayId, plan.Members.Select(m => m.Id)).EnsureValidResult(allRows); }
            catch (ArgumentException) { continue; }
            resolved.Add(attempt.SimulationRequestId, id);
        }
        return resolved.ToImmutable();
    }
}
