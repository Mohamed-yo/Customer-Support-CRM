# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/communication-channels-integrations/12/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Communication Channels & Integrations
- **Feature slug (folder under `plans/`):** `communication-channels-integrations`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `12` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Communication Channels & Integrations
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Story 12 of the consolidated 6-story remaining-scope breakdown (Stories 09-14), approved by the
user after a full CRM feature audit. Stories 01-11 are complete and merged into `main`. This story
covers exactly two Core Feature Areas from the original audit: Communication Channels, and
Integrations (their overlapping Email/WhatsApp/SMS items were merged into one, per the approved
consolidation - not duplicated).

NOTE: this intake is a faithful RECREATION. The original Story 12 intake, plan, and a partial
implementation were produced once already, but the entire local working directory was accidentally
deleted before that work was pushed anywhere. The repository was restored from GitHub at commit
6d327ad (PR #11, Story 11 merged) - Stories 01-11 are fully intact; nothing from the lost Story 12
attempt survived, and none of it should be assumed to exist in the codebase. This recreation must
be planned purely from the current, actually-restored code - not from memory of the lost attempt.

Approved scope (8 capabilities):
- Communication Channels: Web forms, Email, WhatsApp, SMS, Live chat
- Integrations: ERP, External systems, Integration APIs/webhooks

CRITICAL, VERIFIED FACT (not assumed): this codebase has ZERO existing external-service
groundwork. Confirmed by direct inspection of the restored repository before this intake was
written: no SMTP config, no third-party API keys of any kind in
`appsettings.json`/`appsettings.Development.json.example`; no SignalR/WebSocket code anywhere; no
email/SMS/WhatsApp/ERP library reference anywhere in `backend/` or `frontend/`; `Ticket.cs` has no
"source"/"channel" field at all. This makes Story 12 categorically different from Stories 09-11,
which were entirely self-contained within this app's own DB/API - most of this story's
capabilities depend on a real third-party account (a live SMTP relay, a WhatsApp Business API
token, an SMS provider account, a named ERP endpoint) that does not exist in this environment and
must not be fabricated.

Six architecture decisions were approved during pre-implementation analysis and are FINAL - do not
re-litigate or propose alternatives during planning:

1. Email / WhatsApp / SMS: build the COMPLETE provider-agnostic code path for each - inbound
   webhook handling, an outbound sender abstraction, ticket-intake wiring, configuration slots, and
   error handling (a missing/misconfigured provider must fail gracefully with a clear result, never
   an unhandled exception). Do NOT claim live provider connectivity has been verified - it hasn't,
   and can't be, without real credentials. Verify these three using synthetic webhook payloads and
   local/dev tooling (e.g. a throwaway local SMTP listener for the outbound email check). The
   architecture must allow real provider credentials to be configured later (via
   `appsettings.Development.json`, following this project's existing secrets pattern where
   `appsettings.json` ships blank placeholders) without any redesign.
2. ERP / External Systems: implement ONE generic, provider-agnostic outbound webhook mechanism
   (configurable target URL + event type, e.g. "ticket.created" / "ticket.closed"). Do NOT assume
   or implement any specific ERP vendor/API - none has been named. This generic mechanism satisfies
   both "ERP integration" and "External system integrations" - it is not duplicated.
3. Live Chat: approved to use SignalR (server: built into the ASP.NET Core shared framework, no new
   NuGet package; client: the `@microsoft/signalr` npm package - the one new frontend dependency
   this story adds). Do not introduce any additional realtime infrastructure beyond what live chat
   itself requires (no generic pub/sub system, no other hub).
4. Email: use the `MailKit` NuGet package for SMTP sending, not the deprecated/discouraged
   `System.Net.Mail.SmtpClient`. This is the one new backend dependency this story adds.
5. Inbound webhook authentication: use a shared-secret header (e.g. `X-Webhook-Secret`, one
   configurable value per channel) for this story. Structure the check behind a small, swappable
   abstraction so a later story can add real provider-specific signature verification (e.g.
   SendGrid's or Twilio's actual signing scheme) without redesigning the channel pipeline. Every
   inbound webhook endpoint MUST perform this check - none may be left completely open.
6. `Ticket.Source` allowed values, exactly: `Manual`, `Portal`, `WebForm`, `Email`, `WhatsApp`,
   `SMS`, `Chat`. `Manual` = created by a staff agent (existing `TicketsController.Create`).
   `Portal` = created by an authenticated customer (existing Story 11 `PortalController`).
   The other five are new, set by this story's channel endpoints.

Additional constraints carried over from the user's approval, apply throughout:
- Keep Story 12 strictly within the approved scope; do not modify Stories 01-11 beyond what is
  minimally, directly required.
- Never fabricate external credentials, provider responses, ERP APIs, or claim a successful live
  integration that wasn't actually exercised against a real provider.
- Every report/summary produced at the end of this story must explicitly separate: (a) fully
  end-to-end verified functionality, (b) synthetic/local-only verified functionality, (c)
  functionality that requires real external credentials and could not be live-tested.
- Preserve all existing RBAC/authorization boundaries and the Staff/Customer identity separation
  from Story 11 (a customer token must still never reach a staff-only endpoint and vice versa).
- Preserve Arabic/English, RTL, and mobile responsiveness conventions.
- All externally-supplied data (web form input, webhook payloads) must be validated server-side -
  never trusted as-is, regardless of the shared-secret check having passed.
- Existing ticket creation, SLA/escalation (Story 10), notifications (Story 10), customer portal,
  and knowledge base (Story 11) behavior must continue working unmodified.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
Ticket.Source (foundation):
1. Every ticket has a Source column with exactly the 7 allowed values from Decision 6. Existing
   tickets (created before this migration) backfill to "Manual".
2. A staff-created ticket (TicketsController.Create) has Source="Manual". A portal-submitted
   ticket (Story 11 PortalController.SubmitTicket) has Source="Portal". Neither existing endpoint's
   other behavior changes.

Web form (fully end-to-end capable, no external dependency):
3. An unauthenticated visitor can submit the public web form (fullName, email, phone optional,
   subject, description optional, priority optional) and it creates a ticket with Source="WebForm".
4. Submitting with an email matching an existing Customer record re-uses that Customer (does not
   create a duplicate); submitting with a new email creates a new Customer (no password set - this
   is NOT the same as Story 11's portal registration/login).
5. The created ticket goes through the same auto-assignment and SLA computation as any other ticket
   (Story 10, unmodified).

Email / WhatsApp / SMS (code path complete; live-provider connectivity NOT verified - synthetic
payloads and local tooling only):
6. A synthetic inbound webhook payload (shaped like a generic provider "inbound message" payload:
   from-address, subject/body, optional ticketId for threading) with a valid shared-secret header
   creates a ticket with the matching Source when no ticketId is given, or appends a
   ChannelMessage(Direction=Inbound) to the referenced ticket when a valid ticketId is given.
7. The same request without the shared-secret header, or with a wrong one, is rejected (401) and
   creates nothing.
8. Malformed/incomplete synthetic payloads are rejected server-side with a clear 400, not a 500.
9. An outbound reply on a channel-sourced ticket (Email/WhatsApp/SMS) persists a
   ChannelMessage(Direction=Outbound) and invokes that channel's sender abstraction. For Email,
   sending via a locally-configured SMTP target is verified end-to-end (a local/dev SMTP listener
   receiving the message counts as verified; a real provider is not required for this). For
   WhatsApp/SMS, the sender abstraction is invoked and its result (success/failure/not-configured)
   is surfaced - actual delivery cannot be verified without real provider credentials, and the
   report must say so explicitly.
10. If a channel's sender is not configured (e.g. no SMTP host set), the outbound attempt fails
    gracefully with a clear "not configured" result - never an unhandled exception.

Live Chat (fully end-to-end capable, no external dependency):
11. An authenticated staff user and the ticket's own authenticated customer can both connect to the
    same ticket's chat channel and exchange messages in real time (message sent by one appears for
    the other without a page reload).
12. Chat messages persist (a page reload replays prior history via a REST history endpoint, then
    SignalR carries new messages live).
13. A customer cannot join or read the chat channel for a ticket they do not own (same ownership
    check pattern as Story 11's `GetMyRequest`/`SubmitFeedback`).
14. A staff (non-owning-customer) token cannot authenticate as a customer on the chat hub, and vice
    versa - the Story 11 identity separation holds on the realtime channel too.

Generic outbound webhook (ERP / External systems):
15. A staff Admin can configure an outbound webhook subscription (target URL + event type).
16. When a matching event fires (ticket created, ticket closed), an HTTP POST fires to every active
    subscription's target URL with a JSON payload describing the event; verified via a synthetic
    local HTTP listener capturing the actual dispatched request/payload.
17. A misconfigured or unreachable target URL fails gracefully (logged, does not throw into the
    request that triggered the event, does not roll back the ticket mutation that triggered it).

Cross-cutting / regression:
18. Every inbound webhook endpoint (Email/WhatsApp/SMS) enforces the shared-secret check; none are
    reachable without it.
19. Existing staff-only and customer-only RBAC boundaries (Story 11) are unaffected - a customer
    token still gets 403 on every staff endpoint and vice versa; this story's new endpoints are
    correctly scoped ([AllowAnonymous] for web form/inbound webhooks with their own auth, RequireStaff
    for webhook-subscription config, RequireStaff/RequireCustomer for chat per side).
20. All new UI (web form page, chat widget, staff webhook-config page) renders correctly in English
    and Arabic (RTL) and on mobile viewport widths.
21. Backend and frontend build cleanly (0 warnings/errors); frontend lint passes.
22. No regression in Stories 01-11 (staff auth/RBAC, audit logging, Customer/Ticket CRUD,
    notes/attachments/tasks/quick-replies, SLA/escalation/notifications, customer portal, knowledge
    base, App Shell nav).
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Stories 01-11 (all completed and merged into `main`, restored from GitHub at commit `6d327ad`). Reuses Story 03/04's JWT/RBAC pattern, Story 05's `AuditLogger`/`AuditLog`, Story 07/08's `Ticket` entity/creation flow, Story 09's internal-static-helper-reuse precedent (`TicketsController`'s `PickLeastLoadedAssigneeAsync`/`ComputeDueDates`/`CreateAssignedNotificationAsync`, already `internal static` for Story 11's `PortalController` to call - this story's Web Form/channel endpoints reuse the same helpers, not new copies), Story 10's SLA/escalation logic (unmodified, applies automatically to any new `Ticket` row regardless of `Source`), Story 11's customer-find-by-email pattern (as design inspiration only - channel intake creates/finds a `Customer` WITHOUT setting a password, unlike Story 11's portal registration "upgrade" path) and its Staff/Customer JWT claim separation (`type=staff` / `type=customer` policies), which the Live Chat hub and all new endpoints must respect.
- **Depends on code areas or other stories:** `backend/src/CustomerSupportCrm.Api/Domain/Ticket.cs` (add `Source`), `Controllers/TicketsController.cs` (set `Source="Manual"` on create; expose any additional internal-static helper needed by the new channel controller), `Controllers/PortalController.cs` (set `Source="Portal"` on create), `Data/AppDbContext.cs` (new entities' fluent config), `Program.cs` (MailKit-based sender DI registration, SignalR registration + hub mapping, new configuration sections), new `Domain/ChannelMessage.cs`, `Domain/ChatMessage.cs`, `Domain/OutboundWebhookSubscription.cs`, new `Integrations/` folder for sender abstractions and the webhook dispatcher, new `Controllers/ChannelsController.cs` + DTOs, new `Controllers/WebhookSubscriptionsController.cs` + DTOs, new `Hubs/ChatHub.cs`, new migration; `frontend/src/routes/AppRouter.tsx` (new public web-form route, new staff webhook-config route), new `frontend/src/pages/WebFormPage.tsx`, new `frontend/src/pages/WebhooksPage.tsx`, new `frontend/src/components/ChatWidget.tsx` (mounted in both `TicketDetailPage.tsx` and Story 11's `MyRequestDetailPage.tsx`), new `frontend/src/api/channels.ts`/`chat.ts`/`webhooks.ts`, `frontend/src/components/layout/AppShell.tsx` (new nav entry, first role-conditional nav item), `frontend/src/i18n/locales/en.json`/`ar.json`.

## Extra notes (optional)

- Do not create an implementation branch as part of planning — branch creation happens later, at implementation time, and only after the user has approved this plan.
- Do not modify or recreate Story 01-11 files, and do not modify their plans under `.squad/plans/`.
- Do not create a new overall project plan.
- This is a RECREATION of a Story 12 intake/plan that was produced once before but lost (along with a partial implementation) when the local working directory was accidentally deleted prior to being pushed. The repository has since been restored from GitHub - Stories 01-11 are fully intact at commit `6d327ad`; nothing from the lost Story 12 attempt exists in the codebase. Plan strictly from the current, actually-restored code - do not assume any Story 12 file, entity, or endpoint already exists.
- This story was pre-scoped in detail across a prior analysis pass (a 13-point Story 12 pre-implementation analysis) that the user has already reviewed and approved, including the six numbered architecture decisions embedded in the Description above. Those decisions are final, not open questions for the plan to re-litigate.
- Keep every new entity/endpoint the smallest shape that satisfies its acceptance criterion. This story adds real external-integration surface area for the first time - the plan must be explicit about which parts are genuinely end-to-end testable in this environment (Web Form, Live Chat, the generic outbound webhook) versus which are code-complete-but-unverifiable-without-real-credentials (Email/WhatsApp/SMS provider connectivity), and must not blur that distinction anywhere (in code comments, in the plan's own verification section, or in the eventual implementation report).
- Follow the existing `GetActorUserId()` / `GetActorCustomerId()` pattern (`TicketsController.cs`, `PortalController.cs`) for resolving the caller's identity on any new authenticated endpoint (webhook-subscription CRUD, chat hub).
- Follow the existing 404-not-403 pattern (`NotificationsController.cs`, Story 10; `PortalController.cs`, Story 11) for a customer attempting to reach another customer's ticket's chat/history.

## Technical hints (optional)

- **Repository root:** `.` — **Backend root:** `backend/` — **Frontend root:** `frontend/`.
- **Backend stack (already established, do not change):** ASP.NET Core Web API, .NET 8, C#, EF Core 8, SQL Server, JWT bearer authentication, `[Authorize(Policy = "RequireStaff"/"RequireCustomer")]` (Story 11).
- **Frontend stack (already established, do not change):** React 18 + TypeScript + Vite, Tailwind CSS v3 (logical `start-`/`end-`/`ps-`/`pe-` classes), React Router v6, Zustand, `react-i18next`, Axios (`frontend/src/api/httpClient.ts` for staff, `portalHttpClient` for customers - Story 11).
- New backend dependency: `MailKit` NuGet package (Decision 4) - for SMTP sending only; do not add `MimeKit`-adjacent extras beyond what `MailKit` itself requires.
- New frontend dependency: `@microsoft/signalr` npm package (Decision 3) - the SignalR JS client; no other realtime library.
- `Ticket.Source` should follow the exact same "simple static string-array constant + contains-check" style already used for `AllowedStatuses`/`AllowedCategories`/`AllowedPriorities` in `TicketsController.cs` - not a new enum type or configuration abstraction.
- Inbound webhook shared-secret check (Decision 5): read one configured secret per channel (e.g. `Channels:Email:InboundSecret`) from `IConfiguration`, compare against an `X-Webhook-Secret` request header using a constant-time comparison (`CryptographicOperations.FixedTimeEquals` or equivalent) to avoid a timing side-channel; structure this as a small reusable helper/interface so a future story can swap in real per-provider signature verification without touching the surrounding controller logic.
- MailKit SMTP sender: read `Smtp:Host`/`Smtp:Port`/`Smtp:Username`/`Smtp:Password`/`Smtp:From` from configuration (blank in the committed `appsettings.json`, following the existing `Jwt:SigningKey`/connection-string pattern); if `Smtp:Host` is blank, the sender must return a "not configured" failure result rather than attempting a connection.
- SMS/WhatsApp outbound: since no specific provider is named (Decision 1), implement a single generic HTTP-POST-to-a-configured-endpoint sender shape for both (differing only by configuration section name), not a vendor-specific SDK/client.
- Generic outbound webhook dispatch point: call it from the same place `Source` is set on ticket create (`TicketsController.Create`, `PortalController.SubmitTicket`, the new `ChannelsController` endpoints) and on the existing Closed-status transition in `TicketsController.Update` - a single shared dispatcher, not duplicated per-controller logic.
- Live Chat: `ChatMessage.SenderType` ("Staff"/"Customer") + `SenderId` (`Guid`, no FK - mirrors the existing `AuditLog.ActorUserId` "generic actor id, no FK constraint" precedent from Story 10/11) rather than two nullable FK columns.
- SignalR + JWT bearer: use the standard `accessTokenFactory` pattern on the client and configure the JWT bearer handler's `OnMessageReceived` event server-side to also accept the token from the query string for hub requests (SignalR's WebSocket transport cannot always set a custom Authorization header) - this is additive to the existing JWT Bearer configuration in `Program.cs` (verified: no `Events` are configured on it yet), not a replacement of it.
- Reuse `PageContainer`/`LanguageSwitcher` (Story 02/03 precedent, already reused for Story 11's portal login/register) for the public Web Form page, since it is unauthenticated and outside any shell.
- Reuse `QuickRepliesPage.tsx`/`KnowledgeBasePage.tsx`'s (Story 09/11) staff CRUD-page structure for the webhook-subscription management page.
- AppShell's `NAV_ITEMS` is currently a flat array shown to every staff user regardless of role - this story's Admin-only "Webhooks" nav entry is the first role-conditional item; keep the filter minimal (e.g. a per-item optional `adminOnly` flag checked against the existing `useAuthStore().hasRole('Admin')`), not a new permissions abstraction.

## Out of scope

- A specific ERP vendor integration (SAP/Zoho/Odoo/etc.) - Decision 2 explicitly calls for the generic webhook mechanism only.
- Real, live-verified provider connectivity for Email/WhatsApp/SMS - Decision 1 explicitly limits this story to code-complete + synthetic/local verification.
- Provider-specific inbound signature verification (SendGrid/Twilio/Meta's actual signing schemes) - Decision 5 explicitly limits this story to a shared-secret header, structured to allow that later.
- Inbound/outbound threading beyond a simple optional `ticketId` correlation field in the synthetic payload shape - no email-header threading (In-Reply-To/References), no provider-side conversation-id mapping beyond storing an optional `ExternalMessageId` on `ChannelMessage` for future use.
- File/media attachments over any channel (email attachments, WhatsApp media, MMS) - text/body content only, this story.
- Any change to Story 09/10's existing staff-side Ticket create/update/delete behavior, RBAC, or SLA/escalation logic, beyond setting `Source` and calling the new webhook dispatcher at the same mutation points.
- Any change to Story 11's Knowledge Base, customer portal ticket submission/tracking/feedback, or customer authentication behavior.
- Reports, dashboards, AI features, multi-department/branch/branding, or any other Story 13/14 scope.
- A UI for editing/retrying failed outbound webhook deliveries, or a delivery-log viewer - only configuring subscriptions and firing them is in scope; a failure is logged, not queued/retried.
- Rate limiting or abuse-prevention on the public web form beyond standard server-side input validation.
