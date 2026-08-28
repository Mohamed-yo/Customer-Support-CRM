# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/sla-automation-notifications/10/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** SLA, Automation & Notifications
- **Feature slug (folder under `plans/`):** `sla-automation-notifications`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `10`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,domain`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SLA, Automation & Notifications
```

---

## Description

```
Consolidates the "SLA & Automation" and remaining "Ticket Management" (Escalation) capabilities from the approved 6-story consolidation (Story 10 of 09-14) into: response/resolution SLA targets, automatic ticket assignment, ticket-level escalation, SLA alerts, and in-app notifications. This story was pre-scoped across two prior rounds this user has already reviewed and approved (a full CRM audit, then a 6-story consolidation, then a 10-point pre-implementation analysis) — the five architecture decisions below are FINAL, confirmed by the user, not open questions for the plan to re-derive.

**Decision 1 — No new scheduling/real-time infrastructure.** This codebase has zero background-job or real-time infrastructure today (confirmed: no `BackgroundService`/`IHostedService`/Hangfire/Quartz/SignalR anywhere in `backend/src`). Escalation is detected **lazily, on access** — recomputed every time a ticket is read (`List`/`Get`) or updated, never via a timer/scheduled job. Do **not** introduce `BackgroundService`, Hangfire, Quartz, SignalR, or any polling-server-side mechanism. The one exception: the frontend notification bell polls the API on an interval (client-side `setInterval`/`useEffect`, not a new library) — this is the story's one explicitly-approved exception to "no new infra," since it's just a repeated `fetch`, not new server infrastructure.

**Decision 2 — First-response trigger.** `Ticket.FirstRespondedAtUtc` (new nullable field) is set the moment the **first `TicketNote`** (Story 09) is posted on the ticket — not on any field edit. A pure `PUT /api/tickets/{id}` field correction does not count as a response. If a ticket already has `FirstRespondedAtUtc` set, posting more notes does not change it.

**Decision 3 — Escalation is a derived flag, not a Status value.** `Ticket.Status` remains exactly `Open`/`InProgress`/`Closed` (Story 07, unchanged). Escalation (`IsEscalated`) is a read-time computed boolean, never persisted, never a `Status` value: `true` when (`Status != "Closed"`) AND (`now > ResponseDueAtUtc` and `FirstRespondedAtUtc` is null) OR (`now > ResolutionDueAtUtc` and `ResolvedAtUtc` is null).

**Decision 4 — Least-loaded automatic assignment.** On `POST /api/tickets` (create), if `AssignedToUserId` is omitted/null in the request, the server automatically assigns the ticket to whichever user in the existing "assignable users" pool (Story 08's `Agent`/`Admin` role members) currently has the **fewest tickets where `Status != "Closed"`**. No new "rotation pointer" entity/state — this is a stateless query computed at assignment time. If the assignable-users pool is empty, the ticket is created unassigned exactly as today (no error) — auto-assignment is a best-effort convenience, not a hard requirement.

**Decision 5 — Notifications are poll-based.** A new `Notification` entity (per-user, ticket-scoped) is created for: (a) a ticket being assigned to a user (manual or automatic — both paths converge on the same notification-creation call), and (b) a ticket newly becoming escalated (detected lazily per Decision 1; the system must avoid creating a duplicate escalation notification for a ticket that is already known to be escalated — check for an existing unread/recent notification of that type+ticket before creating another). The frontend polls `GET /api/notifications/unread-count` on an interval to update a bell badge in the App Shell header, and fetches the full list on demand when the bell is opened.

**Response/resolution SLA targets** (fixed server-side constants this story — an admin-editable configuration UI is explicitly Story 14 scope, not this one):
- Urgent: 1 hour response / 4 hours resolution
- High: 2 hours response / 8 hours resolution
- Normal: 4 hours response / 24 hours resolution
- Low: 8 hours response / 48 hours resolution

These are measured from `Ticket.CreatedAtUtc`. `ResponseDueAtUtc`/`ResolutionDueAtUtc` are **computed**, not persisted columns (derivable from `CreatedAtUtc` + `Priority` via the fixed lookup above) — only the two "did it actually happen" timestamps (`FirstRespondedAtUtc`, `ResolvedAtUtc`) are persisted, since those are facts, not derivable math.

`ResolvedAtUtc` is set the moment `Ticket.Status` transitions **into** `"Closed"` via `PUT /api/tickets/{id}`, and is **cleared back to null** if a closed ticket is reopened (`Status` moves away from `"Closed"`) — so a reopened ticket is correctly treated as unresolved again for SLA purposes.
```

---

## Acceptance criteria

