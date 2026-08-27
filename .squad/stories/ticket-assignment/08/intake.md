# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/ticket-assignment/08/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Ticket Assignment to Agents
- **Feature slug (folder under `plans/`):** `ticket-assignment`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `08`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,domain`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Ticket Assignment to Agents
```

---

## Description

```
Add the ability to assign a support ticket to a specific user (an Agent or Admin), so tickets can be routed to whoever is working them. Story 07 introduced Ticket as the second core domain entity, and its intake explicitly deferred "assigning a ticket to a specific Agent/user, or any ownership/queue model" to a future story — this is that story.

Concretely, after this story:
- A `Ticket` gains an optional `AssignedToUserId` (nullable FK to `Users`). A ticket with no assignee is a valid, normal state ("Unassigned").
- The backend must expose a way to list assignable users (i.e. users holding the `Agent` or `Admin` role) so the frontend can offer a picker. No such "list users" endpoint currently exists anywhere in the API — the only related endpoint is the Admin-only `POST /api/admin/users/{userId}/roles/{roleName}` (role assignment), which is a mutation, not a list, and is gated `[Authorize(Roles = "Admin")]` at the controller level. Because both Admins and Agents can already create/update tickets (Story 07), **Agents must also be able to see and use the assignee picker** — so the new "list assignable users" capability cannot simply be bolted onto the existing Admin-only controller as-is; it needs its own `[Authorize]`-only (not Admin-only) exposure. How exactly to structure that (new controller, new action on an existing non-admin-gated controller, etc.) is left to the plan to decide and justify.
- The ticket create/update endpoints accept an optional `AssignedToUserId` in the request body. If provided, the backend must validate it references an existing user who actually holds the `Agent` or `Admin` role (reject an arbitrary user id, e.g. a non-agent account) — mirroring the existing `customer_not_found`-style validation Story 07 already established for `CustomerId`.
- The ticket list/get responses include the assignee's id and display name (or `null`/absent when unassigned) — following the same "embed the related name to avoid an N+1 frontend call" approach Story 07 used for `CustomerFullName`.
- The Tickets page's create/edit form gains an "Assigned to" picker (a `<select>`, matching the existing Customer picker's shape) with an explicit "Unassigned" option, and the tickets list/table displays the assignee's name (or an "Unassigned" label) in a new column.
- Assigning/reassigning a ticket is done through the existing ticket update flow — it does not require a new permission tier. Anyone who can already update a ticket (Admin or Agent, per Story 07) can also change its assignee. Only ticket *delete* remains Admin-only, unchanged from Story 07.
```

---

## Acceptance criteria

