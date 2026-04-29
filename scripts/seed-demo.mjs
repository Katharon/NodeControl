const apiUrl = (process.env.NODECONTROL_API_URL ?? "http://localhost:5257").replace(/\/$/, "");
const args = new Set(process.argv.slice(2));

if (args.has("--help") || args.has("-h")) {
  console.log(`Usage: ./scripts/dev-seed-demo.sh [--queue-run]

Seeds a compact NodeControl demo story through the running Development API.

Options:
  --queue-run   Queue one real Run for the seeded demo Action after seeding.
  --help        Show this help text.

Environment:
  NODECONTROL_API_URL  API origin to seed. Defaults to http://localhost:5257.`);
  process.exit(0);
}

const shouldQueueRun = args.has("--queue-run");

const story = {
  customer: {
    name: "Acme Managed Services",
    slug: "acme-managed-services",
    description:
      "Showcase tenant for the NodeControl reviewer flow: hosts, inventory, playbook, variables, action, schedule, runs, and audit.",
  },
  controlNode: {
    name: "Local Demo Control Host",
    hostname: "localhost",
    sshPort: 22,
    description:
      "Represents the local Worker/control host used for the development showcase. The API only stores this metadata.",
  },
  managedNodes: [
    {
      name: "web01",
      hostname: "127.0.0.1",
      sshPort: 22,
      operatingSystem: "Ubuntu 24.04",
      environment: "Production",
      description: "Example customer web host for inventory and run wizard demos.",
    },
    {
      name: "app01",
      hostname: "127.0.0.1",
      sshPort: 2222,
      operatingSystem: "Debian 12",
      environment: "Production",
      description: "Example application host. The demo playbook runs locally and does not SSH to this host.",
    },
    {
      name: "db01",
      hostname: "127.0.0.1",
      sshPort: 2200,
      operatingSystem: "PostgreSQL appliance",
      environment: "Staging",
      description: "Example database host for customer-scoped host and inventory screens.",
    },
  ],
  inventoryGroup: {
    name: "demo_fleet",
    description: "Default Acme demo inventory grouping the showcase hosts.",
  },
  playbook: {
    name: "Demo - Inventory Echo",
    slug: "demo-inventory-echo",
    description:
      "Safe inline Ansible demo. It uses the generated inventory and prints selected variables through the Worker execution path.",
    sourceType: "InlineYaml",
    inlineContent: `---
- name: NodeControl demo inventory echo
  hosts: all
  gather_facts: false
  connection: local
  tasks:
    - name: Show selected host and safe demo variables
      ansible.builtin.debug:
        msg:
          - "NodeControl queued this task for {{ inventory_hostname }}."
          - "Environment: {{ demo_environment }}"
          - "Maintenance window: {{ maintenance_window }}"
          - "Change ticket: {{ change_ticket }}"

    - name: Show requested packages without changing hosts
      ansible.builtin.debug:
        var: package_names
`,
    entryFilePath: "site.yml",
  },
  variableSet: {
    name: "Demo - Safe Defaults",
    slug: "demo-safe-defaults",
    description: "Small non-secret variable set used by the demo action.",
    format: "Yaml",
    containsSensitiveValues: false,
    content: `demo_environment: review
maintenance_window: weekday-evening
change_ticket: NC-DEMO-001
package_names:
  - curl
  - git
`,
  },
  job: {
    name: "Demo - Inventory Echo",
    slug: "demo-inventory-echo",
    description:
      "Reusable showcase Action that demonstrates customer scoping, generated inventory, Worker execution, run logs, and audit trail.",
    defaultTimeoutSeconds: 300,
  },
  schedule: {
    name: "Demo - Weekday Review Window",
    slug: "demo-weekday-review-window",
    description:
      "Paused example schedule for reviewers. Resume it in the UI to demonstrate scheduled Runs without surprise background executions.",
    cronExpression: "0 18 * * 1-5",
    timeZoneId: "UTC",
  },
};

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});

async function main() {
  await checkApi();

  const customer = await ensureCustomer(story.customer);
  const controlNode = await ensureControlNode(customer.id, story.controlNode);
  const managedNodes = [];
  for (const node of story.managedNodes) {
    managedNodes.push(await ensureManagedNode(customer.id, node));
  }

  const inventoryGroup = await ensureInventoryGroup(customer.id, story.inventoryGroup);
  for (const node of managedNodes) {
    await ensureInventoryMembership(customer.id, inventoryGroup, node);
  }
  const refreshedInventoryGroup = await getInventoryGroup(customer.id, inventoryGroup.id);

  const playbook = await ensurePlaybook(customer.id, story.playbook);
  const variableSet = await ensureVariableSet(customer.id, story.variableSet);
  const job = await ensureJob(customer.id, {
    ...story.job,
    controlNodeId: controlNode.id,
    inventoryGroupId: refreshedInventoryGroup.id,
    playbookId: playbook.id,
    variableSetId: variableSet.id,
  });
  const schedule = await ensurePausedSchedule(customer.id, { ...story.schedule, jobId: job.id });

  let queuedRun = null;
  if (shouldQueueRun) {
    queuedRun = await post(`/api/v1/customers/${customer.id}/jobs/${job.id}/run`, {});
    console.log(`Queued demo Run: ${queuedRun.id}`);
  }

  console.log("");
  console.log("Demo seed data is ready.");
  console.log(`Customer: ${customer.name} (${customer.id})`);
  console.log(`Control Host: ${controlNode.name}`);
  console.log(`Hosts: ${managedNodes.map((node) => node.name).join(", ")}`);
  console.log(`Inventory: ${refreshedInventoryGroup.name}`);
  console.log(`Playbook: ${playbook.name}`);
  console.log(`Variables: ${variableSet.name}`);
  console.log(`Action: ${job.name}`);
  console.log(`Paused Schedule: ${schedule.name}`);
  console.log("");
  console.log(`Open: http://localhost:3000/customers/${customer.id}`);
  console.log(`Run wizard: http://localhost:3000/run-wizard?customerId=${customer.id}`);
  if (queuedRun) {
    console.log(`Queued run: http://localhost:3000/customers/${customer.id}/runs/${queuedRun.id}`);
  } else {
    console.log("Queue a real run with: ./scripts/dev-seed-demo.sh --queue-run");
  }
}

