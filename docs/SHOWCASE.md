# NodeControl Showcase Flow

This guide creates a compact, repeatable local demo state for reviewers and portfolio walkthroughs. It uses the
existing API and application behavior; it does not insert rows directly or fake Run results.

## Setup

Start local infrastructure and apply migrations:

```bash
./scripts/dev-up.sh
./scripts/dev-migrate.sh
```

Start the API in one terminal:

```bash
./scripts/dev-run-api.sh
```

Seed the demo story from another terminal:

```bash
./scripts/dev-seed-demo.sh
```

Then start the Worker and frontend:

```bash
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Open `http://localhost:3000` and sign in through Fake Auth as Dev Admin.

## Demo Story

The seed flow creates or updates one customer story:

- Customer: `Acme Managed Services`
- Control Host: `Local Demo Control Host`
- Hosts: `web01`, `app01`, `db01`
- Inventory: `demo_fleet`
- Playbook: `Demo - Inventory Echo`
- Variable Set: `Demo - Safe Defaults`
- Action: `Demo - Inventory Echo`
- Schedule: `Demo - Weekday Review Window` paused by default

The playbook is intentionally safe and honest. It is an inline Ansible playbook that uses `connection: local` and
prints generated-inventory and variable data through the normal Worker execution path. It does not pretend to SSH
successfully into demo hosts.

## Walkthrough

1. Open the Acme customer from Customers or the URL printed by `dev-seed-demo.sh`.
2. Show Hosts and Inventar to explain customer-scoped inventory.
3. Open Playbooks and Variables to show the safe inline playbook and non-secret variable set.
4. Open Actions and start `Demo - Inventory Echo`, or queue it from the terminal:

   ```bash
   ./scripts/dev-seed-demo.sh --queue-run
   ```

5. Open Runs / Run Center and inspect status and logs.
6. Open Hostzustand. Queue checks if you want to show that reachability is processed by the Worker and can fail
   honestly against demo endpoints.
7. Open Audit to show Action and Schedule audit events.
8. Open Schedules to show the paused example schedule. Resume it only when you intentionally want scheduled Runs.

## Notes

- The script talks to the running API at `NODECONTROL_API_URL` and defaults to `http://localhost:5257`.
- The default Development API uses Fake Auth, so the script runs as Dev Admin.
- If `ansible-playbook` is not available to the Worker, queued Runs fail honestly and the Run logs show the error.
- The seed script is idempotent for active demo records: running it again updates the same named/sluggified resources.
