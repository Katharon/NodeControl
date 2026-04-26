# File: README.md

# NodeControl

NodeControl is a self-hosted B2B web platform for safely managing and executing Ansible automation through a professional web interface.

The project is designed for IT service providers, managed service providers, system houses, and internal IT teams that want to run Ansible workflows without giving every operator direct terminal, SSH, or full Ansible access.

NodeControl is not intended to replace Ansible. It acts as a control plane around Ansible: customer separation, permissions, inventory management, playbook execution, scheduled jobs, job history, logs, and auditability.

## Product Goal

NodeControl should become a product-grade portfolio project and a realistic self-hosted B2B product.

The main value proposition is:

> Run Ansible workflows safely, repeatedly, and auditably across customer environments without direct terminal work.

## Core Capabilities

Initial product scope:

- Customer management
- User profiles and customer memberships
- External enterprise authentication through OIDC
- Internal NodeControl roles and permissions
- Control nodes
- Managed nodes
- Inventory groups
- Playbooks
- Variable sets
- Manual jobs
- Scheduled jobs / cron jobs
- Job run history
- Job logs
- Audit logs
- Health/status overview
- Docker-based self-hosted deployment

## Target Users

Primary users:

- IT service providers
- Managed service providers
- System houses
- Internal IT departments
- Administrators who already use Ansible but need a safer UI-based workflow

## Non-Goals

NodeControl is intentionally not:

- A full AWX clone
- A replacement for Ansible
- A microservice experiment
- A Keycloak-only application
- A Kubernetes-first product
- A workflow engine for every possible automation technology
- A platform that rebuilds every Ansible feature as a custom UI feature

## High-Level Architecture

```text
Browser
  |
  | HTTPS
  v
Next.js Web UI
  |
  | /api/v1
  v
ASP.NET Core API
  |
  | PostgreSQL
  v
NodeControl Database
  |
  | queued JobRuns / schedules
  v
NodeControl Worker
  |
  | ansible-playbook
  v
Control Node
  |
  | SSH
  v
Managed Nodes
```

External identity providers are integrated through OIDC. Keycloak may be used as a local development/demo provider, but it is not a required product dependency.

## Technology Direction

Backend:

- C#
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- Quartz.NET for scheduling
- Background worker for execution
- xUnit and integration tests

Frontend:

- Next.js
- React
- TypeScript
- MUI
- TanStack Query
- React Hook Form
- Zod

Deployment:

- Docker Compose for MVP/self-hosted installations
- Kubernetes may be added later only if needed

## Development Strategy

Development is performed in vertical slices.

A vertical slice means that one user-visible feature is implemented end-to-end across:

- Domain model
- Application service
- Persistence
- API endpoint
- Frontend screen
- Authorization
- Audit behavior
- Tests

Avoid creating speculative abstractions or empty folders before they are needed.

## First Vertical Slices

1. Project skeleton and documentation
2. OIDC authentication and current user profile
3. Customers and memberships
4. Managed nodes and inventory groups
5. Playbooks and variable sets
6. Manual job execution
7. Scheduled job execution
8. Dashboard and health overview
9. Portfolio/demo polish

## Repository Layout

```text
nodecontrol/
├── AGENTS.md
├── README.md
├── docs/
├── src/
│   ├── backend/
│   └── frontend/
├── tests/
├── deploy/
└── scripts/
```

## Current Status

This repository starts with architecture and product documentation only.

No application code should be generated before the initial project contract is accepted.


---

# File: AGENTS.md

# AGENTS.md

This file defines the working rules for AI agents and human contributors working on NodeControl.

## Project Mission

NodeControl is a self-hosted B2B web platform for running Ansible automation safely, repeatedly, and auditably through a professional web interface.

The product must be useful for IT service providers, managed service providers, system houses, and internal IT teams.

## Core Product Rule

NodeControl is not an Ansible replacement.

NodeControl is a control plane around Ansible:

- Customer separation
- User permissions
- Inventory management
- Playbook management
- Manual execution
- Scheduled execution
- Job history
- Logs
- Auditability

## Hard Architectural Rules

1. Keep the architecture pragmatic.
2. Do not introduce MediatR.
3. Do not introduce CQRS.
4. Do not introduce Event Sourcing.
5. Do not introduce microservices.
6. Do not introduce RabbitMQ, Kafka, MassTransit, or a message bus unless explicitly requested.
7. Do not introduce Kubernetes, Helm, Terraform, Vault, or SAML unless explicitly requested.
8. Do not add speculative abstractions.
9. Do not create empty folders for future features.
10. Do not build an AWX clone.
11. Do not rebuild every Ansible feature as a custom UI feature.
12. Do not add features outside the current vertical slice.

## Backend Architecture Rules

The backend solution should use:

- `NodeControl.Domain`
- `NodeControl.Application`
- `NodeControl.Infrastructure`
- `NodeControl.Api`
- `NodeControl.Worker`

