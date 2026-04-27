import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type JobScheduleStatus = "Active" | "Paused" | "Archived";

export type JobSchedule = {
  id: string;
  customerId: string;
  jobId: string;
  name: string;
  slug: string;
  description: string | null;
  cronExpression: string;
  timeZoneId: string;
  status: JobScheduleStatus;
  nextRunAtUtc: string | null;
  lastRunAtUtc: string | null;
  lastJobRunId: string | null;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type JobScheduleInput = {
  name: string;
  slug: string;
  description?: string | null;
  jobId: string;
  cronExpression: string;
  timeZoneId?: string | null;
};

export function getSchedules(customerId: string) {
  return apiGet<JobSchedule[]>(`/api/v1/customers/${customerId}/schedules`);
}

export function getSchedule(customerId: string, scheduleId: string) {
  return apiGet<JobSchedule>(`/api/v1/customers/${customerId}/schedules/${scheduleId}`);
}

export function createSchedule(customerId: string, input: JobScheduleInput) {
  return apiPost<JobScheduleInput, JobSchedule>(`/api/v1/customers/${customerId}/schedules`, input);
}

export function updateSchedule(customerId: string, scheduleId: string, input: JobScheduleInput) {
  return apiPut<JobScheduleInput, JobSchedule>(`/api/v1/customers/${customerId}/schedules/${scheduleId}`, input);
}

export function pauseSchedule(customerId: string, scheduleId: string) {
  return apiPost<Record<string, never>, JobSchedule>(`/api/v1/customers/${customerId}/schedules/${scheduleId}/pause`, {});
}

export function resumeSchedule(customerId: string, scheduleId: string) {
  return apiPost<Record<string, never>, JobSchedule>(`/api/v1/customers/${customerId}/schedules/${scheduleId}/resume`, {});
}

export function archiveSchedule(customerId: string, scheduleId: string) {
  return apiDelete<JobSchedule>(`/api/v1/customers/${customerId}/schedules/${scheduleId}`);
}
