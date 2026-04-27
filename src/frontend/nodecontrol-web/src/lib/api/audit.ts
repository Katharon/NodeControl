import { apiGet } from "@/lib/api/apiClient";

export type AuditOutcome = "Succeeded" | "Failed" | "Denied";

export type AuditLogEntry = {
  id: string;
  customerId: string | null;
  actorUserId: string | null;
  actorDisplayName: string | null;
  actorType: string;
  action: string;
  entityType: string;
  entityId: string | null;
  entityDisplayName: string | null;
  outcome: AuditOutcome;
  message: string;
  metadataJson: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  createdAtUtc: string;
};

export type AuditLogListResponse = {
  items: AuditLogEntry[];
};

export type AuditLogQuery = {
  action?: string;
  entityType?: string;
  outcome?: AuditOutcome | "";
};

export function getAuditLogs(customerId: string, query: AuditLogQuery = {}) {
  const params = new URLSearchParams();
  if (query.action?.trim()) {
    params.set("action", query.action.trim());
  }

  if (query.entityType?.trim()) {
    params.set("entityType", query.entityType.trim());
  }

  if (query.outcome) {
    params.set("outcome", query.outcome);
  }

  const suffix = params.size > 0 ? `?${params.toString()}` : "";
  return apiGet<AuditLogListResponse>(`/api/v1/customers/${customerId}/audit-logs${suffix}`);
}