Responsibilities:

### Domain

Contains business concepts only.

Allowed:

- Entities
- Value objects
- Enums
- Domain rules

Forbidden:

- ASP.NET Core dependencies
- EF Core dependencies
- Quartz dependencies
- Ansible process execution
- HTTP concerns

### Application

Contains use cases and application-level abstractions.

Allowed:

- Application services
- DTOs
- Request/response models
- Validation
- Authorization checks
- Interfaces/ports such as `ICurrentUser`, `IClock`, `IAnsibleRunner`, `IJobQueue`, `IAuditWriter`

Forbidden:

- Direct shell execution
- Direct HTTP endpoint definitions
- Direct UI concerns

### Infrastructure

Contains technical implementations.

Allowed:

- EF Core DbContext
- PostgreSQL persistence
- Quartz integration
- File storage
- Ansible process runner
- Logging implementation
- External service implementations

### API

Contains the HTTP boundary.

Allowed:

- Minimal API endpoint groups
- Auth middleware
- Authorization policies
- OpenAPI/Scalar
- Request/response mapping
- Exception handling

Forbidden:

- Direct Ansible execution
- Long-running job execution
- Business logic that belongs in Application

### Worker

Contains background execution.

Allowed:

- Scheduled job handling
- Queued job run processing
- Ansible workspace creation
- `ansible-playbook` execution
- stdout/stderr capture
- JobRun status updates

Forbidden:

- UI concerns
- HTTP endpoint definitions

## Frontend Architecture Rules

The frontend should use:

- Next.js App Router
- React
- TypeScript
- MUI
- TanStack Query
- React Hook Form
- Zod

Rules:

1. Use MUI for UI components.
2. Use TanStack Query for server state.
3. Use React Hook Form and Zod for non-trivial forms.
4. Keep API calls in a dedicated `lib/api` area.
5. Keep auth helpers in a dedicated `lib/auth` area.
6. Avoid storing access tokens in `localStorage`.
7. Prefer HttpOnly cookie-based API sessions where possible.
8. Keep pages thin; move feature logic into feature folders/components.

## Auth Rules

1. NodeControl must be OIDC-first.
2. Keycloak may be used only as a development/demo OIDC provider.
3. NodeControl must not require Keycloak as a product dependency.
4. External identity and internal authorization must remain separate.
5. The external identity provider identifies the user.
6. NodeControl decides what the user may do.
7. Internal authorization is based on NodeControl users, customer memberships, roles, and permissions.
8. Do not couple permissions directly to one provider's group model in the MVP.
9. SAML is a later enterprise feature, not MVP.

## Multi-Tenant Rules

1. Customer separation is mandatory.
2. User-facing resources must be scoped by `CustomerId`.
3. Cross-customer access must be prevented.
4. Cross-customer access must be tested.
5. Never trust a `customerId` from the client without authorization checks.
6. Audit logs must include customer context where applicable.

## Job Execution Rules

1. The API must never execute Ansible directly.
2. All Ansible execution belongs to `NodeControl.Worker`.
3. `Job` is a reusable execution template.
4. `JobRun` is one concrete execution.
5. `Schedule` is a trigger rule that creates scheduled `JobRun` records.
6. Manual and scheduled jobs must use the same execution path.
7. Job logs must be persisted.
8. Job status transitions must be explicit.
9. Failed, cancelled, timed out, and successful runs must be distinguishable.
10. Execution must be auditable.

## Scheduling Rules

1. Use Quartz.NET for scheduling.
2. Keep NodeControl's domain model independent from Quartz internals.
3. `Schedule` is a NodeControl concept.
4. Quartz is an implementation detail.
5. Cron expressions must be validated.
6. Schedules must be enableable and disableable.
7. Scheduled runs must write `JobRun.TriggerType = Scheduled`.

## Ansible Rules

1. NodeControl runs Ansible; it does not replace Ansible.
2. MVP playbooks may start with inline YAML.
3. The architecture must allow artifact-directory playbooks later.
4. Git-backed playbooks are a later feature.
5. Use generated inventories from Managed Nodes and Inventory Groups.
6. Use Variable Sets for extra vars.
7. Store run workspaces and logs per JobRun.
8. Do not attempt to parse all Ansible output semantically in the MVP.
9. Capture exit code, stdout, stderr, duration, status, and error message.

## Security Rules

1. Never store secrets casually.
2. Do not log secrets.
3. Do not expose raw private keys in API responses.
4. Do not store browser access tokens in `localStorage`.
5. Do not allow production Fake Auth.
6. Dangerous operations require authorization checks.
7. Job execution must always be customer-scoped.
8. Audit logs must be append-only.

## Testing Rules

Every feature should include relevant tests.

Minimum test categories:

