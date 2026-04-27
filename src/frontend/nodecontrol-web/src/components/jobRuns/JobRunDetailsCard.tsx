"use client";

import { Alert, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { JobRunLogsPanel } from "@/components/jobRuns/JobRunLogsPanel";
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
    return <Alert severity="error">This job run could not be loaded.</Alert>;
  }

  if (!canViewJobRuns) {
    return <Alert severity="warning">You do not have permission to view job runs for this customer.</Alert>;
  }

  if (jobRunQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobRunQuery.isError) {
    return <Alert severity="error">This job run could not be loaded.</Alert>;
  }

  const jobRun = jobRunQuery.data;
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
              <Typography component="h1" variant="h4">Job Run</Typography>
              <Typography color="text.secondary">{jobRun.id}</Typography>
            </Stack>
            <JobRunStatusChip status={jobRun.status} />
          </Stack>
          <Typography>Trigger: {jobRun.triggerType}</Typography>
          <Typography>Queued: {new Date(jobRun.queuedAt).toLocaleString()}</Typography>
          <Typography>Started: {jobRun.startedAt ? new Date(jobRun.startedAt).toLocaleString() : "Not started"}</Typography>
          <Typography>Finished: {jobRun.finishedAt ? new Date(jobRun.finishedAt).toLocaleString() : "Not finished"}</Typography>
          <Typography>Exit code: {jobRun.exitCode ?? "Not available"}</Typography>
          <Typography>Job: {jobRun.jobId}</Typography>
          {jobRun.triggeredByUserId ? <Typography>Triggered by: {jobRun.triggeredByUserId}</Typography> : null}
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
