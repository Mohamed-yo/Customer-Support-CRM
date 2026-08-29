# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/crm-completion-ai-platform-administration/15/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** CRM Completion, AI Features & Platform Administration
- **Feature slug (folder under `plans/`):** `crm-completion-ai-platform-administration`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `15` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CRM Completion, AI Features & Platform Administration
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
# 1. Title

CRM Completion, AI Features & Platform Administration

# 2. User Story / Business Goal

As the business owner of this CRM, I need the product to reach its originally agreed
functional target state: full AI-assisted agent tooling, complete administrative control
over staff/roles/audit/configuration, a genuinely collaborative agent workspace, and
trustworthy external communication integrations - so that the CRM is not just
feature-rich in individual areas but operable, administrable, and intelligent as a whole
product, matching what was promised for the platform from the start.

# 3. Business Context

This story consolidates two things that must be read together, not as two separate
lists bolted on:

1. **The complete, unmodified business scope of the original Story 14 — "AI Features &
   Platform Administration"** (from the project's approved 6-story consolidation:
   Stories 09-14). That story was never started - no `.squad/stories` or `.squad/plans`
   folder for it exists, and a repo-wide code audit found zero AI/ML code and zero
   Platform Administration code anywhere. Its full original business intent is carried
   into this story without dilution: AI-assisted agent tooling (ticket summaries,
   suggested replies, automatic categorization, suggested solutions, an AI chatbot) and
   Platform Administration (user/role management UI, runtime system configuration,
   multi-department, multi-branch, custom branding).

2. **Genuine, verified remaining gaps from four already-implemented stories** - Story 4
   (Roles & Permissions), Story 5 (Audit Logging), Story 9 (Advanced Ticket Management &
   Agent Workspace), and Story 12 (Communication Channels & Integrations). Each of these
   stories shipped real, working functionality; each also left one or more of its own
   originally-stated scope items short of complete. Those specific shortfalls - and only
   those, verified against the current codebase immediately before writing this story,
   not assumed - are folded in below, in the functional area where they naturally belong
   rather than kept as a separate "leftovers" list.