- Domain unit tests
- Application service tests
- API integration tests
- Authorization tests
- Cross-tenant access tests
- Scheduling tests where relevant
- Worker tests where relevant

Avoid only testing happy paths.

## Documentation Rules

When adding or changing architecture-significant behavior, update the relevant file in `docs/`.

Important docs:

- `docs/PRODUCT.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/AUTH.md`
- `docs/SCHEDULING.md`
- `docs/ANSIBLE_EXECUTION.md`
- `docs/ROADMAP.md`

## Implementation Style

Prefer:

- Small vertical slices
- Clear names
- Simple application services
- Explicit validation
- Explicit authorization
- Minimal but meaningful tests
- Readable code suitable for portfolio review

Avoid:

- Generic repositories everywhere
- Over-abstracted service layers
- Empty interfaces without need
- Excessive folder nesting
- Premature plugin systems
- Premature distributed systems
- Large unrelated commits

## Done Criteria for a Vertical Slice

A vertical slice is done only when:

1. The backend model/use case exists.
2. Persistence works if needed.
3. API endpoint exists if needed.
4. Frontend view/form exists if needed.
5. Authorization is handled.
6. Audit behavior is handled where relevant.
7. Tests exist for critical behavior.
8. Documentation is updated if needed.
9. The project builds.
10. Existing tests pass.


---

# File: docs/PRODUCT.md

# NodeControl Product Definition

## Product Summary

NodeControl is a self-hosted B2B control plane for Ansible automation.

It allows IT teams and service providers to manage customers, nodes, inventories, playbooks, jobs, schedules, job history, logs, and audit trails through a professional web interface.

NodeControl does not replace Ansible. It wraps Ansible in a safer operational model.

## One-Sentence Value Proposition

NodeControl lets IT teams run Ansible workflows safely, repeatedly, and auditably across customer environments without direct terminal work.

## Target Customers

Primary:

- IT service providers
- Managed service providers
- System houses
- Internal IT departments with recurring infrastructure automation tasks

Secondary:

- Small DevOps teams
- Consultants who manage several client environments
- Companies that want Ansible execution without broad shell access

## User Personas

### Service Provider Owner

Needs:

- Control over customer environments
- Clear separation between customers
- Visibility into who executed what
- A product that can be used by staff without direct server access

### Senior Administrator

Needs:

- Reliable playbook execution
- Job history
- Logs
- Schedules
- Safe variable handling
- Quick troubleshooting

### Junior Operator

Needs:

- Simple UI
- Predefined jobs
- Clear warnings
- Limited permissions
- No direct SSH key exposure

### Customer Auditor / Viewer

Needs:

- Read-only visibility
- Job history
- Audit logs
- Evidence of maintenance activities

## Problems NodeControl Solves

1. Direct terminal execution is risky.
2. SSH and Ansible access are often too broad.
3. Job history is fragmented or missing.
4. Recurring maintenance is manual or scripted informally.
5. Customer environments are not always separated cleanly.
6. Auditing who did what is difficult.
7. Junior staff cannot safely execute selected automation tasks.
8. Existing enterprise identity systems should be reused.

## Product Principles

1. Safe by default.
2. Customer-scoped by default.
3. Auditable by default.
4. OIDC-first for enterprise integration.
5. Ansible-native where possible.
6. Simple enough to self-host.
7. Modular enough to extend.
8. Avoid unnecessary enterprise complexity in the MVP.

## MVP Scope

The MVP should include:

- OIDC login with one configured provider
- Internal user profile creation
- Customer management
- Customer memberships with static roles
- Control nodes
- Managed nodes
- Inventory groups
- Inline playbooks
- Variable sets
- Manual jobs
- Scheduled jobs
- Job run history
- Job logs
- Audit logs
- Health/status overview
- Docker Compose deployment

## Post-MVP Scope

Later features may include:

- SAML
- OIDC provider per customer
- Approval workflow
- Git-backed playbooks
- Artifact-directory playbooks
- Secret vault integration
- Notification integrations
- Live log streaming
- Role customization
- Policy engine
- Licensing
- Billing
- Kubernetes deployment
- Multi-control-node dispatching
- Plugin system

## Explicit Non-Goals for MVP

The MVP must not include:

- A complete AWX clone
- Full Ansible UI abstraction
- Dynamic role editor
- Marketplace
- Complex workflow builder
- Billing
- Multi-region deployments
- Kubernetes-first installation
- Event sourcing
- Microservices
- Message bus infrastructure

## Product Differentiation

NodeControl should position itself as:

- Lighter than enterprise automation platforms
- Easier to self-host
- Customer/tenant-oriented
- Suitable for IT service providers
- Focused on safe execution, scheduling, and auditability
- Designed for pragmatic teams that already understand Ansible

## Demo Story

A strong demo should show:

1. Login through enterprise-style SSO.
2. Select a customer.
3. View managed nodes.
4. Open a playbook.
5. Run a job manually.
6. Inspect status and logs.
7. Create a schedule.
8. Show audit logs.
9. Explain customer separation and permissions.


