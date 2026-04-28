# Architecture Decision Record

This document stores important project decisions.

## DEC-001: NodeControl is an Ansible control plane, not an Ansible replacement

Status: Accepted

NodeControl wraps Ansible with customer separation, permissions, scheduling, job history, logs, and auditability.

It does not rebuild every Ansible feature as a custom UI feature.

## DEC-002: Use a monorepo

Status: Accepted

Backend, frontend, deployment files, scripts, and documentation live in one repository.

Reason:

- Easier development
- Easier Codex context
- Easier demo setup
- Easier portfolio review

## DEC-003: Use pragmatic Clean Architecture

Status: Accepted

Backend projects:

- Domain
- Application
- Infrastructure
- Api
- Worker

Reason:

- Clean separation without excessive complexity
- Good testability
- Easy to explain in interviews
- Avoids microservice over-engineering

## DEC-004: Use ASP.NET Core Minimal APIs

Status: Accepted

The API uses Minimal APIs instead of MVC controllers.

Reason:

- Less ceremony
- Good fit for small endpoint groups
- Clear and modern ASP.NET Core style

## DEC-005: No MediatR/CQRS/Event Sourcing in MVP

Status: Accepted

Reason:

- The project does not need that complexity.
- Vertical slices can be implemented with explicit application services.
- The goal is a product, not an architecture showcase.

## DEC-006: OIDC-first authentication

Status: Accepted

NodeControl integrates external identity providers through OIDC.

Reason:

- Better fit for B2B customers
- Works with providers like Entra ID, Okta, Keycloak, Google Workspace
- Avoids forcing customers to run NodeControl's preferred identity provider

## DEC-007: Keycloak is not a required product dependency

Status: Accepted

Keycloak may be used for local development, demo, and self-host testing.

NodeControl must also support other OIDC providers.

## DEC-008: Separate external identity from internal authorization

Status: Accepted

The external provider identifies the user.

NodeControl decides what the user may do.

Internal authorization is based on:

- User
- ExternalIdentity
- CustomerMembership
- Role
- Permission

## DEC-009: Use static roles in the MVP

Status: Accepted

MVP roles:

- Owner
- Admin
- Operator
- Viewer
- Auditor

Dynamic role management is postponed.

Reason:

- Dynamic RBAC is a separate product feature.
- Static roles are easier to secure and test.

## DEC-010: Start with database-polled schedules

Status: Accepted

Reason:

- Scheduling is a product feature.
- Cron support is needed.
- The current implementation keeps the MVP simple by polling due active schedules from `NodeControl.Worker`.
- Quartz.NET remains a possible later implementation if richer misfire handling or clustering becomes necessary.

## DEC-011: Keep scheduling domain independent from Quartz

Status: Accepted

NodeControl owns the Schedule domain model.

Any scheduler implementation is an infrastructure detail.

## DEC-012: API must not execute Ansible

Status: Accepted

The API creates JobRuns and validates permissions.

The Worker executes jobs.

Reason:

- Avoid long-running HTTP requests
- Isolate dangerous execution logic
- Improve reliability
- Easier testing

## DEC-013: Use PostgreSQL as the coordination point in MVP

Status: Accepted

Queued JobRuns are persisted in PostgreSQL.

No message bus is used in MVP.

Reason:

- Simpler deployment
- Good enough for MVP
- Avoids unnecessary distributed infrastructure

## DEC-014: Use MUI for frontend UI

Status: Accepted

Reason:

- NodeControl is an admin/operations UI
- MUI provides strong components for dashboards, forms, dialogs, tables, and navigation
- Faster MVP development than designing every component from scratch

## DEC-015: Use Next.js for the frontend

Status: Accepted

Reason:

- Strong React framework
- Good routing/layout model
- Professional portfolio value
- Works well for dashboard-style applications

## DEC-016: Store playbook metadata in DB and artifacts on filesystem

Status: Accepted

Reason:

- Real Ansible playbooks may contain multiple files, roles, templates, and assets.
- The database should store metadata and references.
- The filesystem can store playbook artifacts and run workspaces in MVP.

## DEC-017: Git-backed playbooks are post-MVP

Status: Accepted

Reason:

- Valuable but not required for the first working product.
- Adds complexity around credentials, sync, branches, and updates.

## DEC-018: SAML is post-MVP

Status: Accepted

Reason:

- OIDC covers the first professional integration path.
- SAML may matter for enterprise customers later.
