"use client";

import RefreshIcon from "@mui/icons-material/Refresh";
import { Alert, Box, Button, Chip, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getJobRunLogs, type JobRunLogEntry, type JobRunStatus } from "@/lib/api/jobRuns";

type JobRunLogsPanelProps = {
  customerId: string;
  jobRunId: string;
  status: JobRunStatus;
};

const activeStatuses: JobRunStatus[] = ["Queued", "Running"];

export function JobRunLogsPanel({ customerId, jobRunId, status }: JobRunLogsPanelProps) {
  const isActive = activeStatuses.includes(status);
  const logsQuery = useQuery({
    queryKey: ["job-run-logs", customerId, jobRunId],
    queryFn: () => getJobRunLogs(customerId, jobRunId, { limit: 500 }),
    refetchInterval: isActive ? 2000 : false,
  });

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 2 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 1.5 }}>
          <Stack>
            <Typography component="h2" variant="h5">Logs</Typography>
            <Typography color="text.secondary" variant="body2">Latest persisted JobRun log entries</Typography>
          </Stack>
          <Button
            disabled={logsQuery.isFetching}
            onClick={() => void logsQuery.refetch()}
            startIcon={<RefreshIcon />}
            variant="outlined"
          >
            Refresh
          </Button>
        </Stack>

        {logsQuery.isPending ? <CircularProgress size={22} /> : null}
        {logsQuery.isError ? <Alert severity="error">Logs could not be loaded.</Alert> : null}
        {logsQuery.data?.items.length === 0 ? <Alert severity="info">No log entries have been persisted yet.</Alert> : null}
        {logsQuery.data?.items.length ? (
          <Box
            sx={{
              bgcolor: "grey.900",
              borderRadius: 1,
              color: "grey.100",
              fontFamily: "monospace",
              maxHeight: 460,
              overflow: "auto",
              p: 2,
            }}
          >
            <Stack component="ol" sx={{ gap: 0.75, listStyle: "none", m: 0, p: 0 }}>
              {logsQuery.data.items.map((entry) => (
                <LogLine entry={entry} key={entry.id} />
              ))}
            </Stack>
          </Box>
        ) : null}
      </Stack>
    </Paper>
  );
}

function LogLine({ entry }: { entry: JobRunLogEntry }) {
  const color = entry.stream === "StdErr" || entry.level === "Error"
    ? "error"
    : entry.stream === "System"
      ? "info"
      : "success";

  return (
    <Stack component="li" direction="row" sx={{ alignItems: "flex-start", gap: 1, minWidth: 0 }}>
      <Typography sx={{ color: "grey.500", flexShrink: 0, fontFamily: "monospace", pt: 0.25 }} variant="caption">
        {entry.sequence}
      </Typography>
      <Typography sx={{ color: "grey.500", flexShrink: 0, fontFamily: "monospace", pt: 0.25 }} variant="caption">
        {new Date(entry.timestampUtc).toLocaleTimeString()}
      </Typography>
      <Chip color={color} label={entry.stream} size="small" sx={{ flexShrink: 0 }} variant="outlined" />
      <Typography sx={{ color: "inherit", fontFamily: "monospace", overflowWrap: "anywhere", whiteSpace: "pre-wrap" }} variant="body2">
        {entry.message}
      </Typography>
    </Stack>
  );
}
