import { apiGet, apiPost } from "@/lib/api/apiClient";

export type JobRunStatus = "Queued" | "Running" | "Cancelling" | "Succeeded" | "Failed" | "Cancelled" | "TimedOut";
export type JobRunTriggerType = "Manual" | "Scheduled" | "System" | "Retry";
export type JobRunLogStream = "System" | "StdOut" | "StdErr";
export type JobRunLogLevel = "Info" | "Warning" | "Error";

export type JobRun = {
  id: string;
  customerId: string;
  jobId: string;
  controlNodeId: string;
  triggerType: JobRunTriggerType;
  triggeredByUserId: string | null;
  scheduleId: string | null;
  retriedFromJobRunId: string | null;
  retryAttempt: number;
  status: JobRunStatus;
  queuedAt: string;
  startedAt: string | null;
  finishedAt: string | null;
  exitCode: number | null;
  errorMessage: string | null;
  workspacePath: string | null;
  stdoutLogPath: string | null;
  stderrLogPath: string | null;
  cancellationRequestedAtUtc: string | null;
  cancellationRequestedByUserId: string | null;
  cancellationReason: string | null;
  createdAt: string;
};

export type JobRunLogEntry = {
  id: string;
  jobRunId: string;
  sequence: number;
  timestampUtc: string;
  stream: JobRunLogStream;
  level: JobRunLogLevel;
  message: string;
};

export type JobRunLogsResponse = {
  items: JobRunLogEntry[];
};

export function getJobRuns(customerId: string) {
  return apiGet<JobRun[]>(`/api/v1/customers/${customerId}/job-runs`);
}

export function getJobRun(customerId: string, jobRunId: string) {
  return apiGet<JobRun>(`/api/v1/customers/${customerId}/job-runs/${jobRunId}`);
}

export function getJobRunLogs(
  customerId: string,
  jobRunId: string,
  options?: { afterSequence?: number; limit?: number },
) {
  const params = new URLSearchParams();
  if (options?.afterSequence !== undefined) {
    params.set("afterSequence", options.afterSequence.toString());
  }

  if (options?.limit !== undefined) {
    params.set("limit", options.limit.toString());
  }

  const query = params.size > 0 ? `?${params.toString()}` : "";
  return apiGet<JobRunLogsResponse>(`/api/v1/customers/${customerId}/job-runs/${jobRunId}/logs${query}`);
}

export function cancelJobRun(customerId: string, jobRunId: string, reason?: string | null) {
  return apiPost<{ reason?: string | null }, JobRun>(
    `/api/v1/customers/${customerId}/job-runs/${jobRunId}/cancel`,
    { reason: reason || null },
  );
}

export function retryJobRun(customerId: string, jobRunId: string) {
  return apiPost<Record<string, never>, JobRun>(
    `/api/v1/customers/${customerId}/job-runs/${jobRunId}/retry`,
    {},
  );
}
