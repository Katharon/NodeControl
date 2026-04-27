"use client";

import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import {
  Alert,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { CancelJobRunButton } from "@/components/jobRuns/CancelJobRunButton";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { RetryJobRunButton } from "@/components/jobRuns/RetryJobRunButton";
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
    return <Alert severity="warning">Du hast keine Berechtigung, Runs für diesen Kunden anzusehen.</Alert>;
  }

  if (jobRunsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobRunsQuery.isError) {
    return <Alert severity="error">Runs konnten nicht geladen werden.</Alert>;
  }

  const canCancelJobRuns = hasPermission(customerQuery.data.permissions, "CancelJobRuns");
  const canRetryJobRuns = hasPermission(customerQuery.data.permissions, "RetryJobRuns");

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
          <ReceiptLongIcon color="primary" />
          <Typography component="h1" variant="h4">
            Runs
          </Typography>
        </Stack>
        <Button href={`/customers/${customerId}/actions`} sx={{ alignSelf: { xs: "flex-start", sm: "center" } }} variant="contained">
          Neuer Run
        </Button>
      </Stack>
      {jobRunsQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Runs vorhanden.</Alert>
      ) : (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Playbook or Action</TableCell>
                <TableCell>Started</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Summary</TableCell>
                <TableCell align="right"> </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
            {jobRunsQuery.data.map((jobRun) => (
              <TableRow key={jobRun.id}>
                <TableCell sx={{ fontFamily: "monospace", maxWidth: 170, overflowWrap: "anywhere" }}>
                  {jobRun.id}
                </TableCell>
                <TableCell>
                  <Typography sx={{ fontWeight: 700 }} variant="body2">
                    Action {jobRun.jobId.slice(0, 8)}
                  </Typography>
                  <Typography color="text.secondary" variant="caption">
                    {jobRun.triggerType}
                  </Typography>
                </TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>
                  {jobRun.startedAt ? new Date(jobRun.startedAt).toLocaleString() : new Date(jobRun.queuedAt).toLocaleString()}
                </TableCell>
                <TableCell>
                  <JobRunStatusChip status={jobRun.status} />
                </TableCell>
                <TableCell>
                  {jobRun.errorMessage ?? `Retry ${jobRun.retryAttempt} · exit ${jobRun.exitCode ?? "n/a"}`}
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" sx={{ alignItems: "center", justifyContent: "flex-end", gap: 1 }}>
                  {canCancelJobRuns && (jobRun.status === "Queued" || jobRun.status === "Running" || jobRun.status === "Cancelling") ? (
                    <CancelJobRunButton customerId={customerId} jobRunId={jobRun.id} status={jobRun.status} />
                  ) : null}
                  {canRetryJobRuns && (jobRun.status === "Failed" || jobRun.status === "TimedOut" || jobRun.status === "Cancelled") ? (
                    <RetryJobRunButton customerId={customerId} jobRunId={jobRun.id} />
                  ) : null}
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/runs/${jobRun.id}`} variant="outlined">Öffnen</Button>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}
