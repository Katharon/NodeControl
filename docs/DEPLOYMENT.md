# Deployment

## Deployment Goal

NodeControl should be easy to run as a self-hosted B2B product.

The MVP target is Docker Compose.

Kubernetes may be added later.

## MVP Deployment Components

Required services:

- NodeControl API
- NodeControl Worker
- NodeControl Web
- PostgreSQL

Development-only service:

- Keycloak dev provider

Optional later services:

- Reverse proxy
- Object/file storage
- Metrics stack
- Log aggregation
- Secret vault

## Deployment Modes

### Development

Development may include:

- PostgreSQL
- Keycloak
- API
- Worker
- Web

### Production-like Selfhost

Production-like setup should include:

- PostgreSQL
- API
- Worker
- Web
- Reverse proxy / TLS termination
- External OIDC provider

### Demo

Demo setup should include:

- Seed data
- Dev identity provider
- Example customer
- Example managed nodes
- Example playbook
- Example schedule

## Production Requirements

Production configuration must validate:

- Fake Auth is disabled.
- OIDC is configured.
- HTTPS/cookie settings are safe.
- Database connection exists.
- File storage paths are configured.
- Worker is enabled.
- Logging is configured.

## File Storage

MVP file storage paths:

```text
/var/lib/nodecontrol/playbooks/
/var/lib/nodecontrol/runs/
```

These paths must be persisted with Docker volumes.

## Backup Considerations

Important data:

- PostgreSQL database
- Playbook artifact storage
- JobRun logs
- Configuration files

## Post-MVP Deployment Features

Potential later features:

- Kubernetes manifests
- Helm chart
- External object storage
- Automated backups
- Observability stack
- High availability
- Multi-worker execution