async function checkApi() {
  try {
    const me = await get("/api/v1/me");
    console.log(`Using API ${apiUrl} as ${me.displayName ?? me.email ?? "current user"}.`);
  } catch (error) {
    throw new Error(
      `Could not reach ${apiUrl}/api/v1/me. Start the Development API with ./scripts/dev-run-api.sh before seeding.\n${error.message}`,
    );
  }
}

async function ensureCustomer(input) {
  const existing = (await get("/api/v1/customers")).find((customer) => customer.slug === input.slug);
  if (existing) {
    console.log(`Updating customer: ${input.name}`);
    return put(`/api/v1/customers/${existing.id}`, input);
  }

  console.log(`Creating customer: ${input.name}`);
  return post("/api/v1/customers", input);
}

async function ensureControlNode(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/control-nodes`)).find(
    (node) => node.name === input.name,
  );
  if (existing) {
    console.log(`Updating control host: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/control-nodes/${existing.id}`, input);
  }

  console.log(`Creating control host: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/control-nodes`, input);
}

async function ensureManagedNode(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/managed-nodes`)).find(
    (node) => node.name === input.name,
  );
  if (existing) {
    console.log(`Updating host: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/managed-nodes/${existing.id}`, input);
  }

  console.log(`Creating host: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/managed-nodes`, input);
}

async function ensureInventoryGroup(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/inventory-groups`)).find(
    (group) => group.name === input.name,
  );
  if (existing) {
    console.log(`Updating inventory: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/inventory-groups/${existing.id}`, input);
  }

  console.log(`Creating inventory: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/inventory-groups`, input);
}

async function ensureInventoryMembership(customerId, inventoryGroup, managedNode) {
  if (inventoryGroup.managedNodeIds.includes(managedNode.id)) {
    return inventoryGroup;
  }

  console.log(`Adding ${managedNode.name} to inventory ${inventoryGroup.name}`);
  return post(`/api/v1/customers/${customerId}/inventory-groups/${inventoryGroup.id}/nodes`, {
    managedNodeId: managedNode.id,
  });
}

async function getInventoryGroup(customerId, inventoryGroupId) {
  return get(`/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}`);
}

async function ensurePlaybook(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/playbooks`)).find(
    (playbook) => playbook.slug === input.slug,
  );
  if (existing) {
    console.log(`Updating playbook: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/playbooks/${existing.id}`, input);
  }

  console.log(`Creating playbook: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/playbooks`, input);
}

async function ensureVariableSet(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/variable-sets`)).find(
    (variableSet) => variableSet.slug === input.slug,
  );
  if (existing) {
    console.log(`Updating variable set: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/variable-sets/${existing.id}`, input);
  }

  console.log(`Creating variable set: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/variable-sets`, input);
}

async function ensureJob(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/jobs`)).find((job) => job.slug === input.slug);
  if (existing) {
    console.log(`Updating action: ${input.name}`);
    return put(`/api/v1/customers/${customerId}/jobs/${existing.id}`, input);
  }

  console.log(`Creating action: ${input.name}`);
  return post(`/api/v1/customers/${customerId}/jobs`, input);
}

async function ensurePausedSchedule(customerId, input) {
  const existing = (await get(`/api/v1/customers/${customerId}/schedules`)).find(
    (schedule) => schedule.slug === input.slug,
  );
  let schedule;
  if (existing) {
    console.log(`Updating schedule: ${input.name}`);
    schedule = await put(`/api/v1/customers/${customerId}/schedules/${existing.id}`, input);
  } else {
    console.log(`Creating schedule: ${input.name}`);
    schedule = await post(`/api/v1/customers/${customerId}/schedules`, input);
  }

  if (schedule.status !== "Paused") {
    console.log(`Pausing schedule: ${input.name}`);
    schedule = await post(`/api/v1/customers/${customerId}/schedules/${schedule.id}/pause`, {});
  }

  return schedule;
}

async function get(path) {
  return request("GET", path);
}

async function post(path, body) {
  return request("POST", path, body);
}

async function put(path, body) {
  return request("PUT", path, body);
}

async function request(method, path, body) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      Accept: "application/json",
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  if (!response.ok) {
    throw new Error(`${method} ${path} failed with HTTP ${response.status}: ${text || response.statusText}`);
  }

  return text ? JSON.parse(text) : null;
}
