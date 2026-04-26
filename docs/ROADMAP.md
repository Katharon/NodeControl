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
