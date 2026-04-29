# NodeControl Architecture

## Architecture Goal

NodeControl should be modular, testable, maintainable, and product-grade without becoming over-engineered.

The architecture follows pragmatic Clean Architecture principles:

- Domain logic is separated from infrastructure.
- Application use cases coordinate behavior.
- Infrastructure implements technical concerns.
- API exposes HTTP endpoints.
- Worker executes background jobs.

## Current Implementation Status

The repository currently contains the backend projects, Next.js frontend, EF Core migrations, tests, Docker
development infrastructure, dev/demo scripts, and a demo-ready product surface.

Implemented product areas include customer and membership management, static roles/permissions, Control Hosts,
Hosts, inventory groups, playbooks, variable sets, actions, runs, schedules, persisted run logs, audit logs,
templates, secrets metadata and reference validation, user overview, Hostzustand checks, a run wizard, and a
Run Center.

The production deployment story is not complete. `deploy/` contains dev/demo notes only, while local bootstrap
is handled by scripts in `scripts/`.

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
- Template
- Job
- JobRun
- Schedule
- AuditLog
- Secret

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
- Database-backed schedule persistence
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

The API also must not perform SSH checks, TCP checks, shell execution, or process starts as product behavior.
It creates and reads records, validates input, authorizes requests, and leaves execution work to the Worker.

### NodeControl.Worker

Contains background execution.

Examples:

- Poll queued JobRuns
- Execute JobRuns
- Poll due schedules and enqueue scheduled JobRuns
- Create execution workspaces
- Run `ansible-playbook`
- Capture logs
- Update job run status
- Process queued HostConnectionCheck records with TCP connect attempts

The current Worker uses PostgreSQL as the coordination point. It polls queued JobRuns, queued host connection
checks, and due active schedules. There is no message bus in the MVP.

## Frontend Structure

The frontend uses Next.js App Router.

Main responsibilities:

- Auth-aware layout
- Customer navigation
- Dashboard
- CRUD screens for current resource types
- Run wizard and Run Center
- Schedule screens
- Hostzustand
- Audit screens
- Clean planned placeholders for post-MVP surfaces

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

### HostConnectionCheck

An append-only, customer-scoped record for a point-in-time TCP reachability check against a ControlNode or
ManagedNode endpoint. The API only queues and reads checks; the Worker is responsible for processing queued
checks and performing the TCP connection attempt. Host connection checks do not run Ansible, perform SSH
authentication, or decrypt secrets.

### InventoryGroup

A group of ManagedNodes used to generate Ansible inventory.

### Playbook

A reusable automation definition.

### VariableSet

Variables passed to an Ansible job.

### Template

A customer-scoped reusable text/Jinja2/config/script template. The current implementation manages templates as plain text
resources only: NodeControl validates simple structure, stores metadata/content, and audits create/update/archive
operations. Templates are not executed, rendered, uploaded to hosts, or wired into JobRun execution yet.

### Secret

A customer-scoped protected value such as a password, API token, SSH private key, certificate, or connection
string. The current implementation exposes secret metadata only through the API. Plaintext values are accepted on create/rotate,
protected before persistence, and never returned in API responses. It also adds the canonical safe reference
syntax `secret://secret-slug`; references are validated by customer and active status only, without decrypting or
returning secret values. Secret values are not injected into Worker execution yet.

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

The MVP Worker polls queued JobRuns from the database and processes the oldest queued run first. It also
polls active schedules and creates queued scheduled JobRuns when they are due. Actual Ansible execution
remains local to the Worker process until remote control-node dispatch is introduced in a later slice.

JobRun logs are persisted by the Worker as ordered entries. The API provides read-only access to those logs
through customer-scoped authorization, applies lightweight response-time redaction for obvious token/password
patterns, and never starts Ansible execution.

JobRun cancellation and retry are operational controls around the same execution pipeline. The API can mark
queued runs Cancelled, mark running runs Cancelling, and create queued retry runs. The Worker observes
cancellation requests from the database and is the only process that stops `ansible-playbook`.

## Data Flow: Host Connection Check

```text
User clicks "Check starten"
  |
API checks ManageNodes permission
  |
API creates HostConnectionCheck with Status = Queued
  |
Worker picks queued HostConnectionCheck
  |
Worker attempts TCP connect to hostname:sshPort
  |
Worker updates status, duration, and result/error message
```

The API never performs TCP, SSH, shell, or Ansible checks. Hostzustand uses the persisted check records and the
latest check per host to show demo-ready reachability state.

Audit logs are persisted separately from JobRun logs. Audit entries are append-only business activity records
with actor, customer, entity, action, outcome, timestamp, and a short message. JobRun logs remain technical
execution output for a single run and may contain stdout/stderr lines; audit entries must not store Ansible
stdout/stderr or secret variable values.

The current audit implementation records focused operational audit actions including `job.created`, `job.updated`, `job.archived`,
`job_run.created_manual`, `job_run.cancel_requested`, `job_run.cancelled_queued`, `job_run.retried`,
`job_run.created_scheduled`, `schedule.created`, `schedule.updated`, `schedule.paused`,
`schedule.resumed`, and `schedule.archived`. Customer-scoped audit API reads require `ViewAuditLogs`.

## Data Flow: Scheduled Job

```text
Worker schedule poller finds due active schedule
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

Migrations are real EF-generated migrations under `src/backend/NodeControl.Infrastructure/Migrations`.

The database stores:

- Product data
- User profiles
- Memberships
- Job definitions
- Job run metadata
- Audit logs
- Schedule definitions

JobRun metadata, log entries, schedules, audit logs, templates, and secret metadata are stored in the database.
Worker run workspaces are stored on the filesystem.

## Execution Storage

Current local/dev path:

```text
src/backend/NodeControl.Worker/.nodecontrol/runs/{jobRunId}/
```

Production-style paths remain configuration choices and are not packaged as a finished deployment in this repo:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/
/var/lib/nodecontrol/runs/{jobRunId}/
```

## Local Dev/Demo Runtime

Use the root scripts for local bootstrap:

```bash
./scripts/dev-up.sh
./scripts/dev-migrate.sh
./scripts/dev-run-api.sh
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

`dev-up.sh` starts PostgreSQL and a Keycloak dev container. The default API development configuration uses Fake
Auth, so Keycloak is available as a dev/demo provider but is not required for the default local path.

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