```
1. `Ticket` gains nullable `FirstRespondedAtUtc` and `ResolvedAtUtc` columns via a new additive migration (does not alter `Users`, `Roles`, `UserRoles`, `AuditLogs`, `Customers`, or any existing `Tickets`/`TicketNotes`/`TicketAttachments`/`TicketTasks`/`QuickReplyTemplates` column).
2. A new `Notification` entity/table (per-user, optionally ticket-scoped) is added in the same migration.
3. `POST /api/tickets/{id}/notes` (existing Story 09 endpoint) sets `FirstRespondedAtUtc = now` on the parent ticket the first time a note is posted, and only the first time (idempotent thereafter).
4. `PUT /api/tickets/{id}` sets `ResolvedAtUtc = now` when `Status` transitions into `"Closed"`, and clears it to `null` when `Status` transitions away from `"Closed"`.
5. `GET /api/tickets` and `GET /api/tickets/{id}` responses include computed `responseDueAtUtc`, `resolutionDueAtUtc` (derived from `createdAtUtc` + `priority`'s fixed target), `firstRespondedAtUtc`, `resolvedAtUtc`, and `isEscalated` (computed per Decision 3's rule) — no new persisted "escalated" column.
6. `POST /api/tickets` auto-assigns to the least-loaded Agent/Admin (fewest open tickets) when `assignedToUserId` is omitted; explicit assignment (including explicit `null`) is unaffected and unchanged from Story 08. Auto-assignment failing to find any candidate leaves the ticket unassigned (no error).
7. Assigning a ticket (manual, Story 08's existing path, or new automatic path) creates exactly one `Notification` for the newly-assigned user. Reassigning to the same user again does not create a duplicate notification. Unassigning creates no notification.
8. A ticket becoming newly escalated (observed during any `List`/`Get`/`Update` call) creates exactly one `Notification` for its current assignee (if any) — never more than one outstanding/unread escalation notification per ticket at a time.
9. `GET /api/notifications` (current user's own only), `GET /api/notifications/unread-count`, and `POST /api/notifications/{id}/read` exist, `[Authorize]`-only (both Admin and Agent), scoped to the caller's own notifications only (never another user's).
10. The App Shell header gains a notification bell (placed next to the existing `LanguageToggle`) showing the unread count, a dropdown of recent notifications linking to their ticket, and a mark-as-read action. The bell polls `unread-count` on an interval; no WebSocket/SignalR/push mechanism is introduced.
11. `TicketsPage.tsx`'s list and `TicketDetailPage.tsx` display escalation state (e.g. a badge) and the SLA due dates, following the existing badge/panel conventions from Stories 08–09.
12. All new user-facing text has EN and AR translations; all new/changed UI follows the existing App Shell/RTL/responsive conventions.
13. Existing Story 01–09 behavior (health, auth, RBAC, audit logging, Customer/Ticket/Note/Attachment/Task/QuickReply CRUD, App Shell nav/header/logout/language toggle, i18n, RTL/LTR) is completely unaffected.
14. No new NuGet or npm packages, no new UI/state/HTTP/real-time libraries.
15. The backend builds cleanly; the frontend builds and lints cleanly.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Stories 01-09 (all completed and merged into `main`). Directly extends Story 07's `Ticket`/`TicketsController`, Story 08's assignment/`assignable-users` endpoint, Story 09's `TicketNote`/notes endpoint and `TicketDetailPage.tsx`/`TicketsPage.tsx`, and reuses Story 05's audit-logging conventions and Story 03/04's auth/RBAC model.
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Ticket.cs` (add `FirstRespondedAtUtc`, `ResolvedAtUtc`), `Data/AppDbContext.cs` (new fields + new `Notification` entity config), `Controllers/TicketsController.cs` (auto-assignment on create, `FirstRespondedAtUtc`/`ResolvedAtUtc` transitions, computed SLA/escalation fields on list/get, lazy escalation-notification creation), `Controllers/TicketDtos.cs` (extend `TicketListItem`), new `Controllers/NotificationsController.cs` + `NotificationDtos.cs`, new `Domain/Notification.cs`; `frontend/src/api/tickets.ts` (extend `Ticket` type), new `frontend/src/api/notifications.ts`, `frontend/src/components/layout/AppShell.tsx` (mount the notification bell next to `LanguageToggle`), `frontend/src/pages/TicketsPage.tsx` and `TicketDetailPage.tsx` (escalation/SLA display), `frontend/src/i18n/locales/en.json`/`ar.json`.

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01-09 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- The five architecture decisions in the Description are final and user-approved — do not propose alternatives (e.g. a real background scheduler, SignalR, a persisted rotation-pointer for assignment, treating escalation as a Status value). Implement exactly as specified.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this story adds derived/computed fields and one new lightweight entity, not a general-purpose rules/workflow engine.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand, `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- SLA target lookup (`Priority` → response/resolution `TimeSpan`) should be a simple static dictionary/constant in `TicketsController.cs` (or a small private static helper), mirroring the existing `AllowedStatuses`/`AllowedCategories`/`AllowedPriorities` constant-array style already in that file — not a new configuration abstraction.
- "Least-loaded" assignment: reuse the same `db.Users.Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))` query already used for `AssignableUsers` (Story 08), then pick by count of `db.Tickets.Count(t => t.AssignedToUserId == candidate.Id && t.Status != "Closed")` — a single additional query, no new persisted state.
- Notification de-duplication for escalation: before creating a new escalation `Notification`, check whether an existing `Notification` of the same type for the same `TicketId` already exists and is unread — if so, don't create another. This is the simplest correct de-dup rule and needs no new "last escalated at" column on `Ticket`.
- The notification bell's polling interval is a frontend implementation detail (e.g. 30–60s) — pick one and justify it in the plan; do not add configurability for it.

## Out of scope

- SLA target configurability (admin-editable thresholds) — Story 14 (Platform Administration).
- Any real-time/push delivery (SignalR, WebSockets, browser push, email/SMS alerts) — polling only, per Decision 5.
- A background job scheduler of any kind (Hangfire, Quartz, `BackgroundService`) — per Decision 1.
- Manual/explicit "escalate this ticket" agent action — escalation is purely automatic/derived this story.
- Escalation rules beyond the fixed response/resolution-target check (e.g. category-based rules, custom conditions).
- Multiple assignees, assignment history, or any change to Story 08's existing manual-assignment UI/endpoint beyond what's needed to also trigger a notification.
- Notification preferences/settings (mute, digest, email) — a simple always-on in-app list only.
- Knowledge Base, Customer Portal, communication channels, reports/dashboards, AI features, multi-department/branch/branding, or any other Story 11-14 scope.
