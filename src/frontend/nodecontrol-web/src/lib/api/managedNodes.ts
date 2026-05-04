import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type ManagedNodeStatus = "Active" | "Archived";

export type ManagedNode = {
  id: string;
  customerId: string;
  name: string;
  hostname: string;
  sshPort: number;
  sshUsername: string | null;
  sshPrivateKeySecretId: string | null;
  jumpHostManagedNodeId: string | null;
  operatingSystem: string | null;
  environment: string | null;
  description: string | null;
  status: ManagedNodeStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type ManagedNodeInput = {
  name: string;
  hostname: string;
  sshPort: number;
  sshUsername?: string | null;
  sshPrivateKeySecretId?: string | null;
  jumpHostManagedNodeId?: string | null;
  operatingSystem?: string | null;
  environment?: string | null;
  description?: string | null;
};

export function getManagedNodes(customerId: string) {
  return apiGet<ManagedNode[]>(`/api/v1/customers/${customerId}/managed-nodes`);
}

export function createManagedNode(customerId: string, input: ManagedNodeInput) {
  return apiPost<ManagedNodeInput, ManagedNode>(
    `/api/v1/customers/${customerId}/managed-nodes`,
    input,
  );
}

export function updateManagedNode(
  customerId: string,
  managedNodeId: string,
  input: ManagedNodeInput,
) {
  return apiPut<ManagedNodeInput, ManagedNode>(
    `/api/v1/customers/${customerId}/managed-nodes/${managedNodeId}`,
    input,
  );
}

export function archiveManagedNode(customerId: string, managedNodeId: string) {
  return apiDelete<ManagedNode>(
    `/api/v1/customers/${customerId}/managed-nodes/${managedNodeId}`,
  );
}