---

# File: docs/ARCHITECTURE.md

# NodeControl Architecture

## Architecture Goal

NodeControl should be modular, testable, maintainable, and product-grade without becoming over-engineered.

The architecture follows pragmatic Clean Architecture principles:

- Domain logic is separated from infrastructure.
- Application use cases coordinate behavior.
- Infrastructure implements technical concerns.
- API exposes HTTP endpoints.
- Worker executes background jobs.

## High-Level System

```text
Browser
  |
  v
Next.js Web UI
  |
  v
ASP.NET Core API
  |
  v
PostgreSQL
  |
  v
NodeControl Worker
  |
  v
ansible-playbook
  |
  v
Control Node
  |
  v
Managed Nodes
```

## Backend Projects

```text
NodeControl.Domain
NodeControl.Application
NodeControl.Infrastructure
NodeControl.Api
NodeControl.Worker
```

## Project Responsibilities

### NodeControl.Domain

Contains the business model.

Examples:

- Customer
- User
- ExternalIdentity
- CustomerMembership
- ControlNode
- ManagedNode
- InventoryGroup
- Playbook
- VariableSet
- Job
- JobRun
- Schedule
- AuditLog

The Domain project must not depend on ASP.NET Core, EF Core, Quartz, or Ansible execution code.

### NodeControl.Application

Contains use cases and application-level abstractions.

Examples:

- CreateCustomer
- AddCustomerMember
- CreateManagedNode
- GenerateInventoryPreview
- CreatePlaybook
- CreateJob
- RunJobManually
- CreateSchedule
- ListAuditLogs

Application services enforce validation, authorization rules, and orchestration.

### NodeControl.Infrastructure

Contains technical implementations.

Examples:

- EF Core DbContext
- PostgreSQL mappings
- Quartz scheduler integration
- File storage
- Ansible process runner
- Audit writer implementation
- Clock implementation

### NodeControl.Api

Contains HTTP boundaries.

Examples:

- Minimal API endpoint groups
- Auth setup
- Authorization policies
- OpenAPI/Scalar
- Error handling
- Request/response mapping

The API must not execute Ansible directly.

### NodeControl.Worker

Contains background execution.

Examples:

- Poll queued JobRuns
- Execute JobRuns
- Host Quartz scheduler
- Create execution workspaces
- Run `ansible-playbook`
- Capture logs
- Update job run status

## Frontend Structure

The frontend uses Next.js App Router.

Main responsibilities:

- Auth-aware layout
- Customer navigation
- Dashboard
- CRUD screens
- Job run screens
- Schedule screens
- Audit screens

Recommended libraries:

- MUI for UI
- TanStack Query for server state
- React Hook Form for forms
- Zod for validation

## Core Domain Concepts

### Customer

A tenant/customer boundary. Most resources are scoped to a Customer.

### User

Internal NodeControl user profile.

### ExternalIdentity

External OIDC identity linked to an internal User.

### CustomerMembership

Defines which User has which Role in which Customer.

### ControlNode

A machine or execution environment from which Ansible is run.

### ManagedNode

A target system managed by Ansible.

### InventoryGroup

A group of ManagedNodes used to generate Ansible inventory.

### Playbook

A reusable automation definition.

### VariableSet

Variables passed to an Ansible job.

### Job

Reusable execution template.

### JobRun

One concrete execution of a Job.

### Schedule

Recurring trigger rule for creating JobRuns.

### AuditLog

Append-only record of important actions.

## Data Flow: Manual Job

```text
User clicks "Run"
  |
API checks permission
  |
API creates JobRun with Status = Queued
  |
Worker picks queued JobRun
  |
Worker builds workspace
  |
Worker runs ansible-playbook
  |
Worker captures stdout/stderr/exit code
  |
Worker updates JobRun
  |
AuditLog records execution
```

## Data Flow: Scheduled Job

```text
Quartz trigger fires
  |
Worker creates JobRun with TriggerType = Scheduled
  |
Same execution pipeline as manual job
```

## Auth Model

NodeControl is OIDC-first.

External identity providers identify the user.

NodeControl manages:

- Internal User
- CustomerMembership
- Role
- Permission

Keycloak is allowed as a development/demo provider only.

## Multi-Tenancy Model

All customer-owned resources must include CustomerId.

Authorization must verify that the current user has access to the requested customer.

Cross-customer access must be tested.

## Persistence

PostgreSQL is the main database.

EF Core is used for persistence.

The database stores:

- Product data
- User profiles
- Memberships
- Job definitions
- Job run metadata
- Audit logs
- Schedule definitions

Large logs and playbook artifacts may be stored on the filesystem in the MVP.

## Execution Storage

