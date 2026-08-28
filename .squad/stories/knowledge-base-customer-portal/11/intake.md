# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/knowledge-base-customer-portal/11/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Knowledge Base & Customer Portal
- **Feature slug (folder under `plans/`):** `knowledge-base-customer-portal`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `11` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Knowledge Base & Customer Portal
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Story 11 of the consolidated 6-story remaining-scope breakdown (Stories 09-14), approved by the
user after a full CRM feature audit. Stories 01-10 are complete and merged into `main`. This story
covers exactly two Core Feature Areas from the original audit: Knowledge Base, and Customer Portal.

Approved scope (10 capabilities, unchanged from the consolidation):
- Knowledge Base: FAQs, Help articles, Solutions, Guides, Knowledge-base search
- Customer Portal: Customer portal (auth), Customer ticket submission, Customer request tracking,
  Customer history, Customer feedback

This is the first story to introduce a SECOND identity model alongside the existing staff
(Admin/Agent) `User`/`Role` model from Stories 03/04. A `Customer` (today a plain CRM record with
no login capability - `Domain/Customer.cs` has no password field) becomes an authenticateable
principal in its own right, distinct from staff.

Five architecture decisions were proposed during pre-implementation analysis and are APPROVED
EXACTLY AS STATED - do not re-litigate or propose alternatives during planning:

1. Customer authentication: extend the existing `Customer` entity with a nullable `PasswordHash`
   column, reusing the existing `PasswordHasher<T>` pattern already used for staff `User` in
   `AuthController.cs`. Do NOT introduce a separate `CustomerAccount` entity unless the plan's own
   inspection proves a shared `Customer` shape is technically impossible (it should not be -
   `Customer` already has `Id`/`FullName`/`Email`/`Phone`/`CreatedAtUtc`; a nullable `PasswordHash`
   is an additive column, matching the "smallest correct shape" precedent from every prior story).
2. JWT/authentication: reuse the existing JWT infrastructure (`JwtTokenService`, same signing key,
   issuer, audience, `[Authorize]` pipeline) with an explicit identity-kind claim distinguishing
   `type=staff` vs `type=customer` on the token (in addition to the existing `sub`/`email`/`name`/
   role claims already issued for staff). Enforce this claim server-side via ASP.NET Core
   authorization policies so a customer token is REJECTED by every existing staff endpoint
   (`TicketsController`, `CustomersController`, `NotificationsController`, `AdminController`,
   the audit-log endpoint, etc.) and a staff token is REJECTED by every new customer-only portal
   endpoint. Do not build a second, parallel ASP.NET Core authentication scheme - a claim-based
   policy on the single existing JWT Bearer scheme is sufficient and matches how staff Roles are
   already just claims checked via policy/`[Authorize(Roles=...)]`.
3. Knowledge Base permissions: both Admin and Agent staff roles may create, edit, and delete KB
   articles - do not restrict authoring to Admin-only (this differs from the Admin-only precedent
   used for user-deletion/ticket-deletion, and instead matches the existing `QuickReplyTemplate`
   precedent from Story 09, which is creatable/editable by any authenticated staff user).
4. Feedback: a customer may submit feedback on a ticket ONLY when that ticket's `Status` is
   exactly `"Closed"`. Submitting feedback on a ticket in any other status must be rejected
   server-side (this is a server-side authorization/validation rule, not merely a UI affordance
   that hides the button).
5. Customer ticket submission: customer authentication is REQUIRED before a ticket can be
   submitted through the portal. Anonymous/unauthenticated ticket submission is explicitly NOT
   supported in this story - every portal-submitted ticket has a real, authenticated `Customer` as
   its owner from the moment it is created.

CRITICAL server-side security requirements (apply throughout the plan and implementation, not
just as an afterthought):
- Customer data must always be server-side scoped to the authenticated customer identity (resolved
  from the JWT `sub`/customer-id claim on the server). NEVER trust a client-supplied `customerId`
  route/query/body parameter for authorization decisions - it may be used to look up the caller's
  OWN record by matching against the server-resolved identity, never as the sole authorization key.
- A customer must never be able to read, modify, assign, close, reassign, or otherwise access
  another customer's tickets, notes, attachments, tasks, or history - every customer-facing query
  must filter by the server-resolved customer id, not a client-supplied one.
