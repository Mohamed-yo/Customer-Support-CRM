# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/audit-logging/05/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Audit Logging Framework
- **Feature slug (folder under `plans/`):** `audit-logging`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `05`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,security,audit`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Audit Logging Framework
```

---

## Description

```
Add a minimal, backend-only audit logging framework that records who did what and when, starting with the two real mutating actions that exist in the system today: authentication (login attempts) and role assignment (Story 04's admin-only role grant endpoint). This is the first story to introduce a persisted audit trail — no entity or middleware for it exists yet.

This story is deliberately narrow: it establishes the AuditLog entity, the persistence mechanism, and a minimal Admin-only read endpoint. It does not add any frontend UI, viewer page, or navigation — Story 04 already established the precedent of extending backend capability (RBAC) without adding new frontend surface, and this story follows the same pattern. The existing authenticated App Shell (header, sidebar/drawer, language toggle, logout) and all of Story 01–04's behavior must remain completely untouched.

Concretely, after this story:
- A successful or failed login attempt (POST /api/auth/login) produces a retrievable audit record — who (email, and user id when known), when, and the outcome.
- A role assignment (POST /api/admin/users/{userId}/roles/{roleName}) produces a retrievable audit record — who performed it (the authenticated admin), on whom, which role, and when.
- A minimal Admin-only endpoint exists to retrieve recent audit records for verification purposes. No search, filter, or pagination UI is required — a simple "most recent N records" read is sufficient.
- Writing an audit record must never cause the underlying operation (login, role assignment) to fail. If audit logging itself breaks (e.g. database hiccup), the operation it's auditing must still succeed exactly as it does today — mirroring the existing dev-seed's try/catch resilience pattern already in Program.cs.
```

---

## Acceptance criteria

```
1. An `AuditLog` (or equivalently named) entity is persisted via EF Core against SQL Server, with a new additive migration (does not alter the `Users`, `Roles`, or `UserRoles` tables from Stories 03/04).
2. Every call to `POST /api/auth/login` produces an audit record capturing at least: timestamp, the attempted email, the outcome (success or failure), and the resulting user id when the login succeeded.
3. Every call to `POST /api/admin/users/{userId}/roles/{roleName}` produces an audit record capturing at least: timestamp, the acting admin's user id, the target user id, and the role name.
4. A minimal Admin-only endpoint (e.g. `GET /api/admin/audit-logs`) returns recent audit records (most-recent-first is sufficient; no filtering/search/pagination UI required this story).
5. A failure while writing an audit record must not cause the audited operation itself to fail — login and role assignment must continue to succeed/fail exactly as they do today regardless of audit-write outcome.
6. No frontend UI, page, or navigation item is added for audit logs. The existing authenticated App Shell (header, sidebar/drawer, language toggle, logout) is not modified in any way.
7. Existing Story 01–04 behavior is completely unaffected: `GET /api/health` (anonymous, unchanged), login/logout, session persistence, RBAC (401 unauthenticated / 403 unauthorized / 200 authorized on `/api/admin/*`), the App Shell, i18n (English/Arabic), and RTL/LTR all continue to work exactly as before.
8. No new NuGet packages, npm packages, UI libraries, or state-management libraries are introduced.
9. The backend builds; if the plan determines no frontend file needs to change (expected, since AC 6 forbids new UI), the frontend build/lint are unaffected by definition — do not touch frontend files just to have something to verify.
```

---

## Attachments

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: i18n & Responsive Design (completed); Story 03 — User & Agent Authentication, including the authenticated App Shell (completed); Story 04 — Roles & Permissions (completed).
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Controllers/AuthController.cs` (`Login` action — needs an audit hook on both success and failure paths), `backend/src/CustomerSupportCrm.Api/Controllers/AdminController.cs` (`AssignRole` action — needs an audit hook, and/or a new endpoint for reading audit records, likely on the same controller since it's already the Admin-only surface), `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently has `Users`, `Roles`, `UserRoles` — this story adds a fourth entity), `backend/src/CustomerSupportCrm.Api/Program.cs` (DI registration point, and the existing try/catch resilience pattern around the dev seed that any audit-write isolation should mirror).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01, Story 02, Story 03, or Story 04 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this is a foundation story (an audit primitive), not a compliance/reporting feature.
- Learn from Story 04's own planning history: an early draft of that plan added a frontend admin page/nav item that had to be stripped back out because it wasn't actually required by the acceptance criteria. Story 05 should not repeat that — there is no acceptance criterion here that requires any frontend change, so the plan should not invent one.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/` (expected to be untouched by this story).
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize(Roles = "Admin")]` as the existing authorization pattern (see `AdminController.cs`).
- Prefer explicit, targeted audit calls at the two known mutation points (login, role assignment) over a generic EF Core `SaveChanges` interceptor that audits every entity change — the domain has very few mutating actions today (seed, login, role assignment), so a generic interceptor is more machinery than this story's scope justifies. If the plan has a good reason to prefer an interceptor instead, it should say why.
- Consider whether the audit-write endpoint/service belongs on the existing `AdminController` (extending the already-established Admin-only surface) versus a new dedicated controller — either is acceptable; state and justify the choice in the plan.
- Mirror the existing `Program.cs` try/catch pattern (see the dev-seed call site) for isolating audit-write failures from the operation being audited, rather than inventing a different resilience mechanism.
- Do not introduce a new state-management library, a new CSS/styling library, a new HTTP client, a new i18n library, or any new NuGet/npm package.

## Out of scope

- Any frontend UI, page, viewer, or navigation item for audit logs (explicitly excluded by AC 6).
- A full generic "audit every entity/every change" interceptor covering hypothetical future entities (tickets, customers, agents) that don't exist yet.
- Retention, archival, or purging policy for audit records.
- Filtering, search, or pagination beyond a minimal "most recent N records" read.
- Auditing of read/query operations — only the two mutating actions named in the acceptance criteria (login, role assignment) are in scope.
- Any CRM domain functionality (tickets, customers, agents-as-a-business-entity).
- CI/CD, Docker, or production deployment/security hardening beyond what's needed for this story to function in development.
