import { apiGet } from "@/lib/api/apiClient";

export type JobRunStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Cancelled" | "TimedOut";
export type JobRunTriggerType = "Manual" | "Scheduled" | "System";

export type JobRun = {
  id: string;
  customerId: string;
  jobId: string;
  triggerType: JobRunTriggerType;
  triggeredByUserId: string | null;
  scheduleId: string | null;
  status: JobRunStatus;
  queuedAt: string;
  startedAt: string | null;
  finishedAt: string | null;
  exitCode: number | null;
  errorMessage: string | null;
  workspacePath: string | null;
  stdoutLogPath: string | null;
  stderrLogPath: string | null;
  createdAt: string;
};

export function getJobRuns(customerId: string) {
  return apiGet<JobRun[]>(`/api/v1/customers/${customerId}/job-runs`);
}

export function getJobRun(customerId: string, jobRunId: string) {
  return apiGet<JobRun>(`/api/v1/customers/${customerId}/job-runs/${jobRunId}`);
}
