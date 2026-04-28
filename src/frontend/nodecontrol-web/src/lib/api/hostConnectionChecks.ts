import { apiGet, apiPost } from "@/lib/api/apiClient";

export type HostConnectionTargetType = "ControlNode" | "ManagedNode";

export type HostConnectionCheckStatus =
  | "Queued"
  | "Running"
  | "Succeeded"
  | "Failed"
  | "TimedOut";

export type HostConnectionCheck = {
  id: string;
  customerId: string;
  targetType: HostConnectionTargetType;
  controlNodeId: string | null;
  managedNodeId: string | null;
  hostname: string;
  port: number;
  status: HostConnectionCheckStatus;
  requestedByUserId: string | null;
  queuedAtUtc: string;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  durationMs: number | null;
  resultMessage: string | null;
  errorMessage: string | null;
};

export type HostHealthTarget = {
  targetType: HostConnectionTargetType;
  targetId: string;
  name: string;
  hostname: string;
  port: number;
  latestCheck: HostConnectionCheck | null;
};

export type HostHealthSummary = {
  targets: HostHealthTarget[];
};

export function getHostHealth(customerId: string) {
  return apiGet<HostHealthSummary>(`/api/v1/customers/${customerId}/host-health`);
}

export function getHostConnectionChecks(customerId: string) {
  return apiGet<HostConnectionCheck[]>(
    `/api/v1/customers/${customerId}/host-connection-checks`,
  );
}

export function queueControlNodeConnectionCheck(customerId: string, controlNodeId: string) {
  return apiPost<undefined, HostConnectionCheck>(
    `/api/v1/customers/${customerId}/control-nodes/${controlNodeId}/connection-checks`,
    undefined,
  );
}

export function queueManagedNodeConnectionCheck(customerId: string, managedNodeId: string) {
  return apiPost<undefined, HostConnectionCheck>(
    `/api/v1/customers/${customerId}/managed-nodes/${managedNodeId}/connection-checks`,
    undefined,
  );
}