- A customer must never be able to reach any staff-only endpoint or see staff-only data (other
  customers' records, the audit log, assignable-users list, notifications belonging to staff, etc.).
- A staff user's token must never be treated as a customer identity by the new customer-only
  endpoints, and vice versa - this must be enforced by the `type=staff`/`type=customer` claim
  check on every relevant endpoint, verified with both directions of cross-token testing.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
Knowledge Base (staff side):
1. An authenticated Admin or Agent can create a KB article (title + body, required) and it
   persists.
2. An authenticated Admin or Agent can edit and delete any KB article.
3. Articles are listable in a staff-side KB management page.
4. Keyword search over article title/body (simple case-insensitive contains match) returns
   matching articles, staff-side and portal-side.

Customer identity & authentication:
5. A customer can register (set a password on their `Customer` record, or be created fresh with
   one) and log in with email + password, receiving a JWT distinct from a staff JWT via a
   `type=customer` claim.
6. A customer's portal session persists across a page reload (same pattern as staff: token stored
   client-side, restored on load, cleared on logout/expiry).
7. Logging out of the portal clears the customer session; a reload after logout returns to the
   portal login screen.

Customer ticket submission & tracking:
8. An authenticated customer can submit a new ticket; it is created with that customer as its
   owner (server-resolved from the token, never from a client-supplied customerId).
9. An unauthenticated request to submit a ticket via the portal is rejected (401) - no anonymous
   submission path exists.
10. An authenticated customer can list and view only their OWN tickets (list, detail, and
    read-only history/notes timeline) - tickets belonging to other customers are not visible,
    not enumerable, and return 404 (not 403) if a customer tries a direct id they don't own,
    consistent with this codebase's existing "don't reveal existence" convention (see
    NotificationsController's 404-not-403 pattern from Story 10).

Feedback:
11. A customer can submit feedback (rating + optional comment) on one of their own tickets only
    when that ticket's Status is exactly "Closed".
12. Submitting feedback on a ticket that is not Closed is rejected server-side with a clear error,
    regardless of what the UI does or doesn't show.
13. A customer cannot submit feedback on a ticket they do not own.

Cross-boundary security:
14. A valid customer JWT is rejected (401/403) by every existing staff-only endpoint (Tickets,
    Customers, Notifications, Admin/audit-log, assignable-users, etc.).
15. A valid staff JWT is rejected by every new customer-only portal endpoint (submit ticket via
    portal, my-requests list/detail, feedback submission, portal login/me).
16. KB article CREATE/EDIT/DELETE is rejected for an unauthenticated caller and for a valid
    customer token; only a valid staff (Admin or Agent) token succeeds. KB article LIST/search may
    be reachable by both staff and authenticated customers (read-only for customers).

Platform/quality (must hold across everything above):
17. All new customer-portal and KB pages render correctly in English and Arabic (RTL), matching
    existing i18n/RTL conventions.
