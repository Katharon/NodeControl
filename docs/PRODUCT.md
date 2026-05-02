# NodeControl Product Definition

## Product Summary

NodeControl is a self-hosted B2B control plane for Ansible automation.

It allows IT teams and service providers to manage customers, hosts, inventories, playbooks, actions, schedules,
run history, logs, and audit trails through a professional web interface.

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

The implemented dev/demo MVP is a customer-scoped automation control plane. It currently includes:

- Fake Auth for local demo and OIDC support in the API
- Internal user profile creation
- Customer management
- Customer memberships with static roles
- Control nodes
- Managed nodes
- Inventory groups
- Inline YAML and managed artifact-directory playbooks with product-side file import/editing
- Customer-scoped Git repository sources for one-time artifact imports
- Variable sets
- Manual jobs
- Scheduled jobs
- Job run history
- Job logs
- Run-bound Control Hosts with Worker-side dispatch preparation, local/dev execution fallback, and SSH remote dispatch
- Audit logs
- Hostzustand / TCP reachability checks processed by the Worker
- Template management as plain text resources with Action-linked workspace materialization
- Secret metadata management, rotation, `secret://...` reference validation, and Worker-side execution resolution
- Platform admin user overview
- Run wizard and Run Center
- Docker Compose local infrastructure and dev/demo scripts

## Supporting MVP Surfaces

These product areas exist in the current MVP, but their scope is intentionally limited:

- Templates are managed text resources with file import/editing in the UI. Actions may map selected templates to
  relative files under the run playbook workspace, where the Worker materializes them before Ansible starts.
- Git repository sources store repository metadata such as URL, branch/revision, and subpath. The MVP import flow is
  intentionally one-time: operators copy selected public GitHub files into managed Playbook or Template content, and
  future Runs use that managed content rather than syncing from Git.
- Secrets store protected values and expose safe metadata/reference behavior. Secret values are never returned by the
  API; `secret://...` references are resolved only by the Worker during workspace preparation.
- Platform admin user overview is a review and administration aid for existing users, not user registration,
  invitation, password management, or identity-provider administration.
- The local showcase/bootstrap flow demonstrates real API and Worker paths, but it is not production packaging.

## Current Boundaries

Current implementation boundaries are intentionally explicit:

- NodeControl is still a control plane around Ansible, not an AWX clone.
- The API queues work and enforces authorization; it never executes Ansible, SSH, TCP checks, shell commands, or process starts.
- The Worker executes queued Runs, polls schedules, processes Hostzustand checks, creates workspaces, and captures logs.
- Runs are bound to the selected Control Host when queued; local/dev execution remains available for configured local
  Control Hosts, while non-local Control Hosts can use a minimal Worker-side SSH dispatch configuration.
- Continuous Git-backed playbook execution, Git sync, Ansible Collections management, cloud-provider inventory,
  notifications, approval workflow, external secret providers, and production deployment packaging remain Post-MVP.
- `deploy/` is local dev/demo guidance only at this point.

For the explicit implemented/supporting/deferred split, see `docs/MVP_BOUNDARY.md`.

## Post-MVP Scope

Post-MVP work is directional, not a hidden product promise. Likely expansion paths include:

- Production configuration and deployment hardening
- Basic operational health/version visibility
- Stronger Worker edge-case coverage and operational recovery behavior
- Secret runtime integration and key lifecycle work
- Continuous Git-backed playbooks and richer playbook asset lifecycle
- Import workflows for existing inventories or automation definitions
- Ansible Collections dependency tracking
- Notification integrations
- Cloud-provider integrations as inventory sources
- Broader system and security administration surfaces
- Approval workflow
- Multi-control-node dispatching
- SAML
- OIDC provider per customer
- Live log streaming
- Role customization
- Policy engine
- Licensing
- Billing
- Kubernetes/Helm only if explicitly chosen later
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

1. Start the local stack with the scripts in `scripts/`.
2. Seed the optional showcase story with `./scripts/dev-seed-demo.sh`.
3. Login through Fake Auth as Dev Admin for the local demo.
4. Select the `Acme Managed Services` customer.
5. View hosts and inventory.
6. Open the demo playbook, variable set, and action.
7. Run the action manually through the run wizard or Action page.
8. Inspect run status and logs in the Run Center.
9. Inspect the paused schedule, Hostzustand, and audit logs.
10. Explain customer separation, internal permissions, and the API/Worker execution boundary.