```
1. `Ticket` gains a nullable `AssignedToUserId` (FK to `Users`) via a new additive EF Core migration (does not alter `Users`, `Roles`, `UserRoles`, `AuditLogs`, `Customers`, or the existing `Tickets` columns from Story 07 — only adds the new column/FK).
2. A new endpoint (authenticated, **not** Admin-only) returns the list of users holding the `Agent` or `Admin` role, with at least their id and display name, for use as the assignee picker's data source.
3. `POST /api/tickets` and `PUT /api/tickets/{id}` accept an optional `assignedToUserId`. If provided and non-null, the backend validates it resolves to an existing user who holds the `Agent` or `Admin` role; an invalid/non-agent id is rejected with a clear 400-level error. Omitting it (or sending null) leaves/sets the ticket unassigned.
4. `GET /api/tickets` and `GET /api/tickets/{id}` responses include the assignee's id and display name (or an explicit unassigned representation) alongside the existing customer fields.
5. Assigning/reassigning a ticket does not require a new authorization tier: any authenticated user who can already update a ticket (Admin or Agent, per Story 07) can set or change its assignee. Ticket delete remains `[Authorize(Roles = "Admin")]`, unchanged.
6. Every successful create/update that changes the assignment is still audited through the existing `AuditLogger`-based mechanism from Story 07 (reuse `ticket.create`/`ticket.update` — do not introduce a separate `ticket.assign` action unless the plan gives a specific justification for one).
7. The Tickets page's create/edit form includes an "Assigned to" picker (populated from the new endpoint in AC 2) with an explicit "Unassigned" option, and the tickets table displays the assignee's name or an "Unassigned" label in a new column. Follows the existing App Shell/i18n(EN/AR)/RTL/responsive conventions already established by `TicketsPage.tsx`.
8. Existing Story 01–07 behavior is completely unaffected: health, auth, RBAC, audit logging, Customers CRUD, existing Ticket CRUD/validation/status behavior, App Shell nav/header/logout/language toggle, i18n, and RTL/LTR all continue to work exactly as before.
9. No new NuGet packages, npm packages, UI component libraries, state-management libraries, or HTTP clients are introduced.
10. The backend builds cleanly; the frontend builds and lints cleanly.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: i18n & Responsive Design (completed); Story 03 — User & Agent Authentication + authenticated App Shell (completed); Story 04 — Roles & Permissions / RBAC (completed) — `Role`/`UserRole` entities and role names (`Admin`, `Agent`) are what "assignable user" filters on; Story 05 — Audit Logging Framework (completed); Story 06 — Customer Management (completed); Story 07 — Support Ticket Management (completed) — this story extends the `Ticket` entity and `TicketsController` it introduced, and its intake explicitly deferred agent assignment to a future story.
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Ticket.cs` (add `AssignedToUserId`/nav property), `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently configures `Ticket`'s FK to `Customer` — add the second, nullable FK to `User` with an appropriate `OnDelete` behavior; state and justify the choice, e.g. `SetNull` so deleting a user doesn't block deleting/updating tickets, unlike the deliberate `Restrict` chosen for the Customer FK), `backend/src/CustomerSupportCrm.Api/Controllers/TicketsController.cs` (`Create`/`Update`/`List`/`Get` all need to read/validate/project the new field), `backend/src/CustomerSupportCrm.Api/Controllers/TicketDtos.cs` (`TicketListItem`/`TicketUpsertRequest` both need the new field), `backend/src/CustomerSupportCrm.Api/Controllers/AdminController.cs` (existing Admin-only surface — reference only for the `[Authorize(Roles = "Admin")]` pattern; do **not** add the new "list assignable users" endpoint here, since it's class-level Admin-only and Agents need this data too), `backend/src/CustomerSupportCrm.Api/Domain/User.cs`/`Role.cs`/`UserRole.cs` (query surface for "users holding Agent or Admin role"), `frontend/src/api/tickets.ts` (extend `Ticket`/`TicketUpsert` types, add a function for the new assignable-users endpoint or a new small API module for it), `frontend/src/pages/TicketsPage.tsx` (add the assignee picker to the form and a column to the table, reusing its existing derived-validation pattern), `frontend/src/i18n/locales/en.json` and `ar.json` (new `tickets.*` keys for the assignee field/column/"Unassigned" label).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01-07 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- Reuse Story 07's exact validation/error-code style (`{ error = "..." }` 400 responses, client-side derived-error display) for the new `assignedToUserId` validation, rather than inventing a different error-shape convention.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this is a field + a picker + a list endpoint, not a full workload/queue management feature.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand (`useAuthStore.hasRole`), `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- The "assignable users" query is: users that have a `UserRole` row pointing at a `Role` named `Agent` or `Admin` — this is exactly the same role model Story 04 already established; no new role/permission concept is needed.
- Consider whether the new list-assignable-users endpoint belongs on `TicketsController` (e.g. `GET /api/tickets/assignable-users`) versus a small new controller — either is acceptable; state and justify the choice in the plan, same as Story 07 did for its own controller-shape decisions.
- The existing `Customer` FK on `Ticket` uses `OnDelete(DeleteBehavior.Restrict)` (deliberately, so a customer with tickets can't be silently orphaned). The new `User` FK should be considered separately on its own merits — a user (Agent) being removed from the system shouldn't necessarily block ticket operations the way a missing customer does. State and justify whichever `OnDelete` behavior the plan chooses.

## Out of scope

- An "assigned to me" filter/view, or any ticket search/filter/sort/pagination in general (explicitly deferred separately, same as Story 06/07's posture).
- Multiple assignees per ticket, assignment history/timeline, or reassignment notifications/emails.
- A distinct "assign ticket" permission separate from the existing ticket-update permission (Admin/Agent can already update; this story doesn't add a narrower or broader tier).
- Any change to the Customer entity/controller/page, or to Story 07's ticket Status/Subject/Description behavior, beyond adding the assignee field.
- Any change to authentication, RBAC role definitions, audit logging mechanics, i18n infrastructure, or the App Shell's existing structure.
