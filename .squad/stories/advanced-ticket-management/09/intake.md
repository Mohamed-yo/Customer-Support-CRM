# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/advanced-ticket-management/09/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Advanced Ticket Management & Agent Workspace
- **Feature slug (folder under `plans/`):** `advanced-ticket-management`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `09`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,domain`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Advanced Ticket Management & Agent Workspace
```

---

## Description

```
Consolidates 12 previously-audited CRM capabilities (from the "Remaining CRM Scope" audit, Story 09 of the approved 6-story consolidation: 09-14) into one cohesive story that deepens the existing Ticket/Customer entities and turns ticket-handling into a real "agent workspace," without touching SLA/automation, notifications, Knowledge Base, Customer Portal, communication channels, reporting, AI, or multi-tenancy (all explicitly deferred to Stories 10-14).

Architecture decisions made for this intake (the planner should follow these, not re-derive different ones):

1. **Ticket categories / priorities** — modeled exactly like Story 07's `Ticket.Status`: a plain `string` field with a small server-side allow-list constant, no enum-mapping abstraction or lookup table. `Category` allow-list: `General`, `Billing`, `Technical`, `Account`. `Priority` allow-list: `Low`, `Normal`, `High`, `Urgent`.

2. **Per-ticket history** — reuse the existing `AuditLog` table (Story 05) as-is. No new entity, no schema change. A new `GET /api/tickets/{id}/history` endpoint queries `AuditLogs` where `Action` starts with `"ticket."` and `Details == id.ToString()`, ordered by `TimestampUtc`. This is a "who did what, when" narrative (create/update/delete/assign-via-update), not a field-level before/after diff — a full diff engine is out of scope for this story.

3. **Customer interaction history** — same pattern: a new `GET /api/customers/{id}/history` endpoint queries `AuditLogs` where `Action` starts with `"customer."` and `Details == id.ToString()`. Combined in the UI with that customer's ticket list (see #8) to form the "interaction history" view.

4. **Ticket/customer notes** AND **Team collaboration** are the SAME mechanism, not two systems: one new `TicketNote` entity (ticket-scoped, free-text, author + timestamp), rendered as a shared comment thread visible to every Admin/Agent who can see the ticket. There is no separate "internal team chat" entity — the notes thread on a ticket **is** the team-collaboration surface. (Customer-level notes are out of scope for this pass — the intake's "Ticket/customer notes" bullet is satisfied via ticket-scoped notes, since every ticket already carries a `CustomerId`; a customer's notes are effectively the union of notes on their tickets, visible from the Customer Detail view's ticket list.)

5. **Attachments** — one new `TicketAttachment` entity, ticket-scoped. Files are stored as `varbinary(max)` directly in SQL Server (no blob storage / filesystem dependency — this project has no cloud storage configured anywhere, and adding one would be a new external dependency this story should avoid). Hard cap: 5 MB per file, enforced server-side. Upload via `multipart/form-data` POST; list + metadata GET; download GET (streams bytes with the stored content-type); delete (Admin or Agent, matching the existing ticket-update permission — no new granular permission concept).

6. **Assigned-to-me ticket view** — a client-side toggle on the existing Tickets list ("All tickets" / "My tickets"), filtering the already-fetched ticket list by `assignedToUserId === current user's id`. No new server-side query-param filtering is introduced (that belongs to the later, dedicated search/filter story) — this is a lightweight, scoped exception because "assigned to me" is explicitly named in this story's approved scope.
   - **Blocking gap found:** the current `LoginResponse` (`Auth/AuthDtos.cs`) and the frontend `AuthUser`/`useAuthStore` do NOT carry the logged-in user's own `id` — only email/displayName/roles. `MeResponse` already has `Id`, but `/api/auth/me` is never called anywhere in the frontend (dead code since Story 03). This story must add `Id` to `LoginResponse` (additive, non-breaking) and to `AuthUser`/`setSession` so the client can know "who am I" for the my-tickets filter. This is the one small necessary touch to Story 03's auth contract — everything else about auth/session/RBAC is unchanged.

