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

Slice 3 implements target structure and a read-only inventory preview only.

- Control Nodes are modeled as customer-scoped records, but NodeControl does not test SSH connectivity in this slice.
- Managed Nodes are modeled as customer-scoped records and can be linked to Inventory Groups.
- Inventory Groups generate a YAML preview from active Managed Nodes.
- Archived Managed Nodes are excluded from preview output.
- The API still must not execute Ansible; execution remains a later Worker concern.

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
