# Codex Workflow

## Purpose

This document explains how Codex/AI agents should work on NodeControl.

The goal is to avoid oversized prompts, uncontrolled rewrites, and speculative architecture.

## General Rule

Work in small vertical slices.

Do not ask Codex to build the entire product in one prompt.

## Recommended Workflow

1. Read `README.md`.
2. Read `AGENTS.md`.
3. Read relevant docs in `docs/`.
4. Work on one vertical slice only.
5. Make minimal required changes.
6. Run relevant tests/build commands.
7. Update docs if the architecture or behavior changes.

## Prompt Pattern

Use prompts like:

```text
Implement Slice X only.

Read:
- AGENTS.md
- docs/ARCHITECTURE.md
- docs/ROADMAP.md
- docs/[relevant].md

Scope:
[exact feature]

Do:
[specific tasks]

Do not:
[explicit exclusions]

Done when:
[build/test/behavior checklist]
```

## Good Codex Tasks

Good:

- Create backend solution skeleton.
- Add Customer entity and minimal API endpoints.
- Add current user endpoint.
- Implement CustomerMembership authorization.
- Add inventory preview.
- Add manual JobRun creation.
- Add database-polled schedule creation.
- Add frontend customer list page.

Bad:

- Build the whole application.
- Implement all auth and all jobs and all UI.
- Add enterprise features.
- Add Kubernetes.
- Add a plugin system.
- Refactor everything.
- Make it production-ready without a defined scope.

## Suggested Agent Roles

### Architecture Agent

Checks:

- Architecture rules
- Over-engineering
- Clean boundaries
- Product alignment

### Backend Agent

Implements:

- Domain
- Application services
- EF Core
- Minimal API endpoints
- Tests

### Frontend Agent

Implements:

- Next.js pages
- MUI components
- Forms
- API hooks
- Frontend validation

### Worker Agent

Implements:

- JobRun execution
- Workspace creation
- Ansible runner
- Scheduling integration
- Logs/status updates

### Security Agent

Reviews:

- Auth
- Authorization
- Customer separation
- Secret exposure
- Dangerous execution paths

### Testing Agent

Adds:

- Unit tests
- Integration tests
- Cross-tenant tests
- Worker tests

### Documentation Agent

Updates:

- README
- Architecture docs
- Roadmap
- Demo script

## Subagent Coordination Rule

Do not split work into subagents until the task boundaries are clear.

A useful split is:

- Backend Agent implements API/use case.
- Frontend Agent implements UI.
- Testing Agent adds coverage.
- Architecture/Security Agent reviews.

Do not let multiple agents modify the same files at the same time unless the task is explicitly coordinated.

## Initial Codex Task Order

1. Create documentation and repository skeleton.
2. Create backend solution skeleton.
3. Create frontend skeleton.
4. Add Docker Compose development services.
5. Add Auth + Current User slice.
6. Add Customer + Membership slice.
7. Add Nodes + Inventory slice.
8. Add Playbook + VariableSet slice.
9. Add Manual JobRun slice.
10. Add Schedule slice.

## Done Criteria for Codex Tasks

A task is done when:

- It stays within scope.
- It follows `AGENTS.md`.
- It does not introduce forbidden technologies.
- It builds.
- Relevant tests pass or are clearly documented as not yet available.
- Docs are updated if needed.
- No unrelated rewrites were made.
