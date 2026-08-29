# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/app-shell-navigation-breadcrumbs/16/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** App Shell Navigation: Breadcrumbs & Sidebar Profile Placement
- **Feature slug (folder under `plans/`):** `app-shell-navigation-breadcrumbs`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `16`
- **Work item type:** `User Story`
- **Status:** `Drafted`
- **Assignee:** ``
- **Labels:** `frontend`, `app-shell`, `navigation`, `ux`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Story 16 — App Shell Navigation: Breadcrumbs & Sidebar Profile Placement
```

---

## Description

### 1. User Story / Business Goal

As a staff user (Agent or Admin) navigating the Customer Support CRM, I want to always
see where I am in the application (via breadcrumbs) and to reach my account/profile
actions from a consistent, prominent place in the sidebar, so that I can orient myself
in a growing set of pages (currently 20+ authenticated routes across Tickets, Customers,
Reports, and 8 Admin sub-areas) and reach logout/profile without hunting for it.

### 2. Business Context

Story 15 alone added seven new admin-only pages under `/admin/*`, on top of the
already-existing Reports (5 pages), Tickets, Customers, Knowledge Base, Quick Replies,
Webhooks, and API Keys areas. `AppShell.tsx`'s sidebar (`NAV_ITEMS`) is now a flat list of
15 links with no grouping or hierarchy indicator, and none of the pages show the user
where they are beyond the page's own `<h1>` heading. Detail pages
(`CustomerDetailPage.tsx`, `TicketDetailPage.tsx`) and the Reports area each hand-roll
their own "back to list" link and dynamic title — there is no shared, reusable mechanism,
so every future page that needs "where am I / how do I get back" logic has to reinvent it.

Separately, the account/profile block (display name, email, logout) currently sits at the
very bottom of the sidebar (`AppShell.tsx` lines 106–119), below the full nav list, which
on a long nav list (or on a short viewport) can require scrolling to reach. Moving it to
the top makes it immediately visible and matches a common app-shell convention (identity
first, then navigation).

This story treats both changes as a single **shared App Shell improvement**: a reusable
breadcrumb component driven by route data (not copy-pasted per page), and a repositioned
— not duplicated — profile block.

### 3. Inspection Findings (current implementation, as of Story 15)

Recorded here because they materially shape the Functional Requirements below, and must
be validated again (not assumed) by whoever plans/implements this story:

- **Router:** `frontend/src/routes/AppRouter.tsx` uses React Router v6.30 in declarative
  mode (`<BrowserRouter><Routes><Route>`), not the data-router (`createBrowserRouter`)
  API. `Route.handle` metadata is available in this mode (supported since v6.4
  regardless of router flavor) and `useMatches()` resolves against it — this is the
  most likely foundation for route-driven breadcrumbs, to be confirmed at planning time.
- **Routes are flat, not nested in the route tree.** `/tickets` and `/tickets/:id` (same
  for `/customers`, `/reports/*`) are declared as sibling `<Route>` elements directly
  under the single `<Route element={<AppShell />}>` wrapper — they are **not** parent/child
  routes in React Router's own tree. A breadcrumb "hierarchy" therefore cannot be read
  off existing route nesting as-is; it needs an explicit, lightweight hierarchy
  declaration (e.g., a parent path per route, or a small path-segment convention) as part
  of this story's foundation.
- **No page-title/breadcrumb metadata exists anywhere today.** Every page renders its own
  `<h1>` with a manually-typed i18n key. Detail pages
  (`CustomerDetailPage.tsx:50-60`, `TicketDetailPage.tsx`) render a **dynamic** label (the
  customer's name / ticket's subject) fetched from the API, not the route's `:id`
  parameter or a static string.
  **Consequence:** a route-metadata-only breadcrumb can supply static labels (e.g.
  "Tickets", "Reports", "SLA Targets") automatically, but the *last* crumb on a detail
  page needs a way to receive a dynamic label once the page's data has loaded. The
  reusable foundation must support both cases without every page reimplementing crumb
  rendering.
- **No "raw ID" leakage today** — existing hand-rolled titles already avoid this by
  showing the loaded entity's name. The new breadcrumb system must preserve that
  (never fall back to printing a GUID segment).
- **Sidebar today (`AppShell.tsx`):** top of `<aside>` = logo + app name only
  (lines 93–96); nav list in the middle (`NAV_ITEMS`, filtered by `adminOnly`/role);
  profile block (display name, email, logout button) pinned to the bottom via `mt-auto`
  (lines 106–119).
- **Logout already exists in two places today:** the sidebar's bottom profile block, and
  a separate compact logout icon button in the top header (`AppShell.tsx` lines 141–149,
  visible at all viewport widths, not just mobile). Moving the profile block must not
  introduce a *third* logout affordance or leave two out of sync — how the existing
  header logout icon relates to the relocated profile block is a planning-time decision
  this story surfaces but does not resolve.
- **Mobile/responsive behavior:** the sidebar is a fixed-position slide-in drawer below
  the `lg` breakpoint (`hidden`/`flex` toggle + `fixed inset-y-0`), opened via a hamburger
  button in the header and closed on route change, backdrop click, or Escape. Any
  repositioned profile block or new breadcrumb bar must work inside this existing drawer
  behavior, not replace it.
- **RTL/i18n:** RTL is applied globally via `document.documentElement.dir`, toggled in
  `LanguageProvider.tsx` based on the persisted language; components use logical Tailwind
  classes (`start-`, `end-`, `text-start`, etc.) rather than `left`/`right`. `en.json`/
  `ar.json` are flat-ish namespaced JSON files with a `shell.*` namespace already used by
  `AppShell.tsx` (`shell.nav.*`, `shell.sidebar`, `shell.openMenu`, `shell.closeMenu`).
  A `shell.breadcrumb.*` (or similar) namespace is the natural extension point.
- **Scope note:** this inspection, and this story, covers `AppShell.tsx` (the
  staff/agent-facing shell) only. `PortalShell.tsx` (the customer portal shell) has an
  analogous but separate profile block and nav list; it is **not** touched by this story
  (see Out of Scope).

### 4. Functional Scope

**In scope:**
- A reusable breadcrumb component/system rendered once, inside the shared `AppShell.tsx`
  layout (not per-page), appearing above each page's own content on every authenticated
  route.
- A lightweight, explicit route-hierarchy/label declaration that both the new breadcrumb
  system and (optionally, if convenient) the existing sidebar nav can read from a single
  source of truth, so hierarchy and labels aren't hand-maintained in two places.
- A mechanism for a page with a dynamically-loaded title (customer/ticket detail pages
  today; any future detail page) to supply its current-crumb label once data is loaded,
  without that page owning any other part of the breadcrumb.
- Repositioning the existing profile/account block from the bottom to the top of the
  `AppShell.tsx` sidebar, preserving every action it currently exposes.
- i18n coverage (`en.json` + `ar.json`) and RTL correctness for both changes.
- Preserving existing mobile drawer behavior for the relocated profile block, and making
  the new breadcrumb bar responsive (does not overflow or break layout on narrow
  viewports).

**Out of scope (see also §8):**
- `PortalShell.tsx` (customer portal) — not touched.
- Changing which nav items exist, their `adminOnly` gating, or role-based visibility.
- Changing authentication/authorization behavior of any kind.
- Any visual/branding redesign beyond what's needed to place the breadcrumb bar and
  relocate the profile block (Story 15's `useBranding()`/`--brand-primary` mechanism is
  reused as-is, not extended).
- A generic "page title" system beyond what breadcrumbs need (e.g., `document.title`
  / browser tab title changes are not requested and not included).
- Removing or consolidating the existing duplicate logout affordance (header icon vs.
  sidebar block) — flagged as a planning-time question, not decided here.

### 5. Functional Requirements

**FR1 — Reusable breadcrumb foundation.** A single breadcrumb component is added to the
shared `AppShell.tsx` layout (rendered once, above `<Outlet />`), consuming a route-driven
data source. No individual page under `frontend/src/pages/` implements its own breadcrumb
rendering.

**FR2 — Route hierarchy & static labels.** Every authenticated route in `AppRouter.tsx`
gets an explicit, declared parent (where one exists) and a static, translatable label,
in one place the planner/implementer designates (e.g. route `handle` data, or a small
route-metadata map). This is the single source of truth the breadcrumb reads from.

**FR3 — Dynamic label override for detail pages.** Pages that load an entity
asynchronously (`CustomerDetailPage.tsx`, `TicketDetailPage.tsx`, and any future detail
page) can supply the current page's breadcrumb label once their data has loaded (e.g. the
customer's name, the ticket's subject), replacing the route's default/static label or the
raw `:id` for that one crumb. Until the data loads, the breadcrumb never shows the raw
route parameter (id/GUID) — it shows nothing for that crumb, a loading placeholder, or the
parent's label, at the planner's discretion.

**FR4 — Breadcrumb navigation.** Every crumb except the current (last) one is a link that
navigates to that ancestor route. The current page's crumb is visibly distinguished
(e.g. non-interactive, different styling) from ancestor crumbs, and is announced to
assistive technology as the current page (e.g. `aria-current="page"`).

**FR5 — i18n & RTL.** Every user-visible breadcrumb label resolves through
`react-i18next` with entries in both `en.json` and `ar.json`. The breadcrumb's layout
(separators, direction of reading, link order) is correct in both LTR and RTL without
component-level `if (isRtl)` branching — logical Tailwind classes only, matching the
existing codebase convention.

**FR6 — Responsive/mobile behavior.** The breadcrumb bar renders without layout breaks,
overflow, or text truncation that hides the current page's name at common mobile
viewport widths; on very long trails it may wrap or scroll horizontally within its own
container rather than breaking the page layout.

**FR7 — Sidebar profile relocation.** The existing profile/account block currently at
the bottom of `AppShell.tsx`'s `<aside>` (display name, email, logout action) is moved to
the top of the same `<aside>`, above the nav list. It is the same block — not a second,
independently-implemented copy — reusing the existing `useAuthStore` data and the
existing `handleLogout` behavior already defined in `AppShell.tsx`.

**FR8 — No regression to existing behavior.** Authentication, role-based nav filtering
(`adminOnly`/`hasRole('Admin')`), the mobile drawer (open/close/backdrop/Escape/route-change-close),
and every existing page's own content are unaffected by this story.

### 6. Acceptance Criteria

**Breadcrumb rendering & hierarchy**
- [ ] AC1: On every authenticated route reachable via `AppShell.tsx` (Home, Customers,
      Customer Detail, Tickets, Ticket Detail, Quick Replies, Knowledge Base, Webhooks,
      API Keys, Reports + its 4 sub-pages, and all 7 `/admin/*` pages), a breadcrumb trail
      renders above the page's own content, without that page's own component containing
      any breadcrumb-rendering code.
- [ ] AC2: On a top-level page (e.g. `/tickets`, `/admin/users`), the breadcrumb shows
      exactly the page's own label as the current (non-link) crumb — no artificial
      parent is invented for pages that have none.
- [ ] AC3: On a nested page (e.g. `/tickets/:id`, `/reports/sla`), the breadcrumb shows
      the full ancestor chain (e.g. Tickets → [ticket name]; Reports → SLA Targets), each
      ancestor rendered as a link.

**Parent navigation & current-page indication**
- [ ] AC4: Clicking/activating any non-current crumb navigates to that route and the
      breadcrumb updates to match the new location.
- [ ] AC5: The final crumb (current page) is not a clickable link, is visually
      distinguished from the ancestor crumbs, and carries `aria-current="page"`.

**Dynamic labels / no raw IDs**
- [ ] AC6: On `/customers/:id`, the final crumb shows the loaded customer's name once
      available, and at no point (including the initial loading state) shows the raw
      `:id` route segment.
- [ ] AC7: On `/tickets/:id`, the final crumb shows the loaded ticket's subject once
      available, and at no point shows the raw `:id` route segment.

**i18n / RTL**
- [ ] AC8: Every breadcrumb label sourced from a static route (not a dynamic entity name)
      is present as a key in both `en.json` and `ar.json`; the existing
      `check_i18n_parity`-style key-set diff passes with zero missing keys on either side.
- [ ] AC9: With the app language set to Arabic, the breadcrumb reads correctly in RTL
      (crumb order follows reading direction, separators/icons mirror correctly) with no
      component-level LTR/RTL conditional branching — verified by manual RTL sweep.

**Responsive / mobile**
- [ ] AC10: At a mobile viewport width (matching the existing `lg` breakpoint the sidebar
      drawer already uses), the breadcrumb bar renders without horizontal page overflow
      and the current page's label remains visible (wraps or scrolls within its own
      container rather than being clipped or pushing other header content off-screen).

**Sidebar profile placement**
- [ ] AC11: In `AppShell.tsx`, the profile/account block (display name, email, logout
      action) renders at the top of the `<aside>`, above the nav list; the nav list
      renders below it.
- [ ] AC12: The relocated profile block calls the same `handleLogout`/`useAuthStore`
      logic already present in `AppShell.tsx` today — grep/inspection confirms no second,
      independent implementation of profile data access or logout was added anywhere in
      the codebase for this story.
- [ ] AC13: Every action available from the profile block before this story (at minimum:
      viewing display name/email, logging out) remains available and functionally
      identical after relocation.

**Regression / non-goals**
- [ ] AC14: Role-based nav filtering (`adminOnly` items hidden for a non-Admin) behaves
      identically before and after this change.
- [ ] AC15: The mobile drawer's existing behavior (hamburger toggle, backdrop click to
      close, Escape to close, auto-close on route change) is unchanged, verified manually
      on at least the Home, Tickets, and one `/admin/*` route.
- [ ] AC16: No existing page under `frontend/src/pages/` had its own route, permissions,
      or content logic altered as a side effect of this story (route element changes are
      limited to what FR2 requires — adding hierarchy/label metadata — not changing which
      component renders for which path).

### 7. Dependencies

- Depends on the current state of `frontend/src/components/layout/AppShell.tsx` and
  `frontend/src/routes/AppRouter.tsx` as they exist after Story 15 (merged/implemented in
  this same working tree).
- Depends on `frontend/src/store/useAuthStore.ts` (`user`, `clearSession`, `hasRole`) —
  reused as-is, not modified.
- Depends on the existing i18n setup (`react-i18next`, `en.json`/`ar.json`,
  `LanguageProvider.tsx`'s `document.documentElement.dir` mechanism) — reused as-is.
- Depends on `react-router-dom` v6.30 already in `package.json` — no version change
  expected; if the planner determines route `handle`/`useMatches()` is insufficient and a
  router-mode change (e.g. to `createBrowserRouter`) is required, that is a
  planning-time decision to flag explicitly, not assumed here.
- No backend dependency — this is a frontend-only story. No new API endpoints, no DB
  changes.

### 8. Out of Scope

- `PortalShell.tsx` and every customer-portal page (`/portal/*`).
- Adding, removing, reordering, or re-gating any sidebar nav item.
- Any authentication/authorization behavior change.
- A `document.title`/browser-tab-title system.
- Resolving the pre-existing duplicate logout affordance (sidebar block vs. header icon)
  — noted as a discovered fact and a planning-time question, not resolved by this story.
- Visual redesign/rebranding beyond placing the new breadcrumb bar and relocating the
  profile block (existing Story 15 branding/theming mechanism reused unchanged).
- Introducing a frontend test runner/harness — this project has none (confirmed across
  every prior story through Story 15) and none is authorized here; verification is
  manual, same as every prior story.

### 9. Definition of Done

- [ ] A reusable breadcrumb component/system exists, is wired into `AppShell.tsx` once,
      and every authenticated page listed in AC1 shows a correct trail with no per-page
      breadcrumb code.
- [ ] Route hierarchy and static labels are declared in one designated place (not
      duplicated across files), matching FR2.
- [ ] `CustomerDetailPage.tsx` and `TicketDetailPage.tsx` supply a dynamic current-crumb
      label through the mechanism established by FR3, with no raw ID ever shown.
- [ ] The profile/account block is relocated to the top of `AppShell.tsx`'s sidebar,
      reusing existing data/logout logic, with no second implementation anywhere.
- [ ] All new user-visible strings exist in both `en.json` and `ar.json` (parity check
      passes).
- [ ] RTL sweep (Arabic) and a mobile-viewport sweep both pass manually for every route
      in AC1.
- [ ] Existing role-based nav filtering, mobile drawer behavior, and authentication are
      manually confirmed unchanged (AC14–AC16).
- [ ] No file under `frontend/src/pages/**`, `PortalShell.tsx`, or any backend project is
      modified except as required to wire in dynamic breadcrumb labels (FR3) — confirmed
      via a scoped `git diff`/`git status` review before considering the story complete.

---

## Acceptance criteria

*(See the numbered AC1–AC16 checklist embedded in the Description above — this project's
established intake convention for this codebase, matching Stories 4–15.)*

```
See "6. Acceptance Criteria" above.
```

---

## Attachments

None.

---

## Dependencies

- **Blocked by / related ids:** None. Builds on the current state of the frontend after
  Story 15 (id 15) in this same working tree; not blocked by any open story.
- **Depends on code areas or other stories:** `frontend/src/components/layout/AppShell.tsx`,
  `frontend/src/routes/AppRouter.tsx`, `frontend/src/store/useAuthStore.ts`,
  `frontend/src/i18n/**`. See "7. Dependencies" above for the full list.

## Extra notes (optional)

- This story was explicitly requested to remain separate from Story 15 and must not
  modify Story 15 or any earlier story's behavior beyond the shared-layout files listed
  above.
- Inspection was performed read-only against the current codebase; no code was changed
  to produce this intake.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language:
  `typescript`.
- Likely technical direction (to be confirmed, not decided, at planning time): React
  Router v6 `Route.handle` + `useMatches()` for route-driven static labels, plus a small
  context/hook (e.g. `useBreadcrumbLabel(label)`) that a detail page calls once its data
  has loaded, to override the last crumb's label dynamically.

## Out of scope

- See "8. Out of Scope" above.
