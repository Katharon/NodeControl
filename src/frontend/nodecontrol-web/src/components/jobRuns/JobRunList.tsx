"use client";

import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import { Alert, Button, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { getCustomer } from "@/lib/api/customers";
import { getJobRuns } from "@/lib/api/jobRuns";
import { hasPermission } from "@/lib/auth/permissions";

type JobRunListProps = {
  customerId: string;
};

export function JobRunList({ customerId }: JobRunListProps) {
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewJobRuns = hasPermission(customerQuery.data?.permissions, "ViewJobRuns");
  const jobRunsQuery = useQuery({ queryKey: ["job-runs", customerId], queryFn: () => getJobRuns(customerId), enabled: canViewJobRuns });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  if (!canViewJobRuns) {
    return <Alert severity="warning">You do not have permission to view job runs for this customer.</Alert>;
  }

  if (jobRunsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobRunsQuery.isError) {
    return <Alert severity="error">Job runs could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Typography component="h1" variant="h4">Job Runs</Typography>
      {jobRunsQuery.data.length === 0 ? (
        <Alert severity="info">No job runs have been queued.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {jobRunsQuery.data.map((jobRun) => (
              <Stack direction={{ xs: "column", sm: "row" }} key={jobRun.id} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <ReceiptLongIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{jobRun.triggerType} run</Typography>
                    <Typography color="text.secondary" variant="body2">{new Date(jobRun.queuedAt).toLocaleString()}</Typography>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
                  <JobRunStatusChip status={jobRun.status} />
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/job-runs/${jobRun.id}`} variant="outlined">Open</Button>
                </Stack>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}
    </Stack>
  );
}
