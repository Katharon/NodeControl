"use client";

import { Alert, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
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

  return (
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
        <Typography>Job: {jobRun.jobId}</Typography>
        {jobRun.triggeredByUserId ? <Typography>Triggered by: {jobRun.triggeredByUserId}</Typography> : null}
        {jobRun.errorMessage ? <Alert severity="error">{jobRun.errorMessage}</Alert> : null}
      </Stack>
    </Paper>
  );
}
