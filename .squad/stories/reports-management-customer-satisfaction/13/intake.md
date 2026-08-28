# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/reports-management-customer-satisfaction/13/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Reports, Management & Customer Satisfaction
- **Feature slug (folder under `plans/`):** `reports-management-customer-satisfaction`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `13` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Reports, Management & Customer Satisfaction
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
This story recreates and formalizes the scope already approved for "Story 13" in the
project's consolidated remaining-scope recommendation (produced after Stories 01-08 were
merged, and followed verbatim for Stories 09-12, all now merged). That recommendation is
restated here as the source of truth, since no per-story intake file was created for it
at the time.

CRITICAL, VERIFIED FACT (re-confirmed by direct code inspection before writing this
intake): no reporting, dashboard, aggregate, or summary endpoint exists anywhere in the
backend today. This is a brand-new, read-only reporting layer over data that Stories
07/09/10/11 already capture - it introduces no new data-capture and (per the approved
recommendation's own framing) is "a single reporting subsystem, not new data-capture
work."

Approved scope (verbatim from the consolidated recommendation's Story 13 section):
- Ticket reports
- SLA performance reports
- Agent performance
- Customer satisfaction reporting
- Management dashboards

Approved dependency note (verbatim): "reporting needs real data from Stories 09
(categories/priorities to report by), 10 (SLA data), and 11 (feedback) to be
meaningful; it should come after all three." All three are merged on `main`.

Data this story reports on (verified present in the current schema, re-confirmed by
direct code inspection immediately before writing this intake - no invention):
- `Ticket` (Domain/Ticket.cs): Status ("Open"/"InProgress"/"Closed"), Category
  ("General"/"Billing"/"Technical"/"Account"), Priority ("Low"/"Normal"/"High"/"Urgent"),
  Source ("Manual"/"Portal"/"WebForm"/"Email"/"WhatsApp"/"SMS"/"Chat"), CreatedAtUtc,
  FirstRespondedAtUtc, ResolvedAtUtc, AssignedToUserId.
- SLA due-dates/escalation are NEVER persisted columns - they are computed on the fly by
  `TicketsController.ComputeDueDates(createdUtc, priority)` (an `internal static` priority
  -> (responseDue, resolutionDue) lookup against a fixed `SlaTargets` dictionary) and
  `ComputeIsEscalated(...)` (private to TicketsController). Reports must reuse this exact
  existing logic (made accessible to a new Reports area the same way `AllowedPriorities`
  already is - `internal static`), not reimplement or duplicate the SLA math.
- `TicketFeedback` (Domain/TicketFeedback.cs): Rating (int, 1-5, enforced in
  PortalController not on the entity), Comment, TicketId, CustomerId, CreatedAtUtc. One
  row per ticket (unique index on TicketId, Story 11).
- `User`/`Role`/`UserRole`: only two roles exist anywhere in this codebase today -
  "Admin" and "Agent". There is no "Manager" role, and Role Management UI (which could
  introduce one) is explicitly Story 14 scope, not this one.

This is a report-and-present feature: new read-only GET endpoints that aggregate
existing tables via LINQ, and new staff-facing pages that render the results. No new
persisted entities and no new EF Core migration are expected to be required.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
1. A new Reports area exists, reachable only by staff with the "Admin" role (mirrors the
   existing `AdminController`/`WebhookSubscriptionsController` RBAC precedent exactly -
   [Authorize(Policy = "RequireStaff", Roles = "Admin")] at the controller level; nav item
   filtered via the existing `adminOnly` mechanism in AppShell.tsx, same pattern as the
   Story 12 "Webhooks" nav item). Rationale: "Agent performance" and "Management
   dashboards" are cross-agent, comparative, sensitive views - the same class of concern
   already reserved for Admin-only in this codebase - and there is no "Manager" role to
   assign a middle tier to.
2. Ticket reports: counts of tickets grouped by Status, by Category, by Priority, and by
   Source, over an optional date range (open-ended query params - default to all-time if
   omitted).
3. SLA performance reports: response-SLA-met vs. breached count/percentage,
   resolution-SLA-met vs. breached count/percentage, average response time, average
   resolution time, and escalated-ticket count - all computed via the existing
   `ComputeDueDates`/`ComputeIsEscalated` logic (made `internal` if not already), not
   reimplemented.
4. Agent performance: per-agent (per User with role Agent or Admin who has tickets
   assigned) ticket counts by status, tickets resolved, and average resolution time.
5. Customer satisfaction reporting: average `TicketFeedback.Rating` (overall and, if
   straightforward, per-agent/per-category), rating distribution (count per 1-5 value),
   and total feedback count vs. total closed-ticket count (response rate).
6. Management dashboard: one staff page presenting a summary of the above (ticket
   volume/status breakdown, SLA compliance %, agent leaderboard, average satisfaction) -
   numeric/tabular summary cards, consistent with this codebase's existing plain-Tailwind,
   no-UI-kit convention. No new charting/visualization library is introduced (none exists
   in `frontend/package.json` today, and none is required to satisfy this scope).
7. All new endpoints are read-only (GET) and introduce no new persisted entities and no
   new EF Core migration - confirmed achievable by aggregating the existing Ticket/
   TicketFeedback/User tables directly.
8. i18n: all new UI strings added to both `en.json` and `ar.json` (existing convention);
   RTL-safe layout (logical Tailwind classes only, per every prior story).
9. Existing Stories 01-12 functionality, routes, and RBAC are unmodified except for the
   minimum touch-point needed to expose the internal SLA helpers to the new Reports
   controller (e.g. widening `private` to `internal` on `TicketsController`'s
   `ComputeIsEscalated`/`AllowedStatuses`/`AllowedCategories`, matching the existing
   `internal` precedent already set by `AllowedPriorities`/`ComputeDueDates` in the same
   file for the exact same cross-controller-reuse reason).
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

None.

---

## Dependencies

- **Blocked by / related ids:** Depends on Stories 09, 10, 11 (all merged on `main`) for
  the Category/Priority fields, SLA/escalation computation, and Customer feedback data
  this story reports on. Also touches Story 12's established `[FromServices]`-per-action
  and RBAC conventions, and the existing `internal static` allow-list/helper-sharing
  precedent (`TicketsController.AllowedPriorities`, `ComputeDueDates`) used since Story 12.
- **Depends on code areas or other stories:**
  - `backend/src/CustomerSupportCrm.Api/Domain/Ticket.cs`, `TicketFeedback.cs`, `User.cs`,
    `Role.cs`, `UserRole.cs` (read-only; no changes expected)
  - `backend/src/CustomerSupportCrm.Api/Controllers/TicketsController.cs` (widen
    `ComputeIsEscalated`/`AllowedStatuses`/`AllowedCategories` from `private` to
    `internal`, mirroring the existing `AllowedPriorities`/`ComputeDueDates` precedent -
    no behavior change)
  - `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (read-only queries; no
    schema change expected)
  - `frontend/src/routes/AppRouter.tsx`, `frontend/src/components/layout/AppShell.tsx`
    (new `/reports` route(s) and nav item, `adminOnly: true`, same shape as Story 12's
    `/webhooks`)
  - `frontend/src/i18n/locales/en.json`, `ar.json` (new `reports.*` namespace)

## Extra notes (optional)

- No "Manager" role exists in this codebase (confirmed by direct code inspection); Role
  Management UI that could introduce one is Story 14 scope. Reports are therefore
  Admin-only, not "Admin + Manager."
- No charting/visualization library exists in `frontend/package.json` today; this story
  does not introduce one (see acceptance criterion 6).

## Technical hints (optional)

- Repo root: `.` (backend: `backend/src/CustomerSupportCrm.Api`; frontend:
  `frontend/src`). Backend language: C# / .NET 8 / EF Core 8. Frontend language:
  TypeScript / React.
- RBAC: use `[Authorize(Policy = "RequireStaff", Roles = "Admin")]` at the controller
  level, exactly as `AdminController.cs` and `WebhookSubscriptionsController.cs` already
  do - do not invent a new policy.
- DI: every existing controller in this codebase has no declared constructor; every
  dependency (`AppDbContext`, etc.) is injected as an action-method parameter via
  `[FromServices]`. A new `ReportsController` must follow the same convention.
- SLA math: reuse `TicketsController.ComputeDueDates`/`ComputeIsEscalated` exactly
  (widen access instead of duplicating the `SlaTargets` dictionary or the escalation
  condition anywhere else).
- Nav/RBAC-gating pattern to mirror: `frontend/src/components/layout/AppShell.tsx`'s
  `NAV_ITEMS` array with `adminOnly?: boolean`, filtered against
  `useAuthStore((s) => s.hasRole('Admin'))` - identical to the Story 12 "Webhooks" entry.

## Out of scope

- Introducing a "Manager" role or any new RBAC policy/tier.
- Any new charting/visualization library or dependency.
- Any new persisted entity or EF Core migration - this is aggregation over existing data.
- Exporting reports (CSV/PDF/etc.) - not mentioned in the approved scope.
- Scheduled/emailed reports - not mentioned in the approved scope.
- Per-department or per-branch breakdowns - no department/branch concept exists yet
  (Story 14 scope, "Multi-department"/"Multi-branch").
- Modifying any Story 01-12 behavior beyond the minimum `private` -> `internal` access
  widening described above.
