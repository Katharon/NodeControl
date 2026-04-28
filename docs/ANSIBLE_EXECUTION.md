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

## Current Playbook Storage

The current implementation stores inline playbook definitions through the application/database model and writes
the selected playbook into each JobRun workspace when a Run is processed.

Artifact-directory playbooks remain a later feature. A production-style artifact path could be:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/
```

For inline YAML playbooks, a future artifact-backed layout could write:

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

The Worker implements local execution for queued JobRuns. The workspace root is configured with
`NodeControl:Execution:RunWorkspaceRoot`; development uses `.nodecontrol/runs`, while production-style
configuration uses `/var/lib/nodecontrol/runs`.

## Execution Command

Conceptually:

```text
ansible-playbook -i inventory.yml playbook/site.yml -e @vars.yml
```

The exact implementation belongs to Infrastructure/Worker.

The Worker runs `ansible-playbook` from `NodeControl.Worker` only, using `ProcessStartInfo.ArgumentList`
with `UseShellExecute = false`. The executable path is configured with
`NodeControl:Execution:AnsiblePlaybookPath` and defaults to `ansible-playbook`.

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

The Worker writes persisted System, StdOut, and StdErr JobRun log entries while a run
is processed. The API exposes these entries read-only for authorized customer users; it does not execute or
resume JobRuns.

## JobRun Statuses

Initial statuses:

- Queued
- Running
- Cancelling
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

The current implementation includes target structure, inventory group assignment, and a read-only inventory preview.

- Control Nodes are modeled as customer-scoped records. Hostzustand can queue TCP reachability checks, but
  NodeControl does not perform SSH authentication checks.
- Managed Nodes are modeled as customer-scoped records and can be linked to Inventory Groups.
- Inventory Groups generate a YAML preview from active Managed Nodes.
- Archived Managed Nodes are excluded from preview output.
- The API still must not execute Ansible; execution belongs to the Worker.

For Worker execution, the selected Job inventory group is rendered to `inventory.yml`. Active managed nodes
linked to that group become hosts, and archived nodes are excluded. If the group has no active managed nodes,
the JobRun fails before Ansible starts.

## Variable Sets

VariableSets may be stored as YAML or JSON content.

They are written into the JobRun workspace as `vars.yml` or `vars.json` depending on the VariableSet format.

Sensitive values need special care.

MVP rule:

- Do not log variable content.
- Do not expose secret variable values through API responses.
- Prefer not implementing advanced secret storage until the execution path works.

## Templates

Templates are reusable customer-scoped text/Jinja2/config/script resources. The current implementation stores and validates
template content as plain text only. The API and Application layer do not render templates, execute scripts,
invoke Python/Jinja runtimes, or pass template content into JobRun execution. Future slices may connect
templates to playbook artifacts or deployment flows, but Worker execution remains unchanged here.

## Secrets

Secrets are customer-scoped protected values managed through metadata-only API responses. The current implementation accepts
plaintext values only on create and rotate, protects them before persistence, and never returns plaintext or
protected payloads from API responses. Secrets are not resolved during JobRun execution and are not integrated
with Jobs or Worker workspaces yet. Safe `secret://secret-slug` references are supported for Templates
and VariableSets. Reference validation checks only same-customer active metadata and never decrypts, renders,
exports, or passes secret values to Ansible.

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

Stdout and stderr are also stored as ordered log entries for display. NodeControl still treats these as text
lines rather than parsing Ansible task semantics.

Post-MVP may add:

- JSON callback plugin
- task-level event timeline
- per-host result summary
- changed/failed/unreachable counts

## Cancellation

The API can cancel queued JobRuns or request cancellation for running JobRuns.

- Queued cancellation immediately sets the JobRun to Cancelled.
- Running cancellation sets the JobRun to Cancelling.
- The API never kills operating system processes.
- The Worker observes cancellation state in the database while `ansible-playbook` is running.
- The Worker terminates the process tree, writes cancellation logs, and marks the JobRun Cancelled.

Cancelled, failed, and timed-out JobRuns can be retried by creating a new queued JobRun with
`TriggerType = Retry`. The retry keeps a link to the original JobRun through `RetriedFromJobRunId`.

Manual run creation, cancellation requests, retries, and scheduled run creation write audit entries. These
entries record the business operation and affected customer/entity only. Worker stdout/stderr and detailed
execution progress continue to belong to JobRun log entries, not audit entries.

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
