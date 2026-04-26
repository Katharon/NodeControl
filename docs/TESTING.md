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