**Explicit clarifications carried over from prior direction on this project:**
- The "Public API & Webhook Integration Framework" work merged in git under the label
  "Story 14" (branch `feature/story-14-public-api-webhook-framework`, PR #14) is a
  numbering collision, not the real Story 14. That work is tracked separately as
  **Story 24** and is NOT part of this story or its scope. This story restores the
  original Story 14 business intent under its own number (15).
- Story 11 (Knowledge Base & Customer Portal) is considered fully implemented and
  contributes nothing to this story.
- Stories 6, 7, 8, 10, and 13 are considered fully implemented for the purposes of this
  consolidation and contribute nothing to this story (their own minor nuances, if any,
  were not included in the merge instruction that produced this story and are therefore
  out of scope here).

**Verified current state (re-confirmed by direct code inspection immediately before
writing this story - not assumed):**
- No AI/ML/LLM code of any kind exists anywhere in `backend/` or `frontend/`.
- `AdminController.cs` has only `AssignRole` (assigns an *existing* user to a role) and
  `ListAuditLogs`; there is no user creation/list/edit/deactivate endpoint anywhere, and
  no frontend page exists for user or role administration at all.
- `AuditLog`/`AuditLogger`/`ListAuditLogs` all work and are written on real events, but
  no frontend page exists to browse all audit log entries system-wide - only per-ticket
  and per-customer scoped "history" sections exist.
- `Domain/TicketTask.cs` has an optional due date and a done flag, but nothing in the
  codebase ever turns an approaching or overdue task into a `Notification` - no
  scheduled/background check exists anywhere (confirmed: no `BackgroundService`/
  `IHostedService`/cron-like construct anywhere in `backend/src`).
- No mechanism exists for one agent to explicitly draw another agent's attention to a
  ticket beyond the fact that `TicketNote`s and audit history are visible to all staff
  (the same passive shared-data-access every entity in the app already has).
- `Integrations/OutboundWebhookDispatcher.cs` sends outbound webhook calls via a plain
  `HttpClient.PostAsJsonAsync` with no signature, HMAC, or shared-secret header of any
  kind - a receiving external system has no way to verify a call genuinely originated
  from this CRM.
- `TicketsController.cs` has a code comment directly above its hardcoded `SlaTargets`
  dictionary stating an admin-editable configuration UI was explicitly deferred to
  Story 14.
- No `Department`/`Branch` entity, field, or UI exists anywhere. No branding/logo/theme
  configuration mechanism exists anywhere - the app name is a static hardcoded string.

Real, live connectivity to Email/WhatsApp/SMS providers (Story 12) is explicitly NOT a
gap this story addresses - the code paths are already complete and correct; the absence
of a live-verified connection is an operational/credentials matter (no real vendor
account exists in this environment), not a missing business requirement to build.

# 4. Functional Scope

Organized by domain, not by originating story number:

## 4.1 Roles & User Administration
*(Original Story 14 scope + Story 4's genuine remaining gap, merged - one coherent admin
capability, not two.)*
- Full staff user administration: create, list, view, edit, and deactivate a staff
  (User) account via both API and a dedicated Admin-only frontend page.
- Role management via the same page: assign/revoke a user's role(s) using the existing
  `AssignRole` mechanism, now exposed through a real UI instead of only a raw endpoint.

## 4.2 Audit & Compliance
*(Story 5's genuine remaining gap.)*
- A global, Admin-only audit log viewer page surfacing every `AuditLog` entry
  system-wide (not just the existing per-ticket/per-customer scoped views, which remain
  as-is), with basic filtering (by action, by actor, by date range) so an Admin can
  answer "what happened system-wide" without database access.

## 4.3 Advanced Agent Workspace — Reminders & Agent Mentions
*(Story 9's two genuine remaining gaps.)*
- Task reminders: when a `TicketTask`'s due date arrives, or falls within an
  **Admin-editable reminder lead time** (a runtime setting managed through the same
  mechanism as section 4.6's Runtime Configuration - not a separate, standalone config
  path, and not hardcoded), the system generates a real, delivered notification (reusing
  the existing `Notification`/`NotificationBell` mechanism already built in Story 10) to
  the task's owner - closing the gap between "a task has a due date field" and "a
  reminder actually happens."
- Agent mentions (the concrete, sole form of "team collaboration" in scope): Agent A can
  mention/tag Agent B when writing a ticket note, and Agent B receives a dedicated
  notification, distinguishable from a plain assignment notification, directing their
  attention to that specific ticket. This is the entire "team collaboration" requirement
  - it is explicitly **not** a general chat, presence, or team-messaging system, and must
  not be expanded into one.

## 4.4 Communication Integrations — Outbound Trust
*(Story 12's one genuine, buildable remaining gap - explicitly excludes live
provider-connectivity verification, which is operational, not a business requirement.)*
- Outbound webhook calls (the existing Story 12 mechanism) are signed with a shared
  secret/HMAC header so a receiving external system can verify a call genuinely
  originated from this CRM. No change to the existing subscription model, event types,
  or delivery-failure isolation behavior.

## 4.5 AI Features
*(Original Story 14 scope, carried forward in full, unmodified - now with an explicit,
unambiguous implementation boundary.)*

**AI integration boundary (resolves the "AI must work" vs. "real vendor credentials are
out of scope" tension):** the system introduces one provider-agnostic AI abstraction
(mirroring the existing `IChannelSender` pattern from Story 12 - one interface, swappable
implementations, no controller/feature hardcoded to a specific vendor SDK). All five AI
capabilities below are implemented **against this abstraction**, not against a named
vendor. Building the abstraction, wiring every capability through it, and the "no
provider configured" degraded behavior are **in scope and must be built and verified**.
Only holding a real, paid, credentialed account with a specific AI vendor is out of
scope (an operational/procurement matter, same treatment as Story 12's Email/WhatsApp/SMS
providers). When a provider *is* configured (even a local/mock one used for
verification), each capability must produce a real result through that provider; when
none is configured, each capability must clearly report an "AI unavailable / not
configured" state - mirroring `SendStatus.NotConfigured` in
`EmailSender.cs`/`WhatsappSender.cs`/`SmsSender.cs` - rather than erroring or blocking the
underlying manual workflow (a ticket can still be created/replied-to/categorized by hand
exactly as today, with or without AI available).

- Ticket summaries - an AI-generated concise summary of a ticket's content/notes.
- Suggested replies - AI-generated draft reply suggestions for an agent working a
  ticket, distinct from the existing manually-authored Quick Replies (Story 9).
- Automatic categorization - AI-suggested (not silently auto-applied) Category/Priority
  for a new ticket, building on the existing manual Category/Priority fields (Story 9).
- Suggested solutions - AI-surfaced relevant Knowledge Base articles (Story 11) for a
  given ticket.
- AI chatbot - an AI-driven conversational assistant, distinct from the existing
  human-agent Live Chat (Story 12).

## 4.6 Platform Administration, Runtime Configuration & Branding
*(Original Story 14 scope, carried forward in full, unmodified.)*
- Runtime system configuration: an Admin-editable configuration surface for
  operationally-tunable values that are hardcoded today - starting with the SLA
  response/resolution target hours per priority (`TicketsController.SlaTargets`) -
  changeable without a code deploy.
- Multi-department support: a Department concept that users/tickets can be associated
  with.
- Multi-branch support: a Branch concept (physical/regional org unit), distinct from
  Department, that users/tickets can be associated with.
- Custom branding: an Admin-configurable app name/logo/theme, replacing the current
  static hardcoded app name.

# 5. Functional Requirements

1. An Admin can create a new staff user (email, display name, initial password/role)
   through a dedicated UI, without touching the database or a seed script.
2. An Admin can list all staff users, view a user's current role(s), and change or
   revoke them through the same UI (reusing `AdminController.AssignRole` server-side).
3. An Admin can deactivate a staff user such that they can no longer authenticate,
   without deleting their historical audit trail or authored notes/tickets.
4. An Admin can view a paginated, filterable list of every audit log entry system-wide
   through a dedicated page, independent of any single ticket or customer record.
5. An Admin can set a reminder lead time (a number of hours) as an **Admin-editable
   runtime setting**, using the same runtime-configuration mechanism as Functional
   Requirement 14 (SLA targets) - not a fixed, hardcoded value and not a separate
   standalone config path. When a `TicketTask`'s due date is reached, or falls within
   that configured lead time, the system creates a `Notification` for the task's owner
   exactly once per task, visible through the existing notification bell.
6. Agent A can mention/tag Agent B when writing a note on a ticket. Agent B receives a
   notification distinguishable from a plain assignment notification, directing their
   attention to that specific ticket. This is the sole and complete "team collaboration"
   mechanism in scope - no general chat, presence, or team-messaging capability is to be
   built alongside or instead of it.
7. Every outbound webhook delivery includes a signature (HMAC-SHA256 or equivalent)
   computed from a per-subscription or global shared secret, verifiable by the receiving
   system; the existing `OutboundWebhookDispatcher` retry/failure-isolation behavior is
   unchanged.
8. A provider-agnostic AI integration abstraction exists (mirroring `IChannelSender`),
   and Functional Requirements 9-13 below are implemented against it, not against a
   hardcoded vendor SDK. When no AI provider is configured, every one of Functional
   Requirements 9-13 reports a clear "AI unavailable / not configured" state instead of
   erroring, and the underlying manual workflow it augments (ticket creation, replying,
   categorizing, browsing the Knowledge Base) continues to work exactly as it does today.
9. An agent can request an AI-generated summary of a ticket's content and notes via the
   abstraction in Functional Requirement 8.
10. An agent can request AI-suggested reply text for a ticket, insertable into a note the
    same way an existing Quick Reply is today, via the abstraction in Functional
    Requirement 8.
11. When a ticket is created (or on agent request), the system suggests a Category/
    Priority via the abstraction in Functional Requirement 8, without silently
    overriding the agent's own choice.
12. An agent can see AI-suggested, relevant Knowledge Base articles for the ticket they
    are working, via the abstraction in Functional Requirement 8.
13. A customer (or an anonymous visitor, consistent with the existing Web Form's
    anonymous-intake precedent) can interact with an AI chatbot, distinct from the
    existing human-staffed Live Chat, via the abstraction in Functional Requirement 8.
14. An Admin can edit SLA response/resolution target hours per priority at runtime,
    with the change taking effect for newly-computed due dates without a deploy.
15. An Admin can create/manage Department and Branch records and associate a User and/or
    Ticket with a Department and/or Branch.
16. An Admin can configure the application's display name, logo, and a basic theme
    color, reflected across the staff app shell and customer portal shell.

# 6. Acceptance Criteria

**Users & Roles**
- [ ] A new staff user can be created, assigned a role, and successfully logs in - all
      without any direct database/seed-script action.
- [ ] An Admin can change an existing staff user's role assignment (e.g. Agent → Admin)
      through the UI, and the change takes effect on that user's next authenticated
      request.
- [ ] An Admin can revoke a role from an existing staff user through the UI, wherever the
      underlying RBAC model supports removing a role assignment (no new granular
      permission system is introduced to support this).
- [ ] An existing staff user can be deactivated and immediately loses the ability to
      authenticate; their historical audit/ticket/note data remains intact and visible.

**Audit & Compliance**
- [ ] An Admin can open a single page and see every audit log entry system-wide, filtered
      by at least action type and date range.

**Agent Workspace**
- [ ] An Admin can set the reminder lead time as a runtime setting; a `TicketTask` whose
      due date has passed, or falls within that configured lead time, produces a real,
      visible notification to its owner without any manual trigger.
- [ ] Agent A can mention Agent B on a ticket note, and Agent B receives a notification
      distinguishable from a plain assignment notification, tied to that specific ticket.
      No general chat/presence/team-messaging surface is introduced.

**Communication Integrations**
- [ ] A captured outbound webhook payload includes a verifiable signature header; an
      altered payload fails signature verification.

**AI Features - credential-independent (must pass with no AI provider configured)**
- [ ] With no AI provider configured, every AI entry point (ticket summary, suggested
      reply, suggested category, suggested KB article, chatbot) clearly reports an
      "AI unavailable / not configured" state, and the underlying manual workflow
      (creating/replying-to/categorizing a ticket, browsing the Knowledge Base) is
      completely unaffected and continues to work exactly as it does today.
- [ ] The AI integration abstraction exists as a distinct interface with no controller or
      page hardcoded to a specific vendor SDK (verified by code review, the same way
      `IChannelSender` has multiple interchangeable implementations).

**AI Features - requires a configured provider (may be a local/mock provider for
verification purposes; a real paid vendor account is explicitly not required to pass
these)**
- [ ] An agent can generate a ticket summary and a suggested reply for a real ticket, and
      insert the suggested reply into a note.
- [ ] A newly created ticket receives an AI-suggested Category/Priority that the agent
      can accept or override before or after ticket creation.
- [ ] A ticket detail view surfaces at least one AI-suggested Knowledge Base article when
      relevant content exists.
- [ ] A customer can hold a conversation with the AI chatbot distinct from the human Live
      Chat entry point.

**Platform Administration**
- [ ] An Admin can change an SLA target value and see it reflected in a newly created
      ticket's due dates without a code deploy.
- [ ] An Admin can create a Department and a Branch and assign a User to both.
- [ ] An Admin can change the app's display name/logo/theme color and see it reflected in
      both the staff shell and the customer portal shell.

**Regression**
- [ ] Every existing Story 1-13 (and Story 24) feature continues to work unchanged.

# 7. Dependencies

- Story 3 (JWT auth foundation) - user administration and login/deactivation build on it.
- Story 4 (Roles & Permissions) - role assignment mechanism (`AdminController.AssignRole`)
  is reused, not replaced.
- Story 5 (Audit Logging) - `AuditLog`/`AuditLogger` are reused as the data source for the
  global viewer; no schema change to the entity itself is anticipated.
- Story 9 (Advanced Ticket Management & Agent Workspace) - `TicketTask`/`Notification`
  are reused for reminders; `TicketNote` is the anchor point for the collaboration
  mention feature.
- Story 10 (SLA, Automation & Notifications) - `Notification`/`NotificationBell` delivery
  pipe is reused as-is for task reminders and collaboration mentions; `SlaTargets` is the
  target of the runtime-configuration requirement.
- Story 11 (Knowledge Base & Customer Portal) - Knowledge Base content is what "suggested
  solutions" surfaces; considered fully implemented, contributes no remaining gap itself.
- Story 12 (Communication Channels & Integrations) - `OutboundWebhookDispatcher` is the
  target of the signing requirement; Live Chat (`ChatHub`/`ChatWidget`) is the point of
  contrast for the distinct AI chatbot requirement.
- Story 24 (Public API & Webhook Integration Framework) - unrelated; explicitly not a
  dependency and not to be confused with this story or the original Story 14.
- All of the above are merged on `main` and available to build against.

# 8. Out of Scope

- Holding or connecting to a real, paid, credentialed account with any specific AI
  vendor/LLM provider. **This is the only AI-related exclusion.** Building the
  provider-agnostic AI abstraction, wiring all five AI capabilities through it, and the
  "no provider configured → clearly unavailable" degraded behavior are explicitly IN
  scope and must be implemented and verified (see section 4.5's AI integration boundary
  and the credential-independent Acceptance Criteria in section 6).
- Real, live connectivity to Email/WhatsApp/SMS providers (a Story 12 matter, unrelated
  to AI) - the code paths already exist and are complete; connecting to a real vendor
  account is an operational/procurement matter, not part of this story.
- ERP integration of any kind (explicitly out of scope in every prior story that touched
  integrations, and not part of the original Story 14 scope either).
- Customer-level notes/attachments (a real gap found during the broader coverage audit,
  but tied to Stories 6/9, not the four stories this consolidation was scoped to draw
  from - explicitly not included here per the merge instruction that produced this
  story).
- Knowledge Base content-type distinctions (FAQ vs. Guide vs. Solution) - Story 11 is
  considered fully implemented; this nuance was not part of the merge instruction.
- Any change to Stories 1-13 or Story 24's existing, already-working functionality.
- A granular/custom permission system beyond role-based access control - the original
  Story 14 scope names "User management UI" and "Role management UI" specifically, not a
  new permission model; this story does not invent one.
- Rewriting or renaming the git branch/PR/commit history that caused the Story 14/24
  numbering collision - that is a repository-history matter for the project owner, not
  something this story's implementation addresses.

# 9. Definition of Done

- Every Functional Requirement in Section 5 is implemented and its corresponding
  Acceptance Criterion in Section 6 passes.
- No existing Story 1-13 or Story 24 functionality regresses (existing automated tests
  continue to pass; manual regression spot-check on each area per the project's
  established verification pattern).
- New backend endpoints and frontend pages follow the established conventions of this
  codebase (`[FromServices]` per-action DI, `[Authorize(Policy = "RequireStaff", Roles =
  "Admin")]` for admin-only surfaces, `adminOnly` nav-gating, full `en.json`/`ar.json`
  parity, logical/RTL-safe Tailwind classes) - verified during implementation review, not
  assumed.
- The AI integration abstraction is built, all five AI capabilities are wired through it,
  and every one of them degrades to a clear "not configured/unavailable" state when no
  provider is configured, mirroring the existing `NotConfigured` pattern already used by
  `EmailSender`/`WhatsappSender`/`SmsSender` - this is a required, in-scope deliverable,
  verifiable without any real vendor credential, not an optional stretch goal.
- The credential-independent Acceptance Criteria in Section 6 (the "no AI provider
  configured" behaviors, plus every non-AI requirement) are fully verified before this
  story is considered done. The credential-dependent AI Acceptance Criteria (an actual
  AI-generated result) may be verified against a local/mock provider standing in for a
  real vendor; the implementation plan must explicitly state which of those it verified
  this way vs. which remain untested pending a real provider account.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
See Description, Section 6 "Acceptance Criteria" for the full checklist, grouped by
area. Summarized:

1. Staff user create/deactivate works end-to-end via UI (no DB/seed-script action);
   an Admin can change AND revoke an existing user's role assignment via the UI.
2. Global, filterable, Admin-only audit log viewer exists.
3. Task reminder lead time is an Admin-editable runtime setting; due-date reminders
   produce a real Notification automatically once within it.
4. Agent A can mention Agent B on a ticket note; Agent B gets a notification distinct
   from a plain assignment notification. No general chat/collaboration surface.
5. Outbound webhook payloads carry a verifiable signature.
6. CREDENTIAL-INDEPENDENT (must pass with no AI provider configured): every AI entry
   point clearly reports "unavailable/not configured" and the underlying manual
   workflow is unaffected; the AI abstraction has no vendor-hardcoded controller/page.
7. CREDENTIAL-DEPENDENT (a local/mock provider suffices; no real vendor account
   required): AI ticket summaries + suggested replies are available and insertable
   into notes; AI-suggested Category/Priority is offered (never silently auto-applied);
   AI-suggested relevant Knowledge Base articles surface on a ticket; an AI chatbot
   exists as a distinct entry point from human Live Chat.
8. SLA target hours are Admin-editable at runtime, reflected in new tickets' due dates.
9. Department and Branch entities exist and can be assigned to a User.
10. App display name/logo/theme are Admin-configurable and reflected in both shells.
11. All existing Story 1-13 and Story 24 functionality is unaffected.
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

- **Blocked by / related ids:** None (no external tracker). Conceptually depends on
  Stories 3, 4, 5, 9, 10, 11, 12 (all merged on `main`) as described in the Description's
  Section 7 "Dependencies". Explicitly NOT dependent on, and NOT to be confused with,
  Story 24 (Public API & Webhook Integration Framework) - which is the work mislabeled
  "Story 14" in git history due to a branch-naming collision, unrelated to this story's
  scope.
- **Depends on code areas or other stories:**
  - `backend/src/CustomerSupportCrm.Api/Controllers/AdminController.cs` (extend with user
    CRUD; existing `AssignRole`/`ListAuditLogs` reused, not replaced)
  - `backend/src/CustomerSupportCrm.Api/Domain/User.cs`, `Role.cs`, `UserRole.cs`
  - `backend/src/CustomerSupportCrm.Api/Domain/AuditLog.cs`, `Auditing/AuditLogger.cs`
  - `backend/src/CustomerSupportCrm.Api/Domain/TicketTask.cs`,
    `Domain/Notification.cs`, `Controllers/NotificationsController.cs`,
    `frontend/src/components/NotificationBell.tsx`
  - `backend/src/CustomerSupportCrm.Api/Domain/TicketNote.cs`
  - `backend/src/CustomerSupportCrm.Api/Integrations/OutboundWebhookDispatcher.cs`,
    `Domain/OutboundWebhookSubscription.cs`
  - `backend/src/CustomerSupportCrm.Api/Controllers/TicketsController.cs` (`SlaTargets`
    dictionary - target of the runtime-configuration requirement)
  - `backend/src/CustomerSupportCrm.Api/Domain/KnowledgeArticle.cs` (AI "suggested
    solutions" surfaces this content)
  - `backend/src/CustomerSupportCrm.Api/Hubs/ChatHub.cs`,
    `frontend/src/components/ChatWidget.tsx` (point of contrast for the distinct AI
    chatbot requirement - not modified, only referenced)
  - `frontend/src/components/layout/AppShell.tsx`, `PortalShell.tsx` (branding surfaces)
  - `frontend/src/i18n/locales/en.json`, `ar.json`

## Extra notes (optional)

- This story is a deliberate consolidation, approved by the user, of: (a) the complete,
  unmodified original Story 14 business scope, plus (b) only the verified-genuine
  remaining gaps in Stories 4, 5, 9, and 12. It is not a copy of any prior story and not
  a generic "remaining gaps" catch-all - every item in Section 4 (Functional Scope) was
  either explicitly named in the original Story 14 or verified missing by direct code
  inspection immediately before this intake was written.
- Do not confuse this story's number (15) with the git-history numbering collision on
  "Story 14" (which actually contains Story 24's Public API/Webhook work, merged under a
  mislabeled branch name outside this project's own conversational tracking).

## Technical hints (optional)

- Repo root: `.` (backend: `backend/src/CustomerSupportCrm.Api`; frontend:
  `frontend/src`). Backend language: C# / .NET 8 / EF Core 8. Frontend language:
  TypeScript / React.
- Existing conventions to follow (established since Story 12): `[FromServices]`
  per-action DI (no declared controller constructors), `Guid` for every entity Id/FK,
  `internal static` allow-list constants for shared validation, `adminOnly` nav-gating in
  `AppShell.tsx`, full `en.json`/`ar.json` key parity, logical Tailwind classes for
  RTL-safety, the existing `NotConfigured`/graceful-degradation pattern from
  `EmailSender.cs`/`WhatsappSender.cs`/`SmsSender.cs` for any new external-AI-provider
  integration point.
- No AI provider, vendor, or SDK is specified by this story - the implementation plan
  should treat "AI Features" the same way Story 12 treated channel providers: a generic,
  provider-agnostic interface/abstraction, not a hardcoded dependency on one named
  vendor, so it can degrade gracefully with no configured credentials.

## Out of scope

- What this story explicitly does **not** cover:
  - Holding/connecting a real, paid, credentialed account with a specific AI vendor -
    the only AI-related exclusion; building the provider-agnostic AI abstraction and its
    graceful "not configured" degradation is explicitly IN scope (see Description
    Section 4.5).
  - Live/paid connectivity to any real external Email/WhatsApp/SMS provider, or ERP
    system - operational/procurement matters, not business requirements to build.
  - ERP integration of any kind.
  - Customer-level notes/attachments (a real gap, but out of this story's four-story
    merge instruction - tied to Stories 6/9's Customer Management area, not requested).
  - Knowledge Base content-type distinctions (FAQ vs. Guide vs. Solution) - Story 11 is
    considered fully implemented.
  - Any modification to Stories 1-13 or Story 24's existing functionality.
  - A granular/custom permission system beyond role-based access control.
  - Rewriting git branch/PR/commit history related to the Story 14/24 numbering
    collision.
