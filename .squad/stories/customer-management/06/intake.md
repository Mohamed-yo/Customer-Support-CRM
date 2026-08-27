# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/06/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Customer Management
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `06`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,domain`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Customer Management
```

---

## Description

```
Introduce the first real CRM business entity: Customer. Stories 01–05 built platform infrastructure only (project scaffolding, i18n/responsive design, JWT authentication, RBAC, audit logging) — the repository currently has zero domain entities and the app has no actual customer-support functionality yet, despite being called "Customer Support CRM". This story adds that first domain entity and the minimal screens/endpoints needed to manage it, so future stories (e.g. support tickets/cases) have a Customer to attach to.

Unlike Story 04 and Story 05, which were deliberately backend-only and added no frontend UI, this story DOES require frontend UI — managing customer records is the actual product feature being delivered, not internal platform infrastructure. The existing authenticated App Shell already has an explicit extensibility point for this: `frontend/src/components/layout/AppShell.tsx` has a `NAV_ITEMS` array with a comment reading "future stories add sections here (Tickets, Customers, Agents, Reports, Settings, ...) without touching the shell's structure." This story is the first to use it.

Concretely, after this story:
- A `Customer` entity is persisted via EF Core / SQL Server (new additive migration).
- Authenticated users (both Admin and Agent — this is core day-to-day support work, not an admin-only function) can list customers, view a single customer, create a customer, and edit a customer's details.
- Deleting a customer is restricted to Admin only — it's the one destructive, hard-to-reverse action, consistent with how Story 04 already restricts destructive/sensitive admin actions to the Admin role.
- Every customer create/update/delete writes an audit record using the existing `AuditLogger` service from Story 05 (`backend/src/CustomerSupportCrm.Api/Auditing/AuditLogger.cs`) — this story should reuse that infrastructure exactly as-is, not build a parallel mechanism.
- A new "Customers" item appears in the App Shell's sidebar navigation (`NAV_ITEMS`), linking to the new customers list page, with EN/AR translations and RTL-safe layout following the same conventions already established (e.g. `start-`/`end-` logical Tailwind classes, not hardcoded `left-`/`right-`).
```

---

## Acceptance criteria

```
1. A `Customer` entity is persisted via EF Core against SQL Server, with a new additive migration (does not alter the `Users`, `Roles`, `UserRoles`, or `AuditLogs` tables from Stories 03/04/05). Minimum fields: an id, full name (required), email (required, validated format), phone (optional), and a created timestamp.
2. Backend REST endpoints exist for: list customers, get a single customer by id, create a customer, update a customer. All require authentication (`[Authorize]`) — both Admin and Agent roles can use them.
3. A backend delete-customer endpoint exists and is restricted to the Admin role only (`[Authorize(Roles = "Admin")]`), consistent with Story 04's existing Admin-only pattern.
4. Server-side validation rejects a create/update with a missing name or an invalid email format, returning a clear 400-level error — do not rely on frontend-only validation.
5. Every successful create, update, and delete of a customer writes an audit record via the existing `AuditLogger` service (Story 05), capturing at minimum: the action, the acting user, the affected customer id, and the timestamp. No new/parallel audit mechanism is introduced.
6. A new authenticated frontend page lists customers and supports creating and editing a customer (a single combined list+form page or a list page plus a separate create/edit page — the plan should choose and justify one). The page is reachable from a new "Customers" item added to the App Shell's `NAV_ITEMS`, and follows the existing App Shell layout, i18n (EN/AR), and RTL/LTR conventions.
7. Existing Story 01–05 behavior is completely unaffected: `GET /api/health` (anonymous, unchanged), login/logout, session persistence, RBAC (401 unauthenticated / 403 unauthorized / 200 authorized), audit logging for login and role-assignment, the App Shell's existing nav/header/logout/language toggle, i18n, and RTL/LTR all continue to work exactly as before.
8. No new NuGet packages, npm packages, UI component libraries, state-management libraries, or HTTP clients are introduced — use the existing Zustand/Axios/Tailwind/react-i18next stack.
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

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: i18n & Responsive Design (completed); Story 03 — User & Agent Authentication + authenticated App Shell (completed); Story 04 — Roles & Permissions / RBAC (completed); Story 05 — Audit Logging Framework (completed).
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently `Users`, `Roles`, `UserRoles`, `AuditLogs` — this story adds a fifth entity), `backend/src/CustomerSupportCrm.Api/Auditing/AuditLogger.cs` (reuse as-is for customer-mutation audit records), `backend/src/CustomerSupportCrm.Api/Controllers/` (new controller expected, following the existing `AdminController.cs`/`AuthController.cs` conventions — `[Authorize]`, `MapInboundClaims = false` JWT claims already in place), `frontend/src/components/layout/AppShell.tsx` (`NAV_ITEMS` — the documented extensibility point for this story), `frontend/src/routes/AppRouter.tsx` (new route(s) under the existing `RequireAuth`/`AppShell` route tree), `frontend/src/i18n/locales/en.json` and `ar.json` (new translation keys), `frontend/src/store/useAuthStore.ts` (existing `hasRole` selector — reuse for gating the delete action in the UI, do not duplicate role logic).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01–05 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- This story intentionally does require frontend UI (unlike Story 04/05) — it is the first real product feature, not platform infrastructure. Do not strip the UI back out the way Story 04's early draft did; that precedent applied to admin-only RBAC *proof* UI, not to an actual CRM feature's primary UI.
- Keep the implementation the smallest one that satisfies the acceptance criteria: basic list + create/edit + Admin-only delete. No advanced features (see Out of scope).

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]` as the existing authorization patterns.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-` classes for RTL safety), React Router v6, Zustand (`useAuthStore` for `hasRole`/session, `useAppStore` for app-level state), `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- Reuse the existing `AuditLogger` service and its action-naming convention (Story 05 used `auth.login`, `admin.role.assign`) — e.g. `customer.create` / `customer.update` / `customer.delete` would be consistent, but the plan should state and justify the exact names it picks.
- Reuse the existing `useAuthStore.hasRole('Admin')` selector (established in Story 04) to conditionally show/hide the delete action in the UI — the backend `[Authorize(Roles = "Admin")]` guard remains the actual enforcement; the frontend check is only to avoid showing a control that would 403.
- Consider whether create/update use a single upsert-style form component or two separate ones — either is acceptable; state and justify the choice in the plan.

## Out of scope

- Support ticket / case management, or any linkage between a customer and a ticket (no ticket entity exists yet — that is expected to be a future story).
- Customer notes/activity timeline, file attachments, or communication history.
- Customer search, filtering, sorting, or pagination beyond a simple full list (can be added in a later story once the customer list is expected to grow large).
- Soft-delete/restore, merge/deduplication, or import/export of customers.
- A customer-facing portal or any unauthenticated/self-service customer access.
- Assigning a customer to a specific agent, or any ownership/territory model.
- Changes to authentication, RBAC, audit logging, i18n infrastructure, or the App Shell's existing structure beyond adding the one new nav item and route(s) required by this story.