Recommended MVP paths:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/
/var/lib/nodecontrol/runs/{jobRunId}/
```

## Avoided Complexity

The MVP avoids:

- Microservices
- Message bus
- Kubernetes deployment
- Dynamic policy engine
- Dynamic role builder
- SAML
- Vault integration
- Git-backed playbooks
- Live log streaming


---

# File: docs/DECISIONS.md

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

## DEC-010: Use Quartz.NET for scheduling

Status: Accepted

Reason:

- Scheduling is a product feature.
- Cron support is needed.
- Misfire handling and trigger management should not be hand-written in the MVP.

## DEC-011: Keep scheduling domain independent from Quartz

Status: Accepted

NodeControl owns the Schedule domain model.

Quartz is an implementation detail.

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


---

# File: docs/ROADMAP.md

# NodeControl Roadmap

## Development Strategy

NodeControl is developed in vertical slices.

Each slice should deliver one visible capability end-to-end.

Avoid building large technical layers before they are needed.

## Phase 0: Project Contract and Skeleton

Goal:

The repository has clear documentation, architecture rules, and a startable skeleton.

Deliverables:

- README.md
- AGENTS.md
- PRODUCT.md
- ARCHITECTURE.md
- DECISIONS.md
- ROADMAP.md
- AUTH.md
- SCHEDULING.md
- ANSIBLE_EXECUTION.md
- CODEX_WORKFLOW.md
- Empty backend solution skeleton
- Empty frontend app skeleton
- Docker Compose development base

No business feature yet.

## Phase 1: Auth and Current User

Goal:

A user can authenticate through a development OIDC provider and the API can identify the current user.

Deliverables:

- Dev OIDC configuration
- Keycloak dev provider or fake auth mode
- User auto-provisioning
- ExternalIdentity mapping
- `GET /api/v1/me`
- Frontend display of current user
- Production guard against Fake Auth

## Phase 2: Customers and Memberships

Goal:

Customer isolation exists.

Deliverables:

- Customer CRUD
- CustomerMembership
- Static roles
- Permission checks
- Customer-scoped queries
- Cross-tenant integration tests
- Frontend customer list and detail page

## Phase 3: Nodes and Inventory Groups

Goal:

A customer can define target systems and groups.

Deliverables:

- ControlNode
- ManagedNode
- InventoryGroup
- InventoryGroupNode
- Inventory preview
- Frontend tables/forms for nodes and groups

## Phase 4: Playbooks and Variable Sets

Goal:

A customer can define automation inputs.

Deliverables:

- Inline YAML playbook
- VariableSet as YAML/JSON
- Basic validation
- Frontend editor fields
- File storage structure prepared

## Phase 5: Manual Job Execution

Goal:

A user can run a job manually and inspect the result.

Deliverables:

- Job
- JobRun
- Worker execution pipeline
- Ansible workspace generation
- Inventory generation
- VariableSet file generation
- `ansible-playbook` execution
- stdout/stderr capture
- JobRun status updates
- AuditLog entry
- Frontend run button and JobRun detail page

This is the most important MVP milestone.

## Phase 6: Scheduled Jobs / Cronjobs

Goal:

A job can run cyclically.

Deliverables:

- Schedule entity
- Cron expression validation
- Time zone support
- Quartz integration
- Next run preview
- Enable/disable schedule
- Scheduled JobRun creation
- Frontend schedule management

## Phase 7: Dashboard and Health

Goal:

Operators understand current system state.

Deliverables:

- Dashboard cards
- Recent JobRuns
- Failed JobRuns
- Active Schedules
- ControlNode health
- ManagedNode status
- Basic system health endpoint

## Phase 8: Security and Hardening

Goal:

The MVP becomes more production-like.

Deliverables:

- Authorization review
- Cross-tenant test coverage
- Safer secret handling
- Dangerous operation warnings
- Log redaction basics
- Production configuration validation
- Docker Compose production profile

## Phase 9: Portfolio and Sales Demo

Goal:

NodeControl can be presented convincingly.

Deliverables:

- Demo data
- Demo script
- Screenshots
- Architecture diagram
- README polish
- Sales-oriented explanation
- Interview explanation guide

## Post-MVP Features

Potential later features:

- SAML
- OIDC provider per customer
- Git-backed playbooks
- Artifact-directory playbooks
- Approval workflow
- Notification integrations
- Secret vault integration
- Role editor
- Policy engine
- Live log streaming
- Multi-control-node dispatching
- Kubernetes deployment
- Billing/licensing


---

# File: docs/AUTH.md

# Authentication and Authorization

## Auth Goal

NodeControl must integrate well into real companies.

Many potential customers already have identity management systems such as:

- Microsoft Entra ID / Azure AD
- Okta
- Keycloak
- Google Workspace
- Other OIDC providers
- SAML providers later

NodeControl must not require customers to run a specific identity provider.

## Main Decision

NodeControl is OIDC-first.

Keycloak is allowed as a development/demo provider, but it is not a required product dependency.

## Important Separation

External identity and internal authorization are separate.

The external identity provider answers:

> Who is this user?

NodeControl answers:

> What is this user allowed to do inside NodeControl?

## Internal Auth Model

NodeControl stores:

- User
- ExternalIdentity
- CustomerMembership
- Role
- Permission

## User

Represents a NodeControl user profile.

Fields:

- Id
- DisplayName
- Email
- IsActive
- CreatedAt
- LastLoginAt

## ExternalIdentity

Links a NodeControl User to an external provider subject.

Fields:

- Id
- UserId
- Provider
- Subject
- EmailAtLogin
- DisplayNameAtLogin
- CreatedAt
- LastSeenAt

The pair `(Provider, Subject)` must be unique.

## CustomerMembership

Defines access to a customer.

Fields:

- Id
- CustomerId
- UserId
- Role
- IsActive
- CreatedAt

## MVP Roles

Static roles:

- Owner
- Admin
- Operator
- Viewer
- Auditor

Dynamic roles are post-MVP.

## MVP Permissions

Suggested permissions:

- ViewCustomer
- ManageCustomer
- ViewNodes
- ManageNodes
- ViewPlaybooks
- ManagePlaybooks
- RunJobs
- ManageSchedules
- ViewJobRuns
- ViewAuditLogs
- ManageMemberships

## Login Flow

Recommended MVP flow:

```text
1. User opens the Next.js frontend.
2. Frontend calls GET /api/v1/me.
3. API returns 401 if no session exists.
4. Frontend redirects to /auth/login.
5. API starts OIDC Authorization Code flow.
6. External identity provider authenticates the user.
7. API receives callback.
8. API creates a secure HttpOnly session cookie.
9. API creates or updates User and ExternalIdentity.
10. Frontend can call /api/v1 endpoints with the session cookie.
```

## Token Storage Rule

Do not store access tokens in browser `localStorage`.

Prefer HttpOnly cookies controlled by the backend.

## Local Development Modes

### Keycloak Dev Mode

Use Keycloak as a local OIDC provider.

Example dev users:

- admin@nodecontrol.local
- operator@nodecontrol.local
- viewer@nodecontrol.local

Keycloak is only a test provider.

### Fake Auth Mode

Fake Auth may be used for faster local development.

Rules:

- Fake Auth must never run in production.
- Production startup must fail if Fake Auth is enabled.
- Fake Auth should provide a fixed development user.

## Production Auth Configuration

Production requires:

- OIDC Authority
- Client ID
- Client Secret if applicable
- Callback URL
- Allowed issuer validation
- Secure cookies
- HTTPS
- Production Fake Auth guard

## Authorization Rules

1. All customer-scoped endpoints must verify membership.
2. Role permissions must be checked server-side.
3. Frontend checks are usability only, not security.
4. Cross-tenant access must be tested.
5. Disabled users must not access the system.
6. Disabled memberships must not grant access.

## Post-MVP Auth Features

Potential later features:

- SAML
- OIDC provider per customer
- SCIM provisioning
- Group claim mapping
- Role mapping from external groups
- API tokens
- Service accounts
- Approval workflow permissions


---

# File: docs/SCHEDULING.md

# Scheduling

## Scheduling Goal

NodeControl must support cyclic execution of Ansible jobs.

Examples:

- Run update checks every night.
- Validate backups every morning.
- Restart selected services every Sunday.
- Check certificate expiration daily.
- Apply a baseline configuration every week.

## Main Decision

Use Quartz.NET for scheduling.

Reason:

- Cron expressions are a core requirement.
- Scheduling is a product feature.
- Building reliable cron/misfire behavior manually is unnecessary complexity.

## Important Domain Separation

Quartz is an implementation detail.

NodeControl owns the domain model:

- Job
- JobRun
- Schedule

## Concepts

### Job

A reusable execution template.

Example:

```text
Run "Update packages" playbook on "Production Linux Servers" with "Default Maintenance Vars".
```

### JobRun

One concrete execution of a Job.

Example:

```text
JobRun #123 started at 2026-04-26 02:00 and failed with exit code 2.
```

### Schedule

A recurring trigger rule that creates JobRuns.

Example:

```text
Run Job #5 every Monday at 02:00 Europe/Vienna.
```

## Schedule Fields

Initial fields:

- Id
- CustomerId
- JobId
- Name
- CronExpression
- TimeZone
- IsEnabled
- MisfirePolicy
- LastTriggeredAt
- NextRunAt
- CreatedAt
- UpdatedAt

## Trigger Types

JobRun should distinguish:

- Manual
- Scheduled
- System

## Execution Model

Scheduled and manual jobs must use the same execution path.

```text
Manual Run:
User -> API -> JobRun Queued -> Worker -> Execute

Scheduled Run:
Quartz -> Worker -> JobRun Queued -> Worker -> Execute
```

The actual Ansible execution logic must not be duplicated.

## Cron Validation

When a Schedule is created or updated:

1. Validate cron expression.
2. Validate time zone.
3. Calculate next run preview.
4. Store normalized configuration.
5. Register/update Quartz trigger.

## Misfire Policy

MVP can start simple.

Possible values:

- IgnoreMisfire
- FireOnceNow
- SkipMissedRuns

Recommended MVP default:

```text
SkipMissedRuns
```

Reason:

If NodeControl was offline, it should not unexpectedly run a large backlog of maintenance jobs.

## Concurrency Rules

MVP should prevent the same Job from running concurrently unless explicitly allowed later.

Initial rule:

```text
A Job cannot have two active JobRuns at the same time.
```

Potential future field:

```text
Job.AllowConcurrentRuns
```

## Worker Responsibility

The Worker:

- Hosts Quartz scheduler
- Reacts to scheduled triggers
- Creates scheduled JobRuns
- Executes queued JobRuns
- Updates statuses
- Writes logs
- Writes audit entries

## API Responsibility

The API:

- Creates schedules
- Updates schedules
- Enables/disables schedules
- Shows schedule previews
- Does not execute jobs directly

## Frontend Requirements

The schedule UI should show:

- Name
- Job
- Cron expression
- Time zone
- Enabled/disabled state
- Next run time
- Last triggered time
- Preview of upcoming runs

## Post-MVP Scheduling Features

Potential later features:

- Calendar exclusions
- Maintenance windows
- Approval before scheduled run
- Notification on failure
- Retry policy per job
- Maximum runtime per job
- Concurrency configuration
- Schedule templates


---

# File: docs/ANSIBLE_EXECUTION.md

# Ansible Execution

## Execution Goal

NodeControl must execute Ansible playbooks safely, repeatably, and auditably.

NodeControl does not replace Ansible. It prepares inputs, starts Ansible, captures outputs, and stores results.

## Main Execution Rule

The API must never execute Ansible directly.

Only `NodeControl.Worker` executes Ansible.

## Core Concepts

### Control Node

The system or execution environment where Ansible runs.

### Managed Node

A target host managed by Ansible.

### Inventory Group

A group of Managed Nodes used to generate an Ansible inventory.

### Playbook

An automation definition.

### VariableSet

Variables passed to the playbook.

### Job

A reusable execution template.

### JobRun

One concrete execution.

## MVP Playbook Storage

The database stores playbook metadata.

The filesystem stores playbook content/artifacts.

Suggested path:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/
```

