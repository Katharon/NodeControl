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
- Scheduling infrastructure dependencies
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
- Database-backed schedule polling integration
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

1. Use database-backed Worker polling for the current scheduling implementation.
2. Keep NodeControl's domain model independent from scheduler infrastructure internals.
3. `Schedule` is a NodeControl concept.
4. Scheduler infrastructure is an implementation detail.
5. Cron expressions must be validated.
6. Schedules must be enableable and disableable.
7. Scheduled runs must write `JobRun.TriggerType = Scheduled`.
8. The API manages schedule definitions only; it must not create due scheduled runs or execute jobs directly.

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
