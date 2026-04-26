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
