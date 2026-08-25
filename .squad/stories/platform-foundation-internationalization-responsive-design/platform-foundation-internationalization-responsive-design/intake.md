# Story 02 — Platform Foundation: Internationalization & Responsive Design

## Story intake

### Feature

- **Feature name (display):** Platform Foundation: Internationalization & Responsive Design
- **Feature slug (folder under `plans/`):** `platform-foundation-internationalization-responsive-design`

### Work Item

- **Work item id:** `02`
- **Work item type:** `User Story`
- **Story id:** `02`
- **Status:** `New`
- **Assignee:** ``
- **Labels:** `platform-foundation,i18n,rtl,responsive`

### Title

Platform Foundation: Internationalization & Responsive Design

---

## User Story

As a user, I want the CRM platform to support Arabic and English with responsive layouts, so that I can use the platform consistently across languages, devices, and screen sizes.

---

## Description

Establish the base frontend foundation for Arabic/English internationalization, RTL/LTR direction handling, and responsive layouts before CRM feature screens are implemented.

Story 01 — Initial Project Setup has already been completed and must remain unchanged except where a minimal compatibility change is absolutely required.

This story builds on the existing React + TypeScript + Vite + Tailwind CSS v3 architecture established in Story 01.

The objective is to create a minimal, reusable frontend platform foundation that future stories can build upon without reworking the application shell.

The implementation should provide:

- Arabic and English application language support.
- Centralized and structured translation resources.
- A reusable translation mechanism for future feature modules.
- Language switching.
- Language persistence across page reloads where appropriate.
- Dynamic RTL/LTR direction handling.
- Arabic uses RTL.
- English uses LTR.
- Direction changes dynamically when the language changes.
- Responsive layout foundations.
- Reuse of the existing Tailwind CSS v3 responsive breakpoint system.
- Minimal shared layout/UI primitives only where genuinely required.
- Preservation of the existing HomePage functionality.
- Reusable architecture for Stories 03+.

Before implementation:

1. Inspect the repository and verify that Story 01 is actually implemented.
2. Read `.squad/config.yaml`.
3. Read `.squad/README.md`.
4. Read the Story 01 story/intake/plan files available under `.squad/`.
5. Inspect the actual frontend structure and configuration.
6. Inspect `frontend/package.json` before deciding on any i18n dependency or validation command.
7. Identify the exact files that need to be created or modified for Story 02.
8. Do not recreate or overwrite Story 01 files unnecessarily.
9. Resolve conflicts between Story 02 requirements and the existing Story 01 implementation before coding.
10. Follow the same Squad planning and implementation workflow used for Story 01.
11. Clearly distinguish verified existing files from files that Story 02 needs to create.

---

## Acceptance Criteria

1. The frontend supports Arabic and English as application languages.
2. Translation keys are centralized and structured so future feature modules can add translations without duplicating the i18n infrastructure.
3. Switching the application language changes the displayed UI language.
4. Arabic correctly switches the document/application direction to RTL.
5. English correctly uses LTR.
6. RTL/LTR direction changes dynamically when the language changes.
7. Language and direction state remain consistent across the application shell.
8. Language selection persists across page reloads where appropriate.
9. Responsive behavior uses the existing Tailwind CSS v3 configuration.
10. Existing Tailwind responsive breakpoints are reused wherever possible.
11. Responsive utilities and layout patterns can be reused by future feature screens.
12. Minimal reusable layout/UI primitives are established only where genuinely required.
13. The existing HomePage remains functional after the implementation.
14. Existing Story 01 functionality continues to work.
15. The implementation follows the existing React + TypeScript + Vite + Tailwind CSS v3 architecture.
16. Zustand remains the application's state-management solution.
17. Axios remains the existing HTTP client.
18. No hard-coded duplicated translation logic is introduced.
19. No CRM business functionality is introduced.
20. The backend is not modified unless absolutely required for Story 02.
21. No authentication or authorization functionality is introduced.
22. The frontend builds successfully using the actual build script defined in `frontend/package.json`.
23. Existing lint/type/test checks pass if they are actually configured in `frontend/package.json`.
24. The application starts successfully in development mode using the actual configured script.
25. Arabic → RTL behavior is verified.
26. English → LTR behavior is verified.
27. Responsive behavior is verified at common desktop, tablet, and mobile viewport widths.
28. No Story 03 or subsequent story functionality is introduced.

---

## Attachments

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None | No attachments required |

---

## Dependencies

- **Blocked by / related ids:** Story 01 — Initial Project Setup
- **Depends on code areas or other stories:** Existing frontend implementation created by Story 01.

---

## Extra Notes

- Story 01 is completed and must remain unchanged except where a minimal compatibility change is absolutely required.
- Work on the existing branch: `feature/story-02-platform-foundation`.
- Do not create a new branch.
- Do not start Story 03.
- Follow the same Squad workflow and project conventions used for Story 01.
- After Story 02 is completed and validated, stop.
- Do not replace the existing routing architecture.
- Do not replace Zustand.
- Do not replace Axios.
- Do not replace Tailwind CSS.
- Do not rebuild the existing HomePage.
- Do not introduce a full design system.
- Do not introduce a complex UI component library.
- Prefer the smallest implementation that satisfies the acceptance criteria.

