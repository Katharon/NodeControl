import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type ControlNodeStatus = "Active" | "Archived";

export type ControlNode = {
  id: string;
  customerId: string;
  name: string;
  hostname: string;
  sshPort: number;
  description: string | null;
  status: ControlNodeStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type ControlNodeInput = {
  name: string;
  hostname: string;
  sshPort: number;
  description?: string | null;
};

export function getControlNodes(customerId: string) {
  return apiGet<ControlNode[]>(`/api/v1/customers/${customerId}/control-nodes`);
}

export function createControlNode(customerId: string, input: ControlNodeInput) {
  return apiPost<ControlNodeInput, ControlNode>(
    `/api/v1/customers/${customerId}/control-nodes`,
    input,
  );
}

export function updateControlNode(
  customerId: string,
  controlNodeId: string,
  input: ControlNodeInput,
) {
  return apiPut<ControlNodeInput, ControlNode>(
    `/api/v1/customers/${customerId}/control-nodes/${controlNodeId}`,
    input,
  );
}

export function archiveControlNode(customerId: string, controlNodeId: string) {
  return apiDelete<ControlNode>(
    `/api/v1/customers/${customerId}/control-nodes/${controlNodeId}`,
  );
}