For inline YAML playbooks:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/site.yml
```

Later artifact-directory playbooks may include:

```text
site.yml
roles/
templates/
files/
group_vars/
host_vars/
```

## JobRun Workspace

Each JobRun gets its own workspace.

Suggested path:

```text
/var/lib/nodecontrol/runs/{jobRunId}/
├── inventory.yml
├── vars.yml
├── playbook/
│   └── site.yml
├── stdout.log
└── stderr.log
```

## Execution Command

Conceptually:

```text
ansible-playbook -i inventory.yml playbook/site.yml -e @vars.yml
```

The exact implementation belongs to Infrastructure/Worker.

## MVP Captured Data

For every JobRun, capture:

- Status
- StartedAt
- FinishedAt
- Duration
- ExitCode
- Stdout log path
- Stderr log path
- ErrorMessage
- TriggerType
- TriggeredByUserId if manual
- ScheduleId if scheduled

## JobRun Statuses

Initial statuses:

- Queued
- Running
- Succeeded
- Failed
- Cancelled
- TimedOut

## Inventory Generation

MVP inventory generation is based on:

- Customer
- InventoryGroup
- ManagedNodes

Example conceptual output:

```yaml
all:
  children:
    webservers:
      hosts:
        web-01:
          ansible_host: 10.0.0.10
