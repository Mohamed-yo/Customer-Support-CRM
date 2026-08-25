# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/user-agent-authentication/03/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** User & Agent Authentication
- **Feature slug (folder under `plans/`):** `user-agent-authentication`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `03`
- **Work item type:** `User Story`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `backend,frontend,authentication`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
User & Agent Authentication
```

---

## Description

```
Establish the first real domain entity and the authentication foundation for the Customer Support CRM: an internal Agent/User identity that can log in to the application, and a mechanism for the backend to reject unauthenticated requests to protected API routes.

This is the first story to introduce an actual EF Core entity and migration — Story 01 deliberately shipped `AppDbContext` with no entities, and Story 02 only touched the frontend shell (i18n, RTL, responsive layout). Story 03 builds on both:

- Backend: add a `User` (internal agent) entity, persist it via EF Core + SQL Server, add password hashing, and add a login endpoint that issues a token. Protect at least one existing or new endpoint so unauthenticated requests are rejected.
- Frontend: add a minimal login screen and wire the app so authenticated state gates access to the existing `HomePage`, reusing the design system, layout primitives, i18n, and RTL/LTR behavior already established in Story 02 (`PageContainer`, `LanguageSwitcher`, Tailwind conventions, `useAppStore` patterns).

Story 01 and Story 02 are complete and must not be modified or regressed. Only change what authentication genuinely requires.
```

---

## Acceptance criteria

```
1. A `User` (agent) entity exists, persisted via EF Core against SQL Server, with a first migration checked in.
2. Passwords are stored hashed, never in plain text.
3. A login endpoint accepts credentials and returns a token (or equivalent session artifact) on success, and a clear error on failure.
4. At least one backend route is protected: an unauthenticated request to it is rejected (401/403); an authenticated request succeeds.
5. `GET /api/health` remains unauthenticated and unaffected — it must continue to work exactly as before (Story 01 contract).
6. The frontend has a login screen that authenticates against the new endpoint and stores the resulting session client-side.
7. Once logged in, the existing `HomePage` (title from `useAppStore`, `/api/health` display, language switcher) continues to work exactly as it does today — no regression to Story 02 behavior.
8. An unauthenticated user is routed to the login screen instead of the app content; a logged-in user is not shown the login screen again on reload (session persists across reload, consistent with how language persistence already works).
9. The login screen and any new UI follow the existing design system: reuses `PageContainer`, Tailwind conventions already in the codebase, i18n (`react-i18next`) for all user-facing text in both English and Arabic, and correct RTL/LTR behavior — no ad hoc styling that diverges from Story 02's conventions.
10. Existing Story 01 and Story 02 functionality, routes, components, and behavior are preserved unless a change is explicitly required to support authentication (e.g., wrapping a route to require login).
11. Logout is available and returns the user to the login screen, clearing the client-side session.
12. The backend builds and runs; the frontend builds, lints, and runs — same verification bar as Story 01/02.
```

---

## Attachments

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup (completed); Story 02 — Platform Foundation: Internationalization & Responsive Design (completed).
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (currently no entities — this story adds the first one and the first migration), `backend/src/CustomerSupportCrm.Api/Program.cs` (currently no auth middleware configured), `frontend/src/store/useAppStore.ts` and the i18n/layout foundation from Story 02 (`frontend/src/i18n/`, `frontend/src/components/layout/PageContainer.tsx`, `frontend/src/components/LanguageSwitcher.tsx`).

## Extra notes (optional)

- Work on branch `feature/story-03-user-agent-authentication`. Do not create a new branch beyond this one.
- Do not modify or recreate Story 01 or Story 02 files. Do not regress `GET /api/health`, the existing `HomePage`, the i18n/RTL foundation, or the Zustand `appName`/`language` state.
- Do not create a new overall project plan or touch `.squad/plans/project-setup/` or `.squad/plans/platform-foundation-internationalization-responsive-design/`.
- Keep the implementation the smallest one that satisfies the acceptance criteria — this is still a foundation story, not full account management.
- Design/UI quality matters: the login screen must look and behave like it belongs in the same app as `HomePage` — same spacing/typography/color conventions, same responsive breakpoints, same RTL support, same translation mechanism. No inconsistent or throwaway styling.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, Swagger, CORS already configured for `http://localhost:5173` in `Program.cs`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3, React Router v6, Zustand, Axios, `react-i18next`/`i18next` (added in Story 02).
- Prefer the smallest standard approach for a SPA + API: a `User` entity with hashed password (e.g. ASP.NET Core's built-in `PasswordHasher<T>`, not the full Identity framework, to keep scope minimal), a login endpoint issuing a bearer token (e.g. JWT), and `[Authorize]`/JWT bearer authentication middleware protecting routes. Verify actual package/version choices against what's already installed before adding anything.
- On the frontend, inspect the existing `httpClient.ts` (Axios instance) before deciding how the token is attached to requests, and inspect the existing `useAppStore.ts` persistence pattern (`zustand/middleware` `persist`) before deciding how/whether auth state is persisted — follow the same conventions rather than inventing a new one.
- Do not introduce a new state-management library, a new CSS/styling library, a new HTTP client, or a new i18n library. Reuse what Story 01/02 already established.

## Out of scope

- Roles, permissions, or any fine-grained authorization (that is a separate, later story).
- Password reset, email verification, MFA, or SSO/OAuth.
- Customer-facing authentication or the customer portal (separate, later story) — this story is internal agent/user authentication only.
- Audit logging of authentication events (separate, later story).
- Any CRM domain functionality (tickets, customers, agents-as-a-business-entity beyond the login identity).
- CI/CD, Docker, or production deployment/security hardening beyond what's needed for this story to function in development.
