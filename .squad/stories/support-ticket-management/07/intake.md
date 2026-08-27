# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/support-ticket-management/07/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Support Ticket Management
- **Feature slug (folder under `plans/`):** `support-ticket-management`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `07`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,domain`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Support Ticket Management
```

---

## Description

```
Introduce the second core CRM business entity: Ticket (a support request/case). Story 06 introduced Customer as the first domain entity; this story adds Ticket, linked to an existing Customer, so the application starts to resemble an actual "Customer Support" tool rather than just a customer directory. Stories 01-06 established the full platform: JWT auth + RBAC (Admin/Agent roles), an audit-logging framework (`AuditLogger`), and the Customer feature itself (entity + CRUD controller + authenticated list/create/edit page reachable from the App Shell's `NAV_ITEMS`).

This story follows the exact same shape as Story 06, reusing its established patterns rather than inventing new ones:
- A `Ticket` entity persisted via EF Core / SQL Server (new additive migration), with a required link to an existing `Customer` (by id), a required Subject, an optional Description, and a Status drawn from a small fixed set of values (e.g. Open / InProgress / Closed).
- Backend endpoints for list/get/create/update, requiring authentication (both Admin and Agent — same as Customer), and a delete endpoint restricted to Admin only (same pattern as Story 06's customer delete).
- Every successful create/update/delete writes an audit record via the existing `AuditLogger` service (Story 05), using the same `<area>.<verb>` action-naming convention already established (`customer.create` etc. → `ticket.create`, `ticket.update`, `ticket.delete`).
- A new authenticated frontend page, structured like `CustomersPage.tsx` (list + create/edit modal form, Admin-only delete, client-side validation with inline errors, i18n EN/AR, RTL-safe logical Tailwind classes), reachable from a new "Tickets" item added to the App Shell's `NAV_ITEMS` (the same extensibility point Story 06 used).
- The ticket form's Customer field must reference an existing customer — implemented as a selection (e.g. a `<select>`) populated from the existing `GET /api/customers` endpoint, not free-text entry, so a ticket can never reference a nonexistent customer.

This story is deliberately scoped to the minimum that makes a ticket a real, usable record: creating one, listing them, changing its status/details, and (Admin) deleting one. It intentionally excludes comment threads, attachments, agent assignment, SLA/due dates, filtering/search, and notifications — those are candidates for later stories once the core entity exists.
```

---

## Acceptance criteria

