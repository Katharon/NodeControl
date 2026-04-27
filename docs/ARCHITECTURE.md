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

The MVP Worker polls queued JobRuns from the database and processes the oldest queued run first. Actual
Ansible execution remains local to the Worker process until remote control-node dispatch is introduced in a
later slice.

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
