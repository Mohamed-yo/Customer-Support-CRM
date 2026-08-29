# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/public-api-webhook-integration-framework/24/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Public API & Webhook Integration Framework
- **Feature slug (folder under `plans/`):** `public-api-webhook-integration-framework`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `24` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Public API & Webhook Integration Framework
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
This story recreates a scope that was defined once during this project's early planning
discussions, then set aside (never given its own intake/plan/implementation) while the
project's stories were later renumbered and consolidated into the 6-story plan that
produced Stories 09-14. This intake restates that original scope verbatim as the source
of truth, per explicit user direction, and grounds it in the current, actual state of the
codebase (verified by direct inspection immediately before writing this intake - no
invention).

Original scope (verbatim intent, restated): "Public API & Webhook Integration Framework" -
a merge of two originally-separate concepts:
  1. "Public API & Integration Framework" - API key issuance, external client
     authentication, rate limiting, API documentation.
  2. "External Systems / Generic Webhook Connectors" - a generic webhook/connector
     mechanism for external systems.
ERP integration was explicitly kept separate from this merged scope in the original
planning discussion ("ERP stays separate... bespoke integration with its own
data-mapping/business rules") - ERP integration is out of scope here, same as it always
was.

CRITICAL, VERIFIED FACT (re-confirmed by direct code inspection before writing this
intake): concept #2 above (generic webhook/connector mechanism) was ALREADY BUILT by
Story 12 ("Communication Channels & Integrations", merged) -
`OutboundWebhookSubscription`/`OutboundWebhookDispatcher`/`WebhookSubscriptionsController`
already provide Admin-configured, event-type-triggered outbound webhook delivery
(`ticket.created`/`ticket.closed`) to admin-configured target URLs. This story does NOT
rebuild that - it is reused as-is.

What does NOT exist yet anywhere in this codebase (re-confirmed by direct code
inspection): concept #1 above, in full. Specifically:
- No API-key entity, issuance, storage, or revocation mechanism.
- No authentication scheme other than the existing JWT bearer (`Program.cs`'s
  `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`, backing
  only the existing `RequireStaff`/`RequireCustomer` claim-based policies). No concept of
  a third identity kind ("external client") exists.
- No rate-limiting middleware anywhere (grep-confirmed zero matches for "RateLimit" in the
  entire backend). The project targets `net8.0`, which ships
  `Microsoft.AspNetCore.RateLimiting` in the shared framework - no new NuGet package is
  needed to add it.
- No API documentation beyond a bare, Development-only `AddSwaggerGen()`/`UseSwaggerUI()`
  (`Program.cs`) with no security-scheme definition (an external client's API key is not
  currently representable/testable in Swagger UI at all).
- The existing outbound webhook dispatcher itself has no signing/shared-secret mechanism
  on outbound calls (`OutboundWebhookDispatcher.DispatchAsync` does a plain
  `PostAsJsonAsync` with no signature header) - this is Story 12's existing, already-
  accepted behavior and is unchanged by this story; noted here only for completeness.

This story's real, net-new scope is therefore: build the API-key issuance/authentication/
rate-limiting/documentation mechanism (concept #1), as a new, additive, third
authentication scheme alongside (not replacing) the existing staff/customer JWT scheme,
and apply it to a minimal, clearly-scoped demonstration endpoint that proves the full
mechanism end-to-end - matching the deliberately generic original acceptance criterion
below ("an external client authenticates with an API key and calls a documented
endpoint"), without inventing which specific existing business/ticket data an external
client may read. Exposing specific existing business data (e.g. Tickets, Reports) to
external clients is a separate, later decision requiring its own data-scoping/authorization
design (e.g. which tickets, whose data, is any tenancy model needed) that this story does
not attempt to invent, since none of that is specified anywhere in the original scope or
present in this codebase today (no multi-tenancy exists; that remains Story 14 scope,
already flagged as not-yet-started).
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
1. Admin-only API key management: an Admin can issue a new API key (given a label/name),
   see the plaintext secret exactly once at creation time (never retrievable again,
   matching standard API-key UX and this codebase's own "never store/expose a plaintext
   secret after creation" precedent), list existing keys (label, creation date, last-used
   date, active/revoked status - never the secret itself again), and revoke a key.
   Mirrors `WebhookSubscriptionsController`'s exact RBAC shape:
   `[Authorize(Policy = "RequireStaff", Roles = "Admin")]`.
2. API keys are stored hashed (never plaintext at rest), matching this codebase's existing
   `PasswordHasher<T>` precedent for credential storage.
3. A new, additive authentication scheme validates a per-request API key (via a header,
   e.g. `X-Api-Key`) against the stored hash, independent of and non-disruptive to the
   existing JWT bearer scheme - every existing staff/customer-authenticated endpoint is
   completely unaffected.
4. A new authorization policy (e.g. `RequireExternalClient`) is satisfied only by a
   successfully-authenticated API-key request, mirroring the existing
   `RequireStaff`/`RequireCustomer` claim-based policy pattern exactly.
5. A revoked or unknown API key is rejected with 401; a valid key correctly reaches the
   policy-protected endpoint.
6. Rate limiting is applied to API-key-authenticated endpoints, partitioned per API key
   (one client's usage cannot exhaust another's allowance), using .NET 8's built-in
   `Microsoft.AspNetCore.RateLimiting` middleware - no new NuGet package.
7. A request exceeding its rate limit receives `429 Too Many Requests`.
8. At least one concrete, documented, read-only endpoint is protected by the new scheme +
   policy + rate limiter, end-to-end proving the mechanism (per this story's own
   description: it does not invent a new business-data-sharing scope).
9. API documentation: the existing Swagger setup gains a security-scheme definition for
   the API key header so it can be exercised directly from Swagger UI; still
   Development-only, matching the existing setup - making Swagger available in
   Production is explicitly out of scope (see below).
10. The existing outbound webhook mechanism (Story 12) is unchanged and reused as-is; this
    story does not modify `OutboundWebhookSubscription`, `OutboundWebhookDispatcher`, or
    `WebhookSubscriptionsController`.
11. A frontend "API Keys" Admin-only management page exists, mirroring `WebhooksPage.tsx`
    exactly (nav entry with `adminOnly: true`, same loading/error/table/modal-form
    conventions), reachable only via the existing `adminOnly` nav-gating mechanism (no new
    route-level Admin abstraction, consistent with the established, explicitly-approved
    precedent from Story 12/13's reviews).
12. i18n: all new UI strings added to both `en.json` and `ar.json` with full key parity;
    RTL-safe layout (logical Tailwind classes only), consistent with every prior story.
13. Stories 01-13 and all previously merged functionality are unmodified except for the
    minimum additive touch-points genuinely required (new Program.cs registrations, a new
    migration, new files) - no existing endpoint's current behavior or auth requirement
    changes.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

None.

---

## Dependencies

- **Blocked by / related ids:** Depends on Stories 03 (JWT auth foundation - the new
  scheme is added alongside it, not a replacement) and 04 (RBAC - Admin-only key
  management reuses the existing Role/policy mechanism). Reuses Story 12's outbound
  webhook mechanism as-is (no changes). All prerequisite stories are merged on `main`.
- **Depends on code areas or other stories:**
  - `backend/src/CustomerSupportCrm.Api/Program.cs` (new scheme/policy/rate-limiter
    registration, additive only)
  - `backend/src/CustomerSupportCrm.Api/Auth/JwtTokenService.cs`,
    `PasswordHasher<T>` (Program.cs) - referenced as the existing credential-hashing
    precedent; API keys are NOT JWTs, so `JwtTokenService` itself is not reused/modified,
    only its neighboring conventions
  - `backend/src/CustomerSupportCrm.Api/Controllers/WebhookSubscriptionsController.cs` -
    RBAC/`[FromServices]` skeleton to mirror for the new API-key management controller
  - `backend/src/CustomerSupportCrm.Api/Data/AppDbContext.cs` (new `DbSet`, new migration)
  - `frontend/src/components/layout/AppShell.tsx`, `frontend/src/routes/AppRouter.tsx`
    (new nav item + route, `adminOnly: true`, same shape as `/webhooks`/`/reports`)
  - `frontend/src/i18n/locales/en.json`, `ar.json` (new namespace)

## Extra notes (optional)

- This is a recreated/restated historical scope, not a fresh proposal - the two
  sub-concepts it originally merged (Public API framework + generic webhook connectors)
  are treated as fixed; only the second one turned out to already be built (Story 12), so
  this story's actual net-new work is narrower than the original combined title implies.
- The specific "which business data can an external client read" question is explicitly
  left open beyond one minimal demonstration endpoint - see Description and Out of scope.

## Technical hints (optional)

- Repo root: `.` (backend: `backend/src/CustomerSupportCrm.Api`; frontend:
  `frontend/src`). Backend language: C# / .NET 8 / EF Core 8. Frontend language:
  TypeScript / React.
- Rate limiting: `Microsoft.AspNetCore.RateLimiting` (built into the `net8.0` shared
  framework already referenced by this project - no new package reference).
- API-key storage: hash the key the same way passwords are hashed elsewhere in this
  codebase (`PasswordHasher<T>`, `Program.cs`) - do not invent a different hashing
  scheme without reason.
- DI: every existing controller in this codebase has no declared constructor; every
  dependency is injected as an action-method parameter via `[FromServices]`. The new
  API-key management controller must follow the same convention. A custom
  `AuthenticationHandler` (framework component, like SignalR's `ChatHub`) legitimately
  uses constructor injection - not a violation of the per-action convention.
- Entity conventions: `Guid Id` (client-defaulted `= Guid.NewGuid()`), `CreatedAtUtc`
  (client-defaulted `= DateTime.UtcNow`), a bare `Guid CreatedByUserId` with no FK
  (mirrors `AuditLog`/`OutboundWebhookSubscription`'s "generic actor id" precedent).

## Out of scope

- What this story explicitly does **not** cover:
  - ERP integration (explicitly kept separate in the original planning discussion this
    story's scope comes from, and already out of scope in every later consolidation).
  - Rebuilding or modifying the existing outbound webhook mechanism (Story 12) - reused
    as-is.
  - Exposing any specific existing business/customer/ticket data set to external clients
    beyond one minimal read-only demonstration endpoint - a real data-sharing scope
    (which tickets, whose data, any tenancy/scoping model) is a separate future decision.
  - Making Swagger/API docs available outside Development (a Production-hosting/ops
    decision, not specified anywhere in this story's source scope).
  - Any multi-tenancy, multi-department, or multi-branch concept (Story 14 scope,
    unimplemented).
  - A "Manager" role or any new staff-facing RBAC tier - API key management stays
    Admin-only, consistent with every other Admin-only feature added since Story 12.
  - Introducing any new third-party NuGet package or frontend dependency.