```
1. A `Ticket` entity is persisted via EF Core against SQL Server, with a new additive migration (does not alter `Users`, `Roles`, `UserRoles`, `AuditLogs`, or `Customers`). Minimum fields: an id, a required foreign key to an existing `Customer`, a required Subject (bounded length), an optional Description, a Status (drawn from a small fixed set of allowed string values, e.g. Open/InProgress/Closed), and a created timestamp.
2. Backend endpoints exist for: list tickets, get a single ticket by id, create a ticket, update a ticket (including changing its Status). All require authentication (`[Authorize]`) — both Admin and Agent roles can use them.
3. A backend delete-ticket endpoint exists and is restricted to the Admin role only (`[Authorize(Roles = "Admin")]`), consistent with Story 06's customer-delete pattern.
4. Server-side validation rejects a create/update with a missing Subject, a CustomerId that does not reference an existing customer, or a Status outside the allowed set, returning a clear 400-level error — do not rely on frontend-only validation.
5. Every successful create, update, and delete of a ticket writes an audit record via the existing `AuditLogger` service (Story 05), capturing at minimum: the action, the acting user, the affected ticket id, and the timestamp. No new/parallel audit mechanism is introduced.
6. A new authenticated frontend page lists tickets and supports creating and editing a ticket, including selecting an existing customer from a list (not free-text) and choosing a Status from the fixed set. The page is reachable from a new "Tickets" item added to the App Shell's `NAV_ITEMS`, and follows the existing App Shell layout, i18n (EN/AR), and RTL/LTR conventions — matching the structure already established by `CustomersPage.tsx`.
7. Existing Story 01-06 behavior is completely unaffected: `GET /api/health` (anonymous, unchanged), login/logout, session persistence, RBAC (401 unauthenticated / 403 unauthorized / 200 authorized), audit logging for existing actions, the Customers page and its CRUD/validation behavior, the App Shell's existing nav/header/logout/language toggle, i18n, and RTL/LTR all continue to work exactly as before.
8. No new NuGet packages, npm packages, UI component libraries, state-management libraries, or HTTP clients are introduced — use the existing stack.
9. The backend builds cleanly; the frontend builds and lints cleanly.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: i18n & Responsive Design (completed); Story 03 — User & Agent Authentication + authenticated App Shell (completed); Story 04 — Roles & Permissions / RBAC (completed); Story 05 — Audit Logging Framework (completed); Story 06 — Customer Management (completed) — Ticket must reference an existing Customer.
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Customer.cs` (the entity a Ticket must reference), `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently `Users`, `Roles`, `UserRoles`, `AuditLogs`, `Customers` — this story adds a sixth entity, with a foreign key to `Customers`), `backend/src/CustomerSupportCrm.Api/Auditing/AuditLogger.cs` (reuse as-is), `backend/src/CustomerSupportCrm.Api/Controllers/CustomersController.cs` (the structural template for the new `TicketsController` — same `[Authorize]`/`[Authorize(Roles = "Admin")]` split, same actor-id-from-`sub`-claim pattern), `frontend/src/pages/CustomersPage.tsx` (the structural and validation-pattern template for the new `CustomersPage`-equivalent for tickets, including the recently-added client-side validation approach — derived-state field errors, `noValidate` on the form, blur/attempted-submit display logic), `frontend/src/api/customers.ts` (the template for a new `frontend/src/api/tickets.ts`, and also the actual data source for the ticket form's customer-selection list via `listCustomers()`), `frontend/src/components/layout/AppShell.tsx` (`NAV_ITEMS` — the documented extensibility point, already used once by Story 06), `frontend/src/routes/AppRouter.tsx` (new route(s) under the existing `RequireAuth`/`AppShell` route tree), `frontend/src/i18n/locales/en.json` and `ar.json` (new translation keys, following the existing `customers.*` key shape as a model for a new `tickets.*` section).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01-06 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- Reuse `CustomersPage.tsx`'s validation pattern (derived `validateForm`-style errors, touched/attemptedSubmit display logic, `noValidate` on the form) rather than inventing a different validation approach for the ticket form.
- Reuse the existing `AuditLogger` and its `<area>.<verb>` action-naming convention exactly as Story 06 did for `customer.*`.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this is the second core domain entity, not a full ticketing/helpdesk system.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand (`useAuthStore.hasRole`), `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- Model the `Status` field as a plain `string` with a small allowed-values check (client and server), not a new enum-mapping abstraction or a separate lookup table — matches this codebase's preference for the smallest mechanism that satisfies the requirement (e.g. Story 06 modeled `Customer.Phone` as a free-form string rather than a structured type).
- The ticket form's customer selector should call the existing `listCustomers()` from `frontend/src/api/customers.ts` — do not duplicate that fetch logic or add a new customers-list endpoint.
- Consider whether `TicketsController` needs to `Include`/join `Customer` data (e.g. to show the customer's name in the ticket list) versus the frontend making a second call — either is acceptable; state and justify the choice in the plan.

## Out of scope

- Ticket comments/notes, activity/audit timeline in the UI, or file attachments on a ticket.
- Assigning a ticket to a specific Agent/user, or any ownership/queue model.
- SLA tracking, due dates, priority levels, or escalation.
- Search, filtering, sorting, or pagination beyond a simple full list (same posture Story 06 took for customers).
- Email/notification triggers on ticket create/update.
- Any change to the Customer entity/controller/page beyond consuming its existing list endpoint.
- Changes to authentication, RBAC, audit logging, i18n infrastructure, or the App Shell's existing structure beyond adding the one new nav item and route(s) required by this story.