```

Do not overbuild dynamic inventory in the MVP.

## Variable Sets

VariableSets may be stored as YAML or JSON content.

They are written into the JobRun workspace as `vars.yml`.

Sensitive values need special care.

MVP rule:

- Do not log variable content.
- Do not expose secret variable values through API responses.
- Prefer not implementing advanced secret storage until the execution path works.

## Dangerous Execution Concerns

Ansible can change real systems.

NodeControl must therefore provide:

- Clear target preview
- Customer scoping
- Permission checks
- Audit logs
- Job history
- Logs
- Explicit run action

Post-MVP features:

- Approval workflow
- Dry-run/check mode
- Diff mode
- Tags/skip-tags
- Limit
- Execution confirmation
- Secret vault integration

## Output Parsing

MVP should not attempt full semantic parsing of all Ansible output.

MVP captures:

- stdout
- stderr
- exit code
- final status
- duration

Post-MVP may add:

- JSON callback plugin
- task-level event timeline
- per-host result summary
- changed/failed/unreachable counts

## Cancellation

MVP should model cancellation, but actual process cancellation can be implemented after basic execution works.

Desired later behavior:

- User cancels JobRun.
- Worker terminates process.
- Status becomes Cancelled.
- AuditLog records cancellation.

## Timeouts

Jobs should have a default timeout.

If exceeded:

- Worker terminates execution.
- JobRun status becomes TimedOut.
- Logs are retained.
- AuditLog records timeout.

## Post-MVP Features

Potential later improvements:

- Git-backed playbooks
- Artifact-directory upload
- Ansible roles support in UI
- Vault integration
- Secret masking
- Check mode
- Diff mode
- Tags/skip-tags
- Limit hosts
- Live logs via WebSocket/SSE
- Structured Ansible event parsing


---

# File: docs/TESTING.md

# Testing Strategy

## Testing Goal

NodeControl must be credible as a product-grade portfolio project.

Tests should prove:

- Domain rules work.
- Application use cases work.
- API endpoints enforce authorization.
- Customer isolation works.
- Job execution status transitions work.
- Scheduling creates JobRuns correctly.

## Test Types

### Domain Tests

Test pure domain rules.

Examples:

- JobRun status transitions
- Schedule enable/disable behavior
- Customer status rules
- Role permission mapping

### Application Tests

Test use cases without HTTP.

Examples:

- Create customer
- Add membership
- Create job
- Run job manually
- Create schedule
- Reject unauthorized access

### API Integration Tests

Test HTTP endpoints with real persistence where possible.

Use PostgreSQL through Testcontainers when feasible.

Examples:

- `GET /api/v1/me`
- Customer CRUD
- Cross-tenant access rejection
- JobRun creation
- Schedule creation

### Worker Tests

Test execution logic around:

- JobRun picking
- Workspace generation
- Status updates
- Failure handling
- Timeout handling

The actual Ansible process can be abstracted behind a test double for most tests.

### Frontend Tests

Use frontend tests selectively.

Focus on:

- Important forms
- Permission-dependent rendering
- Job run views
- Schedule forms

### E2E Tests

Post-MVP.

Use Playwright later for:

- Login flow
- Create customer
- Create node
- Create playbook
- Run job
- View logs

## Cross-Tenant Test Requirement

Every customer-scoped API area should include tests proving that User A cannot access Customer B resources.

This is mandatory for credibility.

## Security Test Requirements

Test:

- Disabled users cannot access.
- Users without membership cannot access customer resources.
- Viewers cannot mutate resources.
- Operators cannot manage memberships.
- Fake Auth cannot start in production.

## Test Naming

Use readable test names.

Prefer:

```text
RunJobManually_ShouldCreateQueuedJobRun_WhenUserHasRunJobsPermission
```

Avoid unclear names.

## Test Data

Use explicit test data builders when repeated setup becomes noisy.

Do not introduce complex test frameworks before needed.

## CI Goal

Eventually, CI should run:

- Backend build
- Backend tests
- Frontend lint
- Frontend type check
- Frontend tests


---

# File: docs/DEPLOYMENT.md

# Deployment

## Deployment Goal

NodeControl should be easy to run as a self-hosted B2B product.

The MVP target is Docker Compose.

Kubernetes may be added later.

## MVP Deployment Components

Required services:

- NodeControl API
- NodeControl Worker
- NodeControl Web
- PostgreSQL

Development-only service:

- Keycloak dev provider

Optional later services:

- Reverse proxy
- Object/file storage
- Metrics stack
- Log aggregation
- Secret vault

## Deployment Modes

### Development

Development may include:

- PostgreSQL
- Keycloak
- API
- Worker
- Web

### Production-like Selfhost

Production-like setup should include:

- PostgreSQL
- API
- Worker
- Web
- Reverse proxy / TLS termination
- External OIDC provider

### Demo

Demo setup should include:

- Seed data
- Dev identity provider
- Example customer
- Example managed nodes
- Example playbook
- Example schedule

## Production Requirements

Production configuration must validate:

- Fake Auth is disabled.
- OIDC is configured.
- HTTPS/cookie settings are safe.
- Database connection exists.
- File storage paths are configured.
- Worker is enabled.
- Logging is configured.

## File Storage

MVP file storage paths:

```text
/var/lib/nodecontrol/playbooks/
/var/lib/nodecontrol/runs/
```

These paths must be persisted with Docker volumes.

## Backup Considerations

Important data:

- PostgreSQL database
- Playbook artifact storage
- JobRun logs
- Configuration files

## Post-MVP Deployment Features

Potential later features:

- Kubernetes manifests
- Helm chart
- External object storage
- Automated backups
- Observability stack
- High availability
- Multi-worker execution


---

# File: docs/CODEX_WORKFLOW.md

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
- Add Quartz schedule registration.
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


---

