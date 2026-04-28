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
- ManageMemberships
- ViewNodes
- ManageNodes
- ViewPlaybooks
- ManagePlaybooks
- RunJobs
- ViewJobRuns
- CancelJobRuns
- RetryJobRuns
- ViewSchedules
- ManageSchedules
- ViewAuditLogs
- ViewTemplates
- ManageTemplates
- ViewSecrets
- ManageSecrets

## Static MVP Role Map

The current implementation uses a static role-to-permission map. Dynamic role editing is intentionally post-MVP.

- Owner has all MVP permissions.
- Admin has all MVP permissions except ManageMemberships.
- Operator has ViewCustomer, ViewNodes, ViewPlaybooks, ViewTemplates, ViewSecrets, RunJobs, RetryJobRuns, ViewJobRuns, and ViewSchedules.
- Viewer has ViewCustomer, ViewNodes, ViewPlaybooks, ViewTemplates, ViewSecrets, ViewJobRuns, and ViewSchedules.
- Auditor has ViewCustomer, ViewJobRuns, and ViewAuditLogs.

Platform admins can access and manage every customer. Normal users can access only active customers where they have an active CustomerMembership, and inactive memberships grant no permissions.

## Login Flow

OIDC product flow:

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

The local default development flow is simpler: `Auth:Mode` is `Fake` in
`src/backend/NodeControl.Api/appsettings.Development.json`. `/auth/login` redirects to the frontend and the
Fake Auth handler authenticates requests as the configured Dev Admin user.

## Token Storage Rule

Do not store access tokens in browser `localStorage`.

Prefer HttpOnly cookies controlled by the backend.

## Local Development Modes

### Keycloak Dev Mode

Keycloak can be used as a local OIDC provider. `docker-compose.dev.yml` starts a Keycloak container on
`http://localhost:18080`, but the default local scripts use Fake Auth unless API configuration is changed to
OIDC.

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
- Fake Auth provides the fixed `Dev Admin` development user by default.

The standard dev/demo startup path is:

```bash
./scripts/dev-up.sh
./scripts/dev-migrate.sh
./scripts/dev-run-api.sh
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Then open `http://localhost:3000` and sign in through Fake Auth.

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

## User Management MVP

The current implementation includes a small demo-ready user overview without adding registration, passwords, invitations, or external
identity-provider administration.

- `GET /api/v1/users` and `GET /api/v1/users/{userId}` are platform-admin-only and expose safe metadata:
  display name, email, active state, platform-admin state, timestamps, and a minimal external identity summary.
- Customer membership forms use `GET /api/v1/customers/{customerId}/membership-candidates` to search existing
  active users by display name or email.
- Membership candidate search uses the existing `ManageMemberships` permission for the target customer and excludes
  users who are already active members of that customer.
- Creating a membership still uses the existing user id contract. This slice does not create users by email.

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
