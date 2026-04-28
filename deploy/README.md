# NodeControl Deploy Notes

`deploy/` is intentionally minimal right now. Slice 19 only adds dev/demo bootstrap support; it does not add a production deployment platform.

For a local demo, use the root development compose file through the scripts:

```bash
./scripts/dev-up.sh
./scripts/dev-migrate.sh
```

Then run the API, Worker, and frontend in separate terminals:

```bash
./scripts/dev-run-api.sh
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Open `http://localhost:3000` and sign in through Fake Auth. Production deployment hardening, packaging, TLS, and identity-provider configuration are intentionally outside this slice.
