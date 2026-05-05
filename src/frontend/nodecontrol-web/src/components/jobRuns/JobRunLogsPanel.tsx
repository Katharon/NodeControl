"use client";

import FilterListIcon from "@mui/icons-material/FilterList";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { getJobRunLogs, type JobRunFailureDiagnostic, type JobRunLogEntry, type JobRunLogStream, type JobRunStatus } from "@/lib/api/jobRuns";

type JobRunLogsPanelProps = {
  customerId: string;
  failureDiagnostic?: JobRunFailureDiagnostic | null;
  jobRunId: string;
  status: JobRunStatus;
};

const activeStatuses: JobRunStatus[] = ["Queued", "Running", "Cancelling"];
type StreamFilter = "All" | JobRunLogStream;

export function JobRunLogsPanel({ customerId, failureDiagnostic, jobRunId, status }: JobRunLogsPanelProps) {
  const [streamFilter, setStreamFilter] = useState<StreamFilter>("All");
  const isActive = activeStatuses.includes(status);
  const logsQuery = useQuery({
    queryKey: ["job-run-logs", customerId, jobRunId],
    queryFn: () => getJobRunLogs(customerId, jobRunId, { limit: 500 }),
    refetchInterval: isActive ? 4000 : false,
  });
  const entries = logsQuery.data?.items ?? [];
  const visibleEntries = streamFilter === "All"
    ? entries
    : entries.filter((entry) => entry.stream === streamFilter);
  const counts = {
    All: entries.length,
    StdOut: entries.filter((entry) => entry.stream === "StdOut").length,
    StdErr: entries.filter((entry) => entry.stream === "StdErr").length,
    System: entries.filter((entry) => entry.stream === "System").length,
  };

  return (
    <Paper id="logs" sx={{ p: 3 }}>
      <Stack sx={{ gap: 2 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 1.5 }}>
          <Stack>
            <Typography component="h2" variant="h5">Logs</Typography>
            <Typography color="text.secondary" variant="body2">
              Persistierte Run-Ausgabe aus System, stdout und stderr.
            </Typography>
          </Stack>
          <Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
            {isActive ? <Chip color="info" label="Auto-refresh 4s" size="small" variant="outlined" /> : null}
            <Button
              disabled={logsQuery.isFetching}
              onClick={() => void logsQuery.refetch()}
              startIcon={logsQuery.isFetching ? <CircularProgress color="inherit" size={16} /> : <RefreshIcon />}
              variant="outlined"
            >
              Aktualisieren
            </Button>
          </Stack>
        </Stack>

        {failureDiagnostic ? (
          <Alert severity={status === "Cancelled" ? "warning" : "error"}>
            <Typography sx={{ fontWeight: 800 }}>{failureDiagnostic.title}</Typography>
            <Typography>{failureDiagnostic.summary}</Typography>
            {failureDiagnostic.nextStep ? (
              <Typography color="text.secondary" variant="body2">
                {failureDiagnostic.nextStep}
              </Typography>
            ) : null}
          </Alert>
        ) : null}

        <Stack direction={{ xs: "column", md: "row" }} sx={{ alignItems: { md: "center" }, justifyContent: "space-between", gap: 1.5 }}>
          <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
            <Chip label={`${counts.All} gesamt`} size="small" />
            <Chip color="success" label={`${counts.StdOut} stdout`} size="small" variant="outlined" />
            <Chip color="error" label={`${counts.StdErr} stderr`} size="small" variant="outlined" />
            <Chip color="info" label={`${counts.System} system`} size="small" variant="outlined" />
          </Stack>
          <ToggleButtonGroup
            exclusive
            onChange={(_event, value: StreamFilter | null) => {
              if (value) {
                setStreamFilter(value);
              }
            }}
            size="small"
            value={streamFilter}
          >
            <ToggleButton value="All">
              <FilterListIcon fontSize="small" sx={{ mr: 0.75 }} />
              Alle
            </ToggleButton>
            <ToggleButton value="System">System</ToggleButton>
            <ToggleButton value="StdOut">stdout</ToggleButton>
            <ToggleButton value="StdErr">stderr</ToggleButton>
          </ToggleButtonGroup>
        </Stack>

        {logsQuery.isPending ? (
          <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
            <CircularProgress size={22} />
            <Typography color="text.secondary">Logs werden geladen</Typography>
          </Stack>
        ) : null}
        {logsQuery.isError ? <Alert severity="error">Logs konnten nicht geladen werden.</Alert> : null}
        {logsQuery.data?.items.length === 0 ? (
          <Alert severity={isActive ? "info" : "warning"}>
            {isActive
              ? "Der Run ist aktiv, aber es wurden noch keine Log-Einträge persistiert."
              : "Dieser Run hat keine persistierten Log-Einträge."}
          </Alert>
        ) : null}
        {entries.length > 0 && visibleEntries.length === 0 ? (
          <Alert severity="info">Für diesen Stream gibt es keine Log-Einträge.</Alert>
        ) : null}
        {visibleEntries.length ? (
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
              {visibleEntries.map((entry) => (
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
