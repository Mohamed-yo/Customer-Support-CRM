# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/roles-permissions/04/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Roles & Permissions
- **Feature slug (folder under `plans/`):** `roles-permissions`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `04`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,authorization`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Roles & Permissions
```

---

## Description

```
Add authorization (RBAC) on top of the authentication foundation Story 03 established. Story 03 deliberately shipped authentication only — a User entity, login/logout, JWT bearer middleware, and a single [Authorize]-protected /api/auth/me route with no concept of roles. Story 04 introduces roles and makes at least one real authorization decision (an endpoint that behaves differently, or is restricted, based on the caller's role).

This is the first story to add a second domain entity/relationship (Role, and a User-to-Role association) since Story 03's User entity and first migration. It is also the first story to extend the JWT claims and the authenticated App Shell established in Story 03's App Shell refinement, so the UI can reflect a signed-in user's role(s) — without rebuilding any of that shell.

Story 01, 02, and 03 (including the authenticated App Shell: header, sidebar/drawer, language toggle, logout) are complete and must not be modified except where authorization genuinely requires touching them (e.g. adding a role claim to the JWT, or gating a nav item by role).
```

---

## Acceptance criteria

```
1. A Role entity exists, persisted via EF Core against SQL Server, with a migration checked in (additive to Story 03's schema, not replacing it).
2. Users can be assigned one or more roles (or a single role, if that is the simplest design that satisfies the rest of these criteria — the plan should state and justify the choice).
3. The JWT issued at login includes the user's role(s) as a claim.
4. GET /api/auth/me reflects the caller's role(s) in its response.
5. At least one backend route demonstrates real role-based authorization: an authenticated request without the required role is rejected (403), and a request with the required role succeeds (200).
6. A minimal way exists to assign a role to a user in Development (e.g. extending the existing dev seed, or a minimal admin-only endpoint) — full role-management UI is not required by this story.
7. Existing Story 03 behavior is unaffected for users regardless of role: login, logout, session persistence, GET /api/auth/me (still 401 unauthenticated / 200 authenticated), and GET /api/health (still anonymous, unaffected) all continue to work exactly as before.
8. The authenticated App Shell (header, sidebar/drawer, language toggle, logout) is reused as-is; if the UI needs to reflect role information, it plugs into the existing shell/store rather than introducing a second header, sidebar, or auth store.
9. i18n: any new user-facing text (e.g. an access-denied message, a role label) is added to both en.json and ar.json following the existing key/namespace conventions, with no hard-coded strings.
10. RTL/LTR and responsive behavior for any new UI is consistent with the existing App Shell conventions — no new layout system.
11. Unauthorized/forbidden states are handled gracefully in the frontend (no raw stack traces or unhandled promise rejections surfaced to the user).
12. The backend builds; the frontend builds and lints; manual verification confirms the above.
```

---

## Attachments

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: i18n & Responsive Design (completed); Story 03 — User & Agent Authentication, including the authenticated App Shell refinement (completed).
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/User.cs`, `backend/src/CustomerSupportCrm.Api/Controllers/AuthController.cs` (login/me), `backend/src/CustomerSupportCrm.Api/Auth/JwtTokenService.cs` (claim issuance), `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently one entity, `User`, one migration `InitialUser`), `backend/src/CustomerSupportCrm.Api/Program.cs` (`AddAuthorization()` is already called with no policies configured), `frontend/src/store/useAuthStore.ts` (currently holds `token`/`user`/`expiresAtUtc`, no role field), `frontend/src/components/layout/AppShell.tsx` and `frontend/src/components/LanguageToggle.tsx` (the reusable authenticated shell — reuse, do not duplicate).

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01, Story 02, or Story 03 files. Do not regress authentication, session persistence, protected routes, the App Shell (header/sidebar/drawer/language toggle/logout), i18n/RTL, or `/api/health`.
- Do not create a new overall project plan or touch `.squad/plans/project-setup/`, `.squad/plans/platform-foundation-internationalization-responsive-design/`, or `.squad/plans/user-agent-authentication/`.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this is still a foundation story (authorization primitives), not a full roles-management admin screen.
- Design/UI quality matters wherever this story touches the UI: any new UI must look and behave like it belongs in the same App Shell as the rest of the app — same conventions, same i18n mechanism, no ad hoc styling.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, Swagger, JWT bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`, `MapInboundClaims = false` so claim types are used exactly as issued), `PasswordHasher<User>` (not the full ASP.NET Core Identity framework).
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3, React Router v6, Zustand (`useAppStore` for app/language state, `useAuthStore` for auth/session state — both use `zustand/middleware`'s `persist` + `createJSONStorage(localStorage)` + `partialize`), `react-i18next`/`i18next`.
- Prefer ASP.NET Core's built-in role-claim + policy/`[Authorize(Roles = "...")]` mechanism over inventing a custom authorization scheme, to stay consistent with the JWT bearer setup already in place.
- Inspect `useAuthStore.ts`'s existing `AuthUser`/`AuthState` shape and `partialize` list before deciding how role information is added to client state — extend the existing store's shape rather than introducing a second auth-related store.
- Inspect `AppShell.tsx` and its `NAV_ITEMS` extensibility point before deciding whether/how role affects navigation — reuse that existing extension point rather than building a parallel navigation mechanism.
- Do not introduce a new state-management library, a new CSS/styling library, a new HTTP client, or a new i18n library.

## Out of scope

- A full roles/permissions management UI (create/edit/delete roles, assign roles to arbitrary users via the UI) — a minimal Development-only assignment mechanism is sufficient per acceptance criterion 6.
- Department/branch-scoped permissions or any multi-tenancy concept.
- Audit logging of authorization decisions (a separate, later story).
- Password reset, MFA, SSO/OAuth, or any other authentication-layer change beyond adding the role claim.
- Customer-facing authentication or the customer portal.
- Any CRM domain functionality (tickets, customers, agents-as-a-business-entity).
- CI/CD, Docker, or production deployment/security hardening beyond what's needed for this story to function in development.
