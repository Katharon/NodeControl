import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";
import type { JobRun } from "@/lib/api/jobRuns";

export type JobStatus = "Active" | "Archived";

export type Job = {
  id: string;
  customerId: string;
  name: string;
  slug: string;
  description: string | null;
  controlNodeId: string;
  inventoryGroupId: string;
  playbookId: string;
  variableSetId: string | null;
  status: JobStatus;
  defaultTimeoutSeconds: number;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type JobInput = {
  name: string;
  slug: string;
  description?: string | null;
  controlNodeId: string;
  inventoryGroupId: string;
  playbookId: string;
  variableSetId?: string | null;
  defaultTimeoutSeconds: number;
};

export function getJobs(customerId: string) {
  return apiGet<Job[]>(`/api/v1/customers/${customerId}/jobs`);
}

export function getJob(customerId: string, jobId: string) {
  return apiGet<Job>(`/api/v1/customers/${customerId}/jobs/${jobId}`);
}

export function createJob(customerId: string, input: JobInput) {
  return apiPost<JobInput, Job>(`/api/v1/customers/${customerId}/jobs`, input);
}

export function updateJob(customerId: string, jobId: string, input: JobInput) {
  return apiPut<JobInput, Job>(`/api/v1/customers/${customerId}/jobs/${jobId}`, input);
}

export function archiveJob(customerId: string, jobId: string) {
  return apiDelete<Job>(`/api/v1/customers/${customerId}/jobs/${jobId}`);
}

export function runJob(customerId: string, jobId: string) {
  return apiPost<Record<string, never>, JobRun>(`/api/v1/customers/${customerId}/jobs/${jobId}/run`, {});
}
