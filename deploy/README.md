# NodeControl Deploy Notes

`deploy/` is intentionally minimal right now. It documents dev/demo bootstrap support; it does not add a
production deployment platform.

For a local demo, use the root development compose file through the scripts:

```bash
./scripts/dev-up.sh
./scripts/dev-migrate.sh
```

Then start the API in one terminal and seed the optional showcase story from another terminal:

```bash
./scripts/dev-run-api.sh
./scripts/dev-seed-demo.sh
```

Run the Worker and frontend in separate terminals:

```bash
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Open `http://localhost:3000` and sign in through Fake Auth. The seeded `Acme Managed Services` story can be used to show hosts, inventory, playbooks, variables, actions, schedules, runs, logs, host health, and audit. Production deployment hardening, packaging, TLS, and identity-provider configuration are intentionally outside this slice.

For fuller setup notes, see `docs/DEPLOYMENT.md`, `docs/SHOWCASE.md`, and the root `README.md`.