7. **Tasks** AND **Reminders** are the SAME entity, not two: one new `TicketTask` (ticket-scoped: `Title`, optional `DueAtUtc`, `IsDone`). A task with no due date is a plain checklist item; a task with a due date is a "reminder" and is visually highlighted when due today or overdue. No separate reminder/notification delivery mechanism (that's Story 10's "Alerts & Notifications") — this story only surfaces due/overdue state visually within the page, it does not push anything.

8. **Customer context in the agent workspace** — a new **Ticket Detail page** (`/tickets/:id`) becomes the "agent workspace" for a single ticket: it shows the ticket's fields, an inline customer-summary panel (name/email/phone, fetched via the existing Customer entity), the notes/collaboration thread (#4), attachments (#5), tasks (#7), and the per-ticket history (#2) — all on one page. This is additive: the existing `TicketsPage.tsx` list + its quick-edit modal (Story 07/08) are left completely intact for fast field edits; a new "View" action per row navigates to the new detail page for the richer capabilities. Symmetrically, a new **Customer Detail page** (`/customers/:id`) shows the customer's contact info, their ticket list (reusing `GET /api/tickets` — see note below), and their interaction history (#3); `CustomersPage.tsx`'s existing list + modal are likewise left intact, with a new "View" action added per row.

9. **Quick replies** — one new `QuickReplyTemplate` entity: a shared library of reusable text snippets (`Title`, `Body`), manageable by any Admin/Agent (matching the existing Customer/Ticket create/update permission pattern — no delete-is-Admin-only distinction is required here since templates are low-risk shared content, not customer/financial data; state this choice and allow the plan to confirm or adjust it). Used from the Ticket Detail page's note-composer: selecting a template populates the note textarea, which the agent can edit before posting as a `TicketNote`. There is no live messaging channel to "reply" through yet (that is Story 12) — quick replies only ever populate the internal notes thread in this story.

10. `GET /api/tickets` needs a way to return only a given customer's tickets for the Customer Detail page's ticket list (#8). Add an optional `?customerId=` query parameter to the existing list endpoint (backward-compatible — omitted means "all tickets," exactly like today) rather than introducing a second endpoint.
```

---

## Acceptance criteria

```
1. `Ticket` gains `Category` and `Priority` string fields, each validated server-side against a fixed allow-list (same pattern as `Status`), via a new additive migration. Existing tickets get sensible defaults (e.g. `Category = "General"`, `Priority = "Normal"`) with no data loss.
2. `GET /api/tickets/{id}/history` returns the ticket's audit trail (action, outcome, actor, timestamp) ordered chronologically, using the existing `AuditLog` table — no new history-specific table.
3. `GET /api/customers/{id}/history` returns the customer's audit trail the same way.
4. A new `TicketNote` entity supports listing and creating notes on a ticket (`GET`/`POST /api/tickets/{id}/notes`), authenticated (`[Authorize]`, both Admin and Agent) — this is both the "notes" and the "team collaboration" surface.
5. A new `TicketAttachment` entity supports upload, list, download, and delete of files on a ticket (`POST`/`GET`/`GET .../{attachmentId}/download`/`DELETE`), stored as `varbinary(max)`, capped at 5 MB per file, authenticated the same as ticket update (Admin or Agent).
6. A new `TicketTask` entity supports listing, creating, updating (including toggling done / setting a due date), and deleting tasks on a ticket — same auth pattern.
7. A new `QuickReplyTemplate` entity supports listing, creating, updating, and deleting reusable text snippets — same auth pattern (Admin or Agent).
8. The Tickets list page gains an "All tickets" / "My tickets" toggle that filters to the current user's assigned tickets, using a newly-added `id` on the logged-in user's session (an additive, backward-compatible change to `LoginResponse`/`AuthUser`).
9. A new Ticket Detail page (`/tickets/:id`) shows the ticket's full fields, an inline customer-context panel, the notes/collaboration thread (with a quick-reply picker), attachments, tasks, and per-ticket history — reachable via a new "View" action from the existing Tickets list, which itself is otherwise unchanged.
10. A new Customer Detail page (`/customers/:id`) shows the customer's contact info, their tickets (via `GET /api/tickets?customerId=`), and their interaction history — reachable via a new "View" action from the existing Customers list, which itself is otherwise unchanged.
11. All new frontend surfaces follow the existing App Shell/i18n(EN/AR)/RTL/responsive conventions already established by `TicketsPage.tsx`/`CustomersPage.tsx`. All new user-facing strings have both English and Arabic translations.
12. Existing Story 01-08 behavior is completely unaffected: health, auth, RBAC, audit logging, existing Customer/Ticket CRUD (including Story 07/08's validation, status, and assignment behavior), App Shell nav/header/logout/language toggle, i18n, and RTL/LTR all continue to work exactly as before.
13. No new NuGet packages, npm packages, UI component libraries, state-management libraries, or HTTP clients are introduced.
14. The backend builds cleanly; the frontend builds and lints cleanly.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Stories 01-08 (all completed and merged into `main`) — directly extends the `Ticket`/`Customer` entities and controllers from Stories 06/07/08, reuses the `AuditLog`/`AuditLogger` from Story 05, the RBAC model from Story 04, and the auth/session model from Story 03.
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Ticket.cs`, `Controllers/TicketDtos.cs`, `Controllers/TicketsController.cs` (Category/Priority fields, new sub-resource endpoints), `backend/src/CustomerSupportCrm.Api/Controllers/CustomersController.cs` (new history endpoint), `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (new entities' fluent config), `backend/src/CustomerSupportCrm.Api/Auth/AuthDtos.cs` (`LoginResponse` gains `Id`), `frontend/src/store/useAuthStore.ts` (`AuthUser` gains `id`), `frontend/src/pages/TicketsPage.tsx` and `CustomersPage.tsx` (add a "View" action; otherwise unchanged), `frontend/src/routes/AppRouter.tsx` (two new routes), `frontend/src/components/layout/AppShell.tsx` (no nav changes expected — ticket/customer detail is reached via a row action, not a new top-level nav item, unless the plan finds a reason to add one), `frontend/src/i18n/locales/en.json`/`ar.json` (substantial new key additions).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01-08 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- This story was pre-scoped in detail across two prior analysis passes (a full CRM audit, then a 6-story consolidation) that the user has already reviewed and approved — the architecture decisions in the Description section above are final, not open questions for the plan to re-litigate. The plan should focus on translating them into concrete tasks, file-by-file, in the same style as Stories 06-08's plans.
- Keep every new entity/endpoint the smallest shape that satisfies its acceptance criterion — this story is already large (12 original capabilities); do not add speculative extensibility (no generic "comments on anything" polymorphic system, no generic file-storage abstraction beyond what's needed for ticket attachments).

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand, `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- For file upload/download, use ASP.NET Core's built-in `IFormFile` / `FileContentResult` — no new package is needed for either.
- Follow `TicketsController.cs`'s existing `GetActorUserId()` / `AuditLogger.WriteAsync` pattern for any new mutation that should be audited (e.g. note/task/attachment create — state in the plan whether these should audit-log at all; given Story 05's stated posture of auditing only the two original mutation points, adding audit entries for every note/task/attachment may be more than this story needs — the plan should decide and justify).
- Reuse `CustomersPage.tsx`/`TicketsPage.tsx`'s existing client-side validation pattern (derived `validateForm`, touched/attemptedSubmit) for any new forms (task create, attachment upload, quick-reply template CRUD).

## Out of scope

- SLA/automation, alerts, notifications, automatic assignment, escalation rules (Story 10).
- Knowledge Base and Customer Portal in their entirety (Story 11).
- Any communication channel (email, WhatsApp, SMS, live chat, web forms) or external/ERP integration (Story 12).
- Reports, SLA/agent-performance metrics, management dashboards, customer-satisfaction reporting (Story 13).
- AI features of any kind, and Platform Administration items — Users/Roles management UI, runtime system configuration, multi-department, multi-branch, custom branding (Story 14).
- General search/filter/sort/pagination across Customers or Tickets (a separate, not-yet-scheduled concern; only the specific "assigned to me" toggle named in this story's scope is included).
- Field-level audit diffing ("what changed from what to what") — per-ticket/customer history in this story is an action narrative only, reusing the existing `AuditLog` shape as-is.
- A generic/polymorphic notes-and-attachments system usable by entities other than `Ticket`.
- Any change to Story 07/08's existing Ticket Status/Assignment validation behavior, or to Story 06's existing Customer validation behavior, beyond what's needed to add Category/Priority and the two new sub-resource areas.
