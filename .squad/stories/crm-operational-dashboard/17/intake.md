# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/crm-operational-dashboard/17/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** CRM Operational Dashboard / Home Page
- **Feature slug (folder under `plans/`):** `crm-operational-dashboard`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `17`
- **Work item type:** `User Story`
- **Status:** `Drafted`
- **Assignee:** ``
- **Labels:** `frontend`, `backend`, `dashboard`, `home`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Story 17 — CRM Operational Dashboard / Home Page
```

---

## Description

### 1. User Story / Business Goal

As a staff user (Admin or Agent), when I land on the authenticated Home page, I want to see a
concise, role-aware, operationally useful summary of the CRM's current state and my own
outstanding work, so that the Home page is a genuine starting point for my day instead of a
blank screen showing only the application name and a backend-health check.

### 2. Business Context

`frontend/src/pages/HomePage.tsx` currently renders exactly two things: the branded app name
(via `useBranding()`) and a `GET /api/health` status line. It carries no ticket, customer, SLA,
task, or notification information at all, even though the CRM now has substantial operational
data behind it: Tickets (with status/category/priority/SLA due dates/escalation), Customers,
Departments/Branches, Tasks, Notifications, Agent assignment and performance, Customer
satisfaction, and a full read-only Reports area for Admins. None of that is surfaced anywhere
before a user navigates into a specific area.

This story turns Home into an operational dashboard grounded in data and endpoints that
already exist, scoped to what the authenticated user can already see and do — explicitly not a
cosmetic reskin, and explicitly not a duplicate of the existing `/reports/*` analytics area.

### 3. Inspection Findings (current implementation, read-only — nothing was changed to produce this intake)

Recorded here because they directly shape the Functional Requirements, the ambiguities in
§8, and the Technical Notes in §11.

**Current Home page**
- `frontend/src/pages/HomePage.tsx` — app name heading + `/api/health` status text. No other
  data, no widgets, no quick actions, no role branching.
- `/` is already a top-level, always-visible nav item (`shell.nav.home` in
  `AppShell.tsx`'s `NAV_ITEMS`, no `adminOnly` gating) and is already a top-level breadcrumb
  entry (`BREADCRUMB_ROUTES['/']`, `parentPath: null`, Story 16) — no routing or nav change is
  needed to ship this story; it is a content change to the existing page only.

**Tickets** (`backend/.../Controllers/TicketsController.cs`)
- `GET /api/tickets` (`[Authorize(Policy = "RequireStaff")]`, no Admin restriction) returns
  **every** ticket in the system, unpaginated, unfiltered except an optional `customerId`
  query param. There is no server-side "assigned to me" filter — `frontend/src/pages/TicketsPage.tsx`
  already has an `all`/`mine` toggle, but it filters the **already-fetched full list**
  client-side (`tickets.filter(t => t.assignedToUserId === currentUserId)`). This is the
  existing precedent for "my tickets" as a user-facing concept, but it is not an efficient
  data source to reuse as-is for a dashboard widget.
- SLA due-date and escalation logic is centralized in two internal static methods,
  `TicketsController.ComputeDueDates` and `TicketsController.ComputeIsEscalated`, and a
  runtime-configurable target resolver `TicketsController.ResolveSlaTargetsAsync` (Story 15).
  `ReportsController` already reuses these exact methods rather than reimplementing SLA math.
  Any dashboard SLA/escalation figure must do the same — reusing this logic, not
  reimplementing it, is a hard requirement, not a suggestion.
- **`ComputeIsEscalated` is binary** (escalated / not escalated — "escalated" means a response
  or resolution due date has already passed with no response/resolution yet). **There is no
  existing "approaching breach" / "at risk" concept anywhere in the codebase** — see
  Ambiguity A in §8.
- Ticket also carries optional `DepartmentId`/`BranchId` (Story 15) — see Ambiguity B in §8.

**Customers** (`CustomersController.cs`)
- `GET /api/customers` (`RequireStaff`) returns **every** customer, unpaginated, ordered by
  `CreatedAtUtc` descending. Same over-fetch concern as Tickets if reused naively for a
  "recent customers" widget.

**Tasks** (`Domain/TicketTask.cs`)
- A `TicketTask` has `TicketId`, `Title`, `DueAtUtc`, `IsDone` — **it has no `AssignedToUserId`
  of its own.** "My overdue tasks" can only be derived by joining through the parent ticket's
  `AssignedToUserId`, not from the task directly. This is an existing data-model fact, not
  something this story should change.
- The Story 15 `TaskReminderScanner` already computes "task due within the configured lead
  time" for its own purpose (creating a `Notification`) — it is a background service, not a
  queryable endpoint, and is not directly reusable as a dashboard data source as-is.

**Notifications** (`NotificationsController.cs`)
- `GET /api/notifications` (`RequireStaff`, no Admin restriction) returns the **caller's own**
  notifications (scoped server-side via the JWT `sub` claim), newest 50, of types `Assigned`,
  `Escalated`, `TaskReminder`, `Mention`. `GET /api/notifications/unread-count` exists too.
  This is already a ready-made, correctly-scoped, efficient data source for a "recent
  activity / needs attention" dashboard widget for the current user.

**Agent performance & Customer satisfaction** (`ReportsController.cs`, all four sub-reports
plus `GET /api/reports/dashboard`)
- The whole `ReportsController` is `[Authorize(Policy = "RequireStaff", Roles = "Admin")]` —
  **Admin-only, system-wide, no per-agent-scoped variant of any of it is callable by a
  non-Admin.** `GET /api/reports/dashboard` already composes ticket counts, SLA performance,
  the top 5 agents by resolved count, and satisfaction into one `ManagementDashboardReport` —
  conceptually very close to what an **Admin's** dashboard view needs, but it cannot serve an
  **Agent's** personal-workload view because of this RBAC boundary, and it has no
  department/branch scoping either.
- `BuildSatisfactionReportAsync` already computes `AverageRatingByAgent` — a per-agent
  satisfaction breakdown exists in this aggregate, but only inside the Admin-only endpoint.

**Departments / Branches** (Story 15)
- Full CRUD exists (Admin-only), plus staff-readable option lists
  (`GET /api/tickets/department-options`, `/branch-options`) used by ticket/user forms.
  **No existing endpoint filters tickets, customers, or any report by department or branch.**
  A `User` and a `Ticket` can each optionally have a `DepartmentId`/`BranchId`, but nothing in
  the current implementation defines what should happen when they're null, or scopes any
  list/report by them. See Ambiguity B in §8.

**AI capabilities** (Story 15)
- `AiController`/`AiChatController` are entirely **per-ticket** (summarize/suggest-reply/
  suggest-category/suggest-kb-articles) or a stateless anonymous chat — there is no
  system-wide or aggregate AI capability today (no "AI daily briefing", no "AI risk summary").
  AI is not a usable KPI/data source for this dashboard as the codebase stands; at most it
  could be a navigational quick-action shortcut to a ticket's existing AI panel, which is not
  a KPI and is explicitly optional.

**Roles**
- Exactly two roles exist system-wide: `Admin` and `Agent` (confirmed via `SeedData.cs`
  and every RBAC check across the app). Every existing role-conditional UI branch
  (`AppShell.tsx`'s `adminOnly` nav filtering, `ReportsController`'s controller-level
  attribute) is phrased as "Admin vs. everyone else," never as an explicit `Agent` check.
  This story should follow the same binary, not invent a third tier.

**i18n / layout conventions**
- `home.*` already exists in `en.json`/`ar.json` (`backendHealth`, `statusUnknown`,
  `statusUnreachable`) and will be extended, not replaced.
- RTL is applied globally via `document.documentElement.dir` (`LanguageProvider.tsx`); every
  existing page uses logical Tailwind classes, never `if (isRtl)` branching — the dashboard
  must follow the same convention.
- Loading/empty/error state conventions already exist per-page throughout the app (e.g.
  `if (loading) return null;`, an inline `<p className="text-sm text-red-600">` error line,
  an `empty` i18n key with a centered placeholder) — the dashboard should follow the same
  look, not invent a new state-handling pattern.

### 4. Functional Scope

**In scope:**
- Replacing `HomePage.tsx`'s content with a role-aware operational dashboard, reusing existing
  data/endpoints wherever they already fit, and (per Technical Notes §11) a new,
  appropriately-scoped read aggregation surface where reuse would mean over-fetching or
  reaching data the caller's role cannot already access.
- A concise KPI summary (ticket counts by status; open/at-risk/escalated counts) using only
  concepts that already exist (Ticket status/category/priority, `ComputeIsEscalated`).
- A personal, actionable section for the current user: their assigned tickets, their
  outstanding notifications, and their tasks whose parent ticket they're assigned to.
- An Admin-oriented system-wide operational summary, distinct from (and much smaller than)
  the existing `/reports/*` pages — a glance, not a report.
- Quick actions that link to functionality that already exists (e.g. the Tickets page, the
  Customers page) — not new deep-link/auto-open-modal behavior unless the plan explicitly
  decides to add that narrow convention.
- i18n (`en.json`/`ar.json`), RTL, and responsive behavior for every new element, following
  existing conventions exactly.

**Out of scope (see also §9):**
- Any change to `/reports/*` or `ReportsController`'s existing behavior.
- Any change to `AppShell.tsx`'s structure/navigation/breadcrumbs beyond what's needed to
  route `/` to the new Home content (none is expected — `/` already exists and is unguarded).
- Inventing a department/branch visibility rule (see Ambiguity B) — if the plan chooses to
  scope dashboard data by department/branch, that must be an explicit, flagged planning
  decision, not a silent assumption baked into this intake.
- A new permission/role model, or an `Agent`-specific policy that doesn't already exist.
- Any new AI capability, chat surface, or "AI-generated" dashboard content.
- A frontend test framework (none exists in this project; none is introduced here).

### 5. Functional Requirements

**FR1 — Replace, don't decorate.** `HomePage.tsx`'s current "app name + backend health"
content is replaced by the dashboard. (The backend-health check may be kept or folded in at
the planner's discretion — it is not the point of this story either way.)

**FR2 — KPI summary from real data.** The dashboard shows ticket volume/status KPIs (e.g.
total, open, in-progress, closed, escalated) computed from actual `Ticket` data via the
existing `ComputeDueDates`/`ComputeIsEscalated` logic — never a reimplementation of that math,
and never placeholder/mock numbers.

**FR3 — Personal, actionable section (all staff).** Every authenticated staff user sees their
own assigned tickets, their own unread/recent notifications (reusing
`GET /api/notifications`), and outstanding tasks belonging to tickets assigned to them. This
section reflects only the current user's own data — it must not require Admin-only endpoints.

**FR4 — Admin operational summary.** An Admin additionally sees a system-wide operational
summary (ticket counts, SLA/escalation state, and — reusing the existing per-agent
satisfaction breakdown data — a compact view of agent workload/performance), distinct in
depth and length from the full `/reports/*` pages, which remain the detailed analytics area.

**FR5 — Role-aware, not role-invented.** The dashboard adapts to exactly the Admin/Agent
distinction that already exists in this codebase (`hasRole('Admin')`); it introduces no new
role or permission concept.

**FR6 — Quick actions to existing functionality only.** Any quick action (e.g. "New ticket",
"New customer", "View my tickets") navigates to a page/feature that already exists. No quick
action may reference functionality this story would need to invent.

**FR7 — Efficient data loading.** The dashboard's data loading is evaluated for efficiency per
Technical Notes §11 — it must not, by default, fetch entire unfiltered Tickets/Customers
collections client-side just to compute a handful of summary numbers, when a narrower,
purpose-built read surface would avoid that.

**FR8 — Standard state handling.** Loading, empty, and error states follow the same visual/
structural conventions already used throughout the app (see §3's "i18n / layout conventions").

**FR9 — i18n, RTL, responsiveness.** Every new string is added to both `en.json` and `ar.json`
under the existing `home.*` namespace (extended, not replaced); layout uses logical Tailwind
classes exclusively (no `isRtl` branching); the dashboard remains usable at mobile, tablet, and
desktop widths, matching `AppShell.tsx`'s existing breakpoint conventions.

### 6. Acceptance Criteria

**Baseline / no more empty page**
- [ ] AC1: The authenticated Home page (`/`) no longer shows only the app name and a backend
      health line — it displays real, current ticket/notification/task data for the logged-in
      user (Admin or Agent).
- [ ] AC2: Every number/figure shown on the dashboard is traceable to real data returned by an
      existing or newly-added backend endpoint — no hardcoded or placeholder values.

**KPIs & accuracy**
- [ ] AC3: Ticket status counts (e.g. Open/In Progress/Closed) shown on the dashboard match
      what `GET /api/tickets` (or the new aggregation, per §11) actually returns for the same
      data at the same moment.
- [ ] AC4: Any "escalated"/SLA-breached figure on the dashboard is computed via
      `TicketsController.ComputeIsEscalated`/`ComputeDueDates` (directly or through a new
      endpoint that itself calls them) — not a separate, parallel implementation of SLA logic.

**Personal / actionable section**
- [ ] AC5: A staff user sees a list (or count) of tickets currently assigned to them, matching
      what they'd see using the existing `/tickets` "My tickets" filter for the same account.
- [ ] AC6: A staff user sees their own recent/unread notifications, sourced from
      `GET /api/notifications`, not a re-implementation of notification retrieval.
- [ ] AC7: A staff user sees outstanding (not-done) tasks belonging to tickets assigned to
      them, correctly derived through the ticket's assignment (since `TicketTask` itself has
      no assignee field) — never showing another agent's tasks as "theirs."

**Admin view**
- [ ] AC8: An Admin sees a system-wide operational summary in addition to (not instead of)
      their own personal section from AC5–AC7.
- [ ] AC9: A non-Admin (Agent) never sees the Admin-only system-wide summary section, and
      no dashboard request made by a non-Admin session triggers a 403 from an Admin-only
      endpoint (i.e., the frontend does not call Admin-gated endpoints for a non-Admin user).
- [ ] AC10: The dashboard's Admin summary is visibly smaller/more concise than the existing
      `/reports/dashboard` page — it links to Reports for detail rather than duplicating it.

**Quick actions**
- [ ] AC11: Every quick action on the dashboard navigates to a page or feature that already
      exists (e.g. `/tickets`, `/customers`) and does not 404 or reference unbuilt
      functionality.

**States**
- [ ] AC12: The dashboard shows a loading state while its data is being fetched, and a
      graceful empty state (not a crash or blank screen) for a brand-new account with zero
      tickets/notifications/tasks.
- [ ] AC13: If the dashboard's data request(s) fail, an inline error state is shown, following
      the same visual convention as other pages in this app — the page does not crash.

**i18n / RTL / responsive**
- [ ] AC14: Every new dashboard string exists in both `en.json` and `ar.json` under `home.*`
      (or a clearly-related sub-namespace); the existing key-parity script passes with zero
      missing keys either direction.
- [ ] AC15: With the app language set to Arabic, the dashboard reads correctly in RTL with no
      component-level LTR/RTL conditional branching.
- [ ] AC16: The dashboard remains usable (no horizontal overflow, no clipped/unreachable
      content) at mobile, tablet, and desktop widths.

**Regression**
- [ ] AC17: `/reports/*` pages and `ReportsController`'s existing endpoints are unchanged and
      unaffected.
- [ ] AC18: No existing Story's routing, authentication, authorization, or navigation behavior
      regresses — confirmed via a scoped diff review before this story is considered done.

### 7. Dependencies

- Depends on the current state of `frontend/src/pages/HomePage.tsx`,
  `backend/.../Controllers/TicketsController.cs` (specifically `ComputeDueDates`,
  `ComputeIsEscalated`, `ResolveSlaTargetsAsync`, all `internal static` and already reused
  cross-controller by `ReportsController`), `NotificationsController.cs`,
  `CustomersController.cs`, `Domain/TicketTask.cs`, `Domain/Ticket.cs`, `ReportsController.cs`,
  and `useAuthStore.ts` (`user.id`, `hasRole('Admin')`).
- Depends on Story 16's `AppShell.tsx`/breadcrumb/i18n conventions being in place (they
  already are, in this working tree) — no changes to those files are anticipated, only reuse
  of their existing conventions.
- Depends on Stories 9 (Tasks), 10 (SLA & Notifications), 12/13 (Reports/Satisfaction), and 15
  (Departments/Branches, RuntimeSettings) for the underlying data this dashboard reads — this
  story reads that data, it does not modify any of those stories' behavior.
- No new third-party dependency is anticipated for either backend or frontend.

### 8. Ambiguities Discovered During Inspection (explicitly not resolved here)

**Ambiguity A — "At risk" / "approaching SLA breach" has no existing definition.**
The current codebase only computes a binary escalated/not-escalated state
(`ComputeIsEscalated`). There is no existing threshold, field, or endpoint for "approaching
breach" as distinct from "already breached." If the dashboard is to show an "at risk" tier
(as opposed to only "escalated" vs. "not escalated"), the planner must define that threshold
explicitly (e.g., using the existing runtime-configurable reminder lead time, or a new
threshold) — this intake deliberately does not invent one.

**Ambiguity B — Department/Branch visibility scoping is undefined.**
`User` and `Ticket` can each optionally carry a `DepartmentId`/`BranchId` (Story 15), but no
existing endpoint filters any list or report by them, and there is no existing rule for
"an Agent in Department X should only see Department X's tickets" (or any similar scoping).
This intake explicitly does **not** assume department/branch-scoped dashboard data. If the
business wants that, it must be decided and specified explicitly during planning — absent
that decision, the default/safe assumption is: Agents see their own assigned work
(unfiltered by department/branch), and Admins see the whole system (also unfiltered by
department/branch), matching how every other existing list endpoint already behaves today.

**Ambiguity C — Quick actions that would need to auto-open a modal.**
Several existing "create" flows (new ticket, new customer) are modals on their respective
list pages (`/tickets`, `/customers`), not separate routes. There is no existing deep-link
convention (e.g. a `?new=true` query param) to land on those pages with the create form
already open. A dashboard "quick action" can safely link to the page itself; auto-opening
the create form is a nice-to-have that would require a new, narrow convention this story
does not require the planner to add.

### 9. Out of Scope

- Any modification to `/reports/*` pages or `ReportsController`.
- Any AppShell/sidebar/breadcrumb redesign (Story 16's shell is reused as-is).
- A new role/permission model or Department/Branch-based access control.
- New AI-generated dashboard content or a dashboard-level AI feature.
- Pulling in unrelated missing functionality from other stories to "fill out" the dashboard.
- A frontend test framework.
- Customer-portal (`PortalShell.tsx`/`/portal/*`) changes — this story is for the staff-facing
  authenticated Home page only.

### 10. Definition of Done

- [ ] `HomePage.tsx` shows a real, role-aware operational dashboard per FR1–FR9; the app-name/
      health-only content is gone.
- [ ] Every dashboard figure is sourced from real data via an existing or newly-added,
      appropriately-scoped endpoint — none of it is hardcoded.
- [ ] SLA/escalation figures reuse `ComputeDueDates`/`ComputeIsEscalated` — no parallel SLA
      math exists anywhere in the new code.
- [ ] A non-Admin session never triggers an Admin-only endpoint from the dashboard.
- [ ] Loading, empty, and error states are handled per existing conventions.
- [ ] Full i18n parity (`en.json`/`ar.json`) and an RTL sweep both pass.
- [ ] Desktop/tablet/mobile sweeps all pass with no overflow/clipping.
- [ ] `/reports/*` and every other existing Story's behavior is confirmed unchanged via a
      scoped diff review.
- [ ] Ambiguities A–C from §8 are either explicitly resolved in the implementation plan (with
      the resolution stated) or explicitly deferred with a stated reason — never silently
      assumed.

### 11. Technical Notes (for the planner — not a mandate)

- **Reuse vs. new aggregation endpoint:** `GET /api/tickets` and `GET /api/customers` are
  both unfiltered/unpaginated; reusing them client-side for dashboard widgets means
  over-fetching the entire table just to show a handful of summary numbers, which will not
  scale. `GET /api/reports/dashboard` already demonstrates the right shape for an aggregate
  read (compose several internal `Build*Async` helpers into one response) but is Admin-only
  and system-wide. The planner should evaluate adding a new, narrower, role-appropriate
  aggregation endpoint (e.g. staff-readable "my dashboard" data reusing
  `TicketsController`'s existing internal SLA helpers, separate from the Admin-only
  `ReportsController`) rather than composing many unfiltered list calls in the frontend.
- **Reuse existing internal helpers, don't duplicate them:** `TicketsController.ComputeDueDates`,
  `ComputeIsEscalated`, and `ResolveSlaTargetsAsync` are already `internal static`/`internal
  static async` and already called cross-controller by `ReportsController` — any new endpoint
  should do the same rather than reimplementing SLA/escalation math a third time.
  `AssemblyInfo.cs` already has `InternalsVisibleTo` for the test project if new automated
  tests are added for a new endpoint, following this project's existing direct-call test
  style (no test framework change).
  - `TicketTask` has no `AssignedToUserId` — "my tasks" must join through
  `Ticket.AssignedToUserId`.
- No frontend test runner exists in this project (confirmed across every prior story); this
  story's verification, like every prior one, is manual plus whatever backend automated tests
  the plan adds in the existing direct-call xUnit style.

---

## Acceptance criteria

*(See the numbered AC1–AC18 checklist embedded in the Description above — this project's
established intake convention, matching Stories 4–16.)*

```
See "6. Acceptance Criteria" above.
```

---

## Attachments

None.

---

## Dependencies

- **Blocked by / related ids:** None. Builds on the current state of the codebase after
  Story 16 (id 16) in this same working tree; not blocked by any open story.
- **Depends on code areas or other stories:** See "7. Dependencies" above for the full list.

## Extra notes (optional)

- This story was explicitly requested to be standalone and must not modify Stories 1–16 or
  Story 24's existing behavior.
- Inspection was performed read-only against the current codebase; no code was changed to
  produce this intake.
- Three genuine ambiguities were discovered during inspection (§8) and are deliberately left
  unresolved here, per instruction, rather than silently assumed.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary languages:
  `typescript` (frontend), `csharp` (backend).
- See "11. Technical Notes" above for the reuse-vs-new-endpoint consideration the planner
  should resolve explicitly.

## Out of scope

- See "9. Out of Scope" above.
