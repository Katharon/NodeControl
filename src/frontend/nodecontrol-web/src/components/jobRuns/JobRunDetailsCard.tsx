"use client";

import { Alert, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { CancelJobRunButton } from "@/components/jobRuns/CancelJobRunButton";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { JobRunLogsPanel } from "@/components/jobRuns/JobRunLogsPanel";
import { RetryJobRunButton } from "@/components/jobRuns/RetryJobRunButton";
import { getCustomer } from "@/lib/api/customers";
import { getJobRun } from "@/lib/api/jobRuns";
import { hasPermission } from "@/lib/auth/permissions";

type JobRunDetailsCardProps = {
  customerId: string;
  jobRunId: string;
};

export function JobRunDetailsCard({ customerId, jobRunId }: JobRunDetailsCardProps) {
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewJobRuns = hasPermission(customerQuery.data?.permissions, "ViewJobRuns");
  const jobRunQuery = useQuery({ queryKey: ["job-run", customerId, jobRunId], queryFn: () => getJobRun(customerId, jobRunId), enabled: canViewJobRuns });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">Dieser Run konnte nicht geladen werden.</Alert>;
  }

  if (!canViewJobRuns) {
    return <Alert severity="warning">Du hast keine Berechtigung, Runs für diesen Kunden anzusehen.</Alert>;
  }

  if (jobRunQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobRunQuery.isError) {
    return <Alert severity="error">Dieser Run konnte nicht geladen werden.</Alert>;
  }

  const jobRun = jobRunQuery.data;
  const canCancel = hasPermission(customerQuery.data.permissions, "CancelJobRuns")
    && (jobRun.status === "Queued" || jobRun.status === "Running" || jobRun.status === "Cancelling");
  const canRetry = hasPermission(customerQuery.data.permissions, "RetryJobRuns")
    && (jobRun.status === "Failed" || jobRun.status === "TimedOut" || jobRun.status === "Cancelled");
  const pathRows = [
    ["Workspace", jobRun.workspacePath],
    ["Stdout log", jobRun.stdoutLogPath],
    ["Stderr log", jobRun.stderrLogPath],
  ] as const;

  return (
    <Stack sx={{ gap: 2 }}>
      <Paper sx={{ p: 3 }}>
        <Stack sx={{ gap: 2 }}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
            <Stack>
              <Typography component="h1" variant="h4">Run</Typography>
              <Typography color="text.secondary">{jobRun.id}</Typography>
            </Stack>
            <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
              <JobRunStatusChip status={jobRun.status} />
              {canCancel ? <CancelJobRunButton customerId={customerId} jobRunId={jobRunId} status={jobRun.status} /> : null}
              {canRetry ? <RetryJobRunButton customerId={customerId} jobRunId={jobRunId} /> : null}
            </Stack>
          </Stack>
          <Typography>Trigger: {jobRun.triggerType}</Typography>
          <Typography>Retry attempt: {jobRun.retryAttempt}</Typography>
          {jobRun.retriedFromJobRunId ? <Typography>Retried from: {jobRun.retriedFromJobRunId}</Typography> : null}
          <Typography>Queued: {new Date(jobRun.queuedAt).toLocaleString()}</Typography>
          <Typography>Started: {jobRun.startedAt ? new Date(jobRun.startedAt).toLocaleString() : "Not started"}</Typography>
          <Typography>Finished: {jobRun.finishedAt ? new Date(jobRun.finishedAt).toLocaleString() : "Not finished"}</Typography>
          <Typography>Exit code: {jobRun.exitCode ?? "Not available"}</Typography>
          <Typography>Action: {jobRun.jobId}</Typography>
          {jobRun.triggeredByUserId ? <Typography>Triggered by: {jobRun.triggeredByUserId}</Typography> : null}
          {jobRun.cancellationRequestedAtUtc ? (
            <Typography>Cancellation requested: {new Date(jobRun.cancellationRequestedAtUtc).toLocaleString()}</Typography>
          ) : null}
          {jobRun.cancellationRequestedByUserId ? <Typography>Cancellation requested by: {jobRun.cancellationRequestedByUserId}</Typography> : null}
          {jobRun.cancellationReason ? <Typography>Cancellation reason: {jobRun.cancellationReason}</Typography> : null}
          {jobRun.errorMessage ? <Alert severity="error">{jobRun.errorMessage}</Alert> : null}
          {pathRows.some(([, value]) => value) ? (
            <>
              <Divider />
              <Stack sx={{ gap: 1 }}>
                {pathRows.map(([label, value]) => value ? (
                  <Stack key={label}>
                    <Typography color="text.secondary" variant="body2">{label}</Typography>
                    <Typography sx={{ overflowWrap: "anywhere" }}>{value}</Typography>
                  </Stack>
                ) : null)}
              </Stack>
            </>
          ) : null}
        </Stack>
      </Paper>
      <JobRunLogsPanel customerId={customerId} jobRunId={jobRunId} status={jobRun.status} />
    </Stack>
  );
}