---

## Technical Hints

- **Repository/root:** `.`
- **Frontend root:** `frontend/`
- **Primary language:** TypeScript
- **Frontend:** React + TypeScript + Vite
- **Styling:** Tailwind CSS v3
- **State management:** Zustand
- **Routing:** React Router
- **HTTP client:** Axios
- **Existing project configuration:** Story 01 implementation is the source of truth.
- Keep i18n implementation simple and modular.
- Centralize direction handling rather than implementing RTL/LTR independently in individual components.
- Use the existing Tailwind responsive breakpoint system.
- Do not introduce a separate responsive framework.
- Do not introduce another state-management library.
- Do not introduce another CSS framework or UI library.

---


````text
# Planning / Validation Rules

## 1. Planning Objective

You are planning Story 02:

Platform Foundation — Internationalization & Responsive Design.

The purpose of this planning run is to generate an implementation-ready plan for Story 02 only.

Story 01 is already completed and is the source of truth for the existing frontend foundation.

The planner MUST inspect the actual repository structure before making repository-specific implementation decisions.

Do not invent architecture, files, symbols, dependencies, scripts, configuration, or framework libraries.

The final plan must be internally consistent with the actual repository state.

---

## 2. Work Item Context

The planner must preserve the Story / Work Item identity supplied by the intake.

Do not change, recreate, rename, or reinterpret the Work Item.

The final plan must clearly identify:

- Work Item / Story
- Work Item ID, if provided by the intake or planning context
- Story title
- Story scope
- Out-of-scope work

Do not invent a Work Item ID if one was not supplied.

The plan must not silently merge Story 02 with Story 01 or Story 03.

---

## 3. Repository Root

The repository root is:

```text
.
````

The frontend application root is:

```text
frontend/
```

All frontend repository paths MUST be resolved relative to:

```text
frontend/
```

Do not generate frontend paths relative to the repository root unless the file actually exists there.

---

## 4. Repository Is the Source of Truth

The repository, not the Story description, is the source of truth for:

* existing files
* existing directories
* existing components
* existing stores
* existing dependencies
* existing scripts
* existing configuration
* existing symbols
* existing framework integrations
* existing testing infrastructure

The Story describes desired behavior and capabilities.

It does NOT define filenames, folder structure, libraries, symbols, or implementation architecture.

Never convert a conceptual requirement into an assumed existing file.

---

## 5. Mandatory Repository Inspection

Before drafting the implementation plan, the planner MUST inspect the actual repository.

The planner MUST:

1. Inspect the repository map.
2. Identify the actual frontend structure.
3. Verify relevant existing files.
4. Read relevant existing files before proposing modifications.
5. Search for equivalent implementations when an expected responsibility is not found at an assumed location.
6. Inspect `frontend/package.json` before mentioning dependencies or npm scripts.
7. Inspect the actual Tailwind configuration before proposing Tailwind changes.
8. Inspect the actual application entry and root component before proposing application-level integration.
9. Inspect the actual Zustand store before proposing language state changes, if Zustand is present.
10. Inspect the actual routing structure before proposing route changes.
11. Inspect the existing HomePage before proposing changes to it.
12. Inspect existing i18n/language implementation, if any.
13. Inspect existing tests and validation configuration before proposing tests or validation commands.

The planner MUST NOT rely only on the intake or Story text for repository-specific implementation decisions.

---

## 6. Repository Discovery Candidates

The following paths are inspection candidates only.

They MUST NOT be treated as existing files unless repository inspection confirms that they exist:

```text
frontend/package.json
frontend/index.html
frontend/src/main.tsx
frontend/src/App.tsx
frontend/src/pages/HomePage.tsx
frontend/src/store/useAppStore.ts
frontend/src/routes/AppRouter.tsx
frontend/src/api/httpClient.ts
frontend/tailwind.config.ts
frontend/tailwind.config.js
frontend/tailwind.config.cjs
frontend/postcss.config.js
frontend/postcss.config.cjs
frontend/vite.config.ts
frontend/tsconfig.json
frontend/tsconfig.app.json
frontend/tsconfig.node.json
```

These paths are examples based on the known repository map and MUST still be verified.

If an expected path does not exist, search the repository for the actual equivalent.

Do not report an example path as an implementation failure merely because it does not exist.

---

## 7. Existing Files vs New Files

Every file in the final plan MUST have exactly one operation:

```text
CREATE
MODIFY
NO CHANGE
```

### MODIFY

Use MODIFY only when the planner has verified that the exact file exists.

### CREATE

Use CREATE when:

* the required file does not currently exist, AND
* Story 02 genuinely requires a new file.

Absence alone does NOT justify CREATE.

### NO CHANGE

Use NO CHANGE when an existing file was inspected and does not require modification.

Never list a nonexistent file as MODIFY.

Never list an existing file as CREATE.

---

## 8. No Invented File Names