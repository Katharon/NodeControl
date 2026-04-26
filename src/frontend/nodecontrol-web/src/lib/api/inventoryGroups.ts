import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type InventoryGroup = {
  id: string;
  customerId: string;
  name: string;
  description: string | null;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
  managedNodeIds: string[];
};

export type InventoryGroupInput = {
  name: string;
  description?: string | null;
};

export type InventoryPreview = {
  inventoryGroupId: string;
  inventoryGroupName: string;
  format: "yaml";
  content: string;
};

export function getInventoryGroups(customerId: string) {
  return apiGet<InventoryGroup[]>(`/api/v1/customers/${customerId}/inventory-groups`);
}

export function getInventoryGroup(customerId: string, inventoryGroupId: string) {
  return apiGet<InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}`,
  );
}

export function createInventoryGroup(customerId: string, input: InventoryGroupInput) {
  return apiPost<InventoryGroupInput, InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups`,
    input,
  );
}

export function updateInventoryGroup(
  customerId: string,
  inventoryGroupId: string,
  input: InventoryGroupInput,
) {
  return apiPut<InventoryGroupInput, InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}`,
    input,
  );
}

export function archiveInventoryGroup(customerId: string, inventoryGroupId: string) {
  return apiDelete<InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}`,
  );
}

export function addManagedNodeToInventoryGroup(
  customerId: string,
  inventoryGroupId: string,
  managedNodeId: string,
) {
  return apiPost<{ managedNodeId: string }, InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}/nodes`,
    { managedNodeId },
  );
}

export function removeManagedNodeFromInventoryGroup(
  customerId: string,
  inventoryGroupId: string,
  managedNodeId: string,
) {
  return apiDelete<InventoryGroup>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}/nodes/${managedNodeId}`,
  );
}

export function getInventoryPreview(customerId: string, inventoryGroupId: string) {
  return apiGet<InventoryPreview>(
    `/api/v1/customers/${customerId}/inventory-groups/${inventoryGroupId}/preview`,
  );
}