18. All new pages are usable on mobile viewport widths, matching existing responsive conventions.
19. Backend and frontend build cleanly (0 warnings/errors) and frontend lint passes.
20. No regression in Stories 01-10 (staff auth/RBAC, audit logging, Customer/Ticket CRUD,
    notes/attachments/tasks/quick-replies, SLA/escalation/notifications, App Shell nav).
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Stories 01-10 (all completed and merged into `main`). Reuses Story 03/04's JWT/RBAC pattern (extended with a customer-identity claim), Story 05's `AuditLogger`/`AuditLog`, Story 07/08's `Ticket` entity/shape as-is, Story 09's `TicketNote`/history read pattern and `QuickReplyTemplate` staff-CRUD-page precedent, Story 10's `TicketsController` SLA/escalation fields (a portal-submitted ticket is a normal `Ticket` row and flows through Story 10's logic automatically, unmodified).
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Customer.cs` (add nullable `PasswordHash`), `Auth/JwtTokenService.cs` (generalize to issue a token for either identity kind, adding the `type` claim), `Auth/AuthController.cs` (new customer login/register actions, or a new sibling controller), `Program.cs` (new authorization policies for staff-only vs customer-only), `Data/AppDbContext.cs` (new `KnowledgeArticle`/`Feedback` entities + `Customer.PasswordHash` column config), new `Controllers/KnowledgeArticlesController.cs` + DTOs, new `Controllers/PortalController.cs` (or similarly named) + DTOs for customer ticket submission/my-requests/feedback; `frontend/src/store/useAuthStore.ts` (reused as-is for staff) plus a new `useCustomerAuthStore.ts`, `frontend/src/routes/AppRouter.tsx` (new portal route subtree), new `frontend/src/routes/RequirePortalAuth.tsx`, new `frontend/src/components/layout/PortalShell.tsx`, new portal pages under `frontend/src/pages/portal/`, new staff `frontend/src/pages/KnowledgeBasePage.tsx`, `frontend/src/i18n/locales/en.json`/`ar.json`.

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, same as prior stories.
- Do not modify or recreate Story 01-10 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- This story was pre-scoped in detail across a prior analysis pass (a 12-point Story 11 pre-implementation analysis) that the user has already reviewed and approved, including the five numbered architecture decisions embedded in the Description above. Those decisions are final, not open questions for the plan to re-litigate. The plan should focus on translating them into concrete tasks, file-by-file, in the same style as Stories 09/10's plans.
- Keep every new entity/endpoint the smallest shape that satisfies its acceptance criterion. This story introduces a second identity model - the plan must be explicit and exhaustive about every authorization boundary (which endpoints accept `type=staff`, which accept `type=customer`, which reject the other), since this is the single highest-risk aspect of the story.
- Follow the existing `GetActorUserId()` / `User.FindFirst("sub")` pattern already used in `TicketsController.cs`/`NotificationsController.cs` for resolving the caller's identity server-side; the customer-equivalent should resolve the caller's `Customer.Id` the same way, from the JWT, never from a request parameter.
- Follow the existing 404-not-403 pattern (established in `NotificationsController.cs`, Story 10) when a customer attempts to access another customer's ticket by id - do not reveal existence via a 403.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize]` / `[Authorize(Roles = "Admin")]`.
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand (`persist` + `createJSONStorage(localStorage)` + `partialize`, with a `version`/`migrate` pair per Story 09's precedent if the customer store's shape ever needs to change later), `react-i18next`, Axios (`frontend/src/api/httpClient.ts`).
- `JwtTokenService.IssueToken` is currently hard-typed to the staff `User` domain type — the plan should specify exactly how it's generalized (e.g. an overload, or a signature taking `(Guid id, string email, string displayName, string type, IEnumerable<string> roles)`), not a parallel duplicate service.
- ASP.NET Core authorization policies (`AddAuthorization(options => options.AddPolicy(...))` in `Program.cs`) are the correct mechanism for the staff-only vs customer-only split — e.g. a `RequireStaff` policy checking a `type` claim equals `staff`, and a `RequireCustomer` policy checking it equals `customer`; apply via `[Authorize(Policy = "RequireCustomer")]` etc., mirroring how `[Authorize(Roles = "Admin")]` is already used.
- KB search should be a simple case-insensitive `Contains` query (EF Core translates `string.Contains` to `LIKE`), matching this codebase's precedent of no new search infrastructure (see Story 06/07 intakes' explicit deferral of search).
- Reuse `PasswordHasher<T>` for `Customer` the same way it's used for `User` today in `AuthController.cs`/`Program.cs`'s DI registration (may need a second `PasswordHasher<Customer>` registration, or a generic helper — plan should specify).
- Reuse `CustomersPage.tsx`/`TicketsPage.tsx`'s existing client-side validation pattern for new portal forms (submit-ticket, feedback, register/login).
- Reuse `QuickRepliesPage.tsx`'s CRUD-page structure as the template for the staff-side KB management page.

## Out of scope

- SLA/automation, alerts, notifications, automatic assignment, escalation rules (Story 10 — already done; a portal-submitted ticket flows through this unmodified).
- Communication channels (email, WhatsApp, SMS, live chat, web forms) or external/ERP integration (Story 12).
- Reports, SLA/agent-performance metrics, management dashboards, customer-satisfaction *reporting/aggregation* (Story 13) — this story only *captures* a feedback value, it does not report/aggregate on it.
- AI features of any kind (ticket summaries, suggested replies/solutions, chatbot), and Platform Administration items — Users/Roles management UI, runtime system configuration, multi-department, multi-branch, custom branding (Story 14).
- Anonymous/unauthenticated ticket submission of any kind (per Decision 5).
- A separate `CustomerAccount` entity (per Decision 1, unless the plan proves it's technically necessary).
- A second, fully independent ASP.NET Core authentication scheme (per Decision 2 — one JWT scheme, claim-based policies).
- Password reset / forgot-password flows for either staff or customers (not previously in scope for staff either).
- KB article categories/tags as a separate taxonomy entity, article attachments/images, article versioning, or article view-count/analytics.
- Customer profile self-editing (name/email/phone) from the portal — the portal covers ticket submission/tracking/feedback and KB browsing only, not account management beyond login/register.
- General search/filter/sort/pagination across Customers or Tickets on the staff side (a separate, not-yet-scheduled concern; only KB search is in scope here).
- Any change to Story 09/10's existing staff-side Ticket create/update/delete behavior, RBAC, or SLA/escalation logic, beyond what's needed for a portal-submitted ticket to be a normal `Ticket` row.
