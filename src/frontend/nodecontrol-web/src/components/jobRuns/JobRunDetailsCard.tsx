"use client";

import ArticleIcon from "@mui/icons-material/Article";
import HealthAndSafetyIcon from "@mui/icons-material/HealthAndSafety";
import HistoryIcon from "@mui/icons-material/History";
import HubIcon from "@mui/icons-material/Hub";
import InventoryIcon from "@mui/icons-material/Inventory";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import RefreshIcon from "@mui/icons-material/Refresh";
import RocketLaunchIcon from "@mui/icons-material/RocketLaunch";
import TimerIcon from "@mui/icons-material/Timer";
import WorkIcon from "@mui/icons-material/Work";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  LinearProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import type { ChipProps } from "@mui/material";
import type { SvgIconComponent } from "@mui/icons-material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import type { ReactNode } from "react";
import { CancelJobRunButton } from "@/components/jobRuns/CancelJobRunButton";
import { JobRunStatusChip } from "@/components/jobRuns/JobRunStatusChip";
import { JobRunLogsPanel } from "@/components/jobRuns/JobRunLogsPanel";
import { RetryJobRunButton } from "@/components/jobRuns/RetryJobRunButton";
import { ApiError } from "@/lib/api/apiClient";
import { getControlNodes } from "@/lib/api/controlNodes";
import { getCustomer } from "@/lib/api/customers";
import { getInventoryGroups } from "@/lib/api/inventoryGroups";
import { getJobRun, type JobRun, type JobRunStatus } from "@/lib/api/jobRuns";
import { getJob } from "@/lib/api/jobs";
import { getPlaybooks } from "@/lib/api/playbooks";
import { getVariableSets } from "@/lib/api/variableSets";
import { hasPermission } from "@/lib/auth/permissions";

type JobRunDetailsCardProps = {
  customerId: string;
  jobRunId: string;
};

const activeStatuses: JobRunStatus[] = ["Queued", "Running", "Cancelling"];
const retryableStatuses: JobRunStatus[] = ["Failed", "TimedOut", "Cancelled"];
const cancellableStatuses: JobRunStatus[] = ["Queued", "Running", "Cancelling"];

export function JobRunDetailsCard({ customerId, jobRunId }: JobRunDetailsCardProps) {
  const queryClient = useQueryClient();
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewJobRuns = hasPermission(customerQuery.data?.permissions, "ViewJobRuns");
  const canCancelJobRuns = hasPermission(customerQuery.data?.permissions, "CancelJobRuns");
  const canRetryJobRuns = hasPermission(customerQuery.data?.permissions, "RetryJobRuns");
  const canViewNodes = hasPermission(customerQuery.data?.permissions, "ViewNodes");
  const canViewPlaybooks = hasPermission(customerQuery.data?.permissions, "ViewPlaybooks");
  const canViewAuditLogs = hasPermission(customerQuery.data?.permissions, "ViewAuditLogs");
  const jobRunQuery = useQuery({
    queryKey: ["job-run", customerId, jobRunId],
    queryFn: () => getJobRun(customerId, jobRunId),
    enabled: canViewJobRuns,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status && activeStatuses.includes(status) ? 4000 : false;
    },
  });
  const jobRun = jobRunQuery.data;
  const isActive = jobRun ? activeStatuses.includes(jobRun.status) : false;
  const jobQuery = useQuery({
    queryKey: ["job", customerId, jobRun?.jobId],
    queryFn: () => getJob(customerId, jobRun?.jobId ?? ""),
    enabled: Boolean(jobRun?.jobId && canViewPlaybooks),
  });
  const job = jobQuery.data ?? null;
  const controlNodesQuery = useQuery({
    queryKey: ["control-nodes", customerId],
    queryFn: () => getControlNodes(customerId),
    enabled: Boolean(jobRun?.controlNodeId && canViewNodes),
  });
  const inventoryGroupsQuery = useQuery({
    queryKey: ["inventory-groups", customerId],
    queryFn: () => getInventoryGroups(customerId),
    enabled: Boolean(job && canViewNodes),
  });
  const playbooksQuery = useQuery({
    queryKey: ["playbooks", customerId],
    queryFn: () => getPlaybooks(customerId),
    enabled: Boolean(job && canViewPlaybooks),
  });
  const variableSetsQuery = useQuery({
    queryKey: ["variable-sets", customerId],
    queryFn: () => getVariableSets(customerId),
    enabled: Boolean(job?.variableSetId && canViewPlaybooks),
  });

  if (customerQuery.isPending) {
    return <LoadingCard label="Run Center wird geladen" />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">{errorMessage(customerQuery.error, "Dieser Kunde konnte nicht geladen werden.")}</Alert>;
  }

  if (!canViewJobRuns) {
    return <Alert severity="warning">Du hast keine Berechtigung, Runs für diesen Kunden anzusehen.</Alert>;
  }

  if (jobRunQuery.isPending) {
    return <LoadingCard label="Run wird geladen" />;
  }

  if (jobRunQuery.isError) {
    return <Alert severity="error">{errorMessage(jobRunQuery.error, "Dieser Run konnte nicht geladen werden.")}</Alert>;
  }

  if (!jobRun) {
    return <Alert severity="error">Dieser Run konnte nicht geladen werden.</Alert>;
  }

  const controlNode = controlNodesQuery.data?.find((item) => item.id === jobRun.controlNodeId) ?? null;
  const inventoryGroup = inventoryGroupsQuery.data?.find((item) => item.id === job?.inventoryGroupId) ?? null;
  const playbook = playbooksQuery.data?.find((item) => item.id === job?.playbookId) ?? null;
  const variableSet = variableSetsQuery.data?.find((item) => item.id === job?.variableSetId) ?? null;
  const canCancel = canCancelJobRuns && cancellableStatuses.includes(jobRun.status);
  const canRetry = canRetryJobRuns && retryableStatuses.includes(jobRun.status);
  const pathRows = [
    ["Workspace", jobRun.workspacePath],
    ["Stdout log", jobRun.stdoutLogPath],
    ["Stderr log", jobRun.stderrLogPath],
  ] as const;

  return (
    <Stack sx={{ gap: 2.5 }}>
      <Paper
        sx={{
          borderLeft: 6,
          borderColor: statusBorderColor(jobRun.status),
          overflow: "hidden",
          p: { xs: 2, md: 3 },
        }}
      >
        <Stack sx={{ gap: 2.5 }}>
          <Stack direction={{ xs: "column", md: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
            <Stack direction="row" sx={{ alignItems: "flex-start", gap: 1.5, minWidth: 0 }}>
              <ReceiptLongIcon color="primary" sx={{ mt: 0.5 }} />
              <Stack sx={{ minWidth: 0 }}>
                <Typography component="h1" variant="h4">
                  Run Center
                </Typography>
                <Typography color="text.secondary" sx={{ overflowWrap: "anywhere" }}>
                  {jobRun.id}
                </Typography>
                <Typography color="text.secondary" variant="body2">
                  {customerQuery.data.name} · {job?.name ?? `Action ${jobRun.jobId.slice(0, 8)}`}
                </Typography>
              </Stack>
            </Stack>
            <Stack sx={{ alignItems: { xs: "flex-start", md: "flex-end" }, gap: 1 }}>
              <Chip
                color={statusChipColor(jobRun.status)}
                label={jobRun.status}
                sx={{ fontSize: 18, fontWeight: 800, height: 40, px: 1 }}
              />
              <Typography color="text.secondary" variant="body2">
                {isActive ? "Aktive Ausführung" : "Ausführung abgeschlossen oder gestoppt"}
              </Typography>
            </Stack>
          </Stack>

          {isActive ? <LinearProgress /> : null}

          <SummaryGrid
            items={[
              ["Status", <JobRunStatusChip key="status" status={jobRun.status} />],
              ["Trigger", jobRun.triggerType],
              ["Queued", formatDateTime(jobRun.queuedAt)],
              ["Started", jobRun.startedAt ? formatDateTime(jobRun.startedAt) : "Noch nicht gestartet"],
              ["Finished", jobRun.finishedAt ? formatDateTime(jobRun.finishedAt) : "Noch nicht beendet"],
              ["Duration", durationLabel(jobRun)],
              ["Exit code", jobRun.exitCode?.toString() ?? "n/a"],
              ["Retry", jobRun.retryAttempt > 0 ? `Attempt ${jobRun.retryAttempt}` : "Original run"],
            ]}
          />

          {jobRun.errorMessage ? <Alert severity="error">{jobRun.errorMessage}</Alert> : null}

          <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
            <Button
              disabled={jobRunQuery.isFetching}
              onClick={async () => {
                await Promise.all([
                  jobRunQuery.refetch(),
                  queryClient.invalidateQueries({ queryKey: ["job-run-logs", customerId, jobRunId] }),
                ]);
              }}
              startIcon={<RefreshIcon />}
              variant="outlined"
            >
              Aktualisieren
            </Button>
            {canCancel ? <CancelJobRunButton customerId={customerId} jobRunId={jobRunId} status={jobRun.status} /> : null}
            {canRetry ? <RetryJobRunButton customerId={customerId} jobRunId={jobRunId} /> : null}
          </Stack>
        </Stack>
      </Paper>

      <Stack direction={{ xs: "column", lg: "row" }} sx={{ alignItems: "stretch", gap: 2 }}>
        <Paper sx={{ flex: 2, p: { xs: 2, md: 3 } }}>
          <Stack sx={{ gap: 2 }}>
            <SectionHeading
              description="Welche Action diesen Run erzeugt hat und welche Ausführungsbausteine dahinterliegen."
              icon={WorkIcon}
              title="Action & Kontext"
            />
            {jobQuery.isPending ? <LoadingInline label="Action-Kontext wird geladen" /> : null}
            {jobQuery.isError ? (
              <Alert severity="warning">{errorMessage(jobQuery.error, "Action-Kontext konnte nicht geladen werden.")}</Alert>
            ) : null}
            {!canViewPlaybooks ? (
              <Alert severity="info">Du kannst diesen Run sehen, aber nicht die Action-Definition dieses Kunden.</Alert>
            ) : null}
            {job ? (
              <>
                <SummaryGrid
                  items={[
                    ["Action", job.name],
                    ["Slug", job.slug],
                    ["Timeout", `${job.defaultTimeoutSeconds}s`],
                    ["Run Control Host", contextValue(controlNodesQuery.isPending, controlNodesQuery.isError, controlNode?.name)],
                    ["Inventar", contextValue(inventoryGroupsQuery.isPending, inventoryGroupsQuery.isError, inventoryGroup?.name)],
                    ["Playbook", contextValue(playbooksQuery.isPending, playbooksQuery.isError, playbook?.name)],
                    ["Variablen", job.variableSetId ? contextValue(variableSetsQuery.isPending, variableSetsQuery.isError, variableSet?.name) : "Keine"],
                  ]}
                />
                {job.description ? (
                  <Typography color="text.secondary">{job.description}</Typography>
                ) : null}
              </>
            ) : null}
            <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
              <Button
                component={Link}
                href={`/customers/${customerId}/actions/${jobRun.jobId}`}
                startIcon={<OpenInNewIcon />}
                variant="outlined"
              >
                Action öffnen
              </Button>
              {playbook ? (
                <Button
                  component={Link}
                  href={`/customers/${customerId}/playbooks/${playbook.id}`}
                  startIcon={<ArticleIcon />}
                  variant="outlined"
                >
                  Playbook öffnen
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </Paper>

        <Paper sx={{ flex: 1, p: { xs: 2, md: 3 } }}>
          <Stack sx={{ gap: 2 }}>
            <SectionHeading
              description="Schnelle Wechselpunkte für Demo, Betrieb und Nachvollziehbarkeit."
              icon={RocketLaunchIcon}
              title="Verknüpfungen"
            />
            <RelatedLink href={`/customers/${customerId}/runs`} icon={ReceiptLongIcon} label="Runs" />
            <RelatedLink href={`/customers/${customerId}/run-wizard`} icon={RocketLaunchIcon} label="Ausführungsassistent" />
            <RelatedLink href={`/customers/${customerId}/host-health`} icon={HealthAndSafetyIcon} label="Hostzustand" />
            {canViewAuditLogs ? (
              <RelatedLink href={`/customers/${customerId}/audit`} icon={HistoryIcon} label="Audit" />
            ) : null}
            {jobRun.controlNodeId ? (
              <RelatedLink href={`/customers/${customerId}/hosts`} icon={HubIcon} label="Hosts" />
            ) : null}
            {job?.inventoryGroupId ? (
              <RelatedLink href={`/customers/${customerId}/inventories`} icon={InventoryIcon} label="Inventare" />
            ) : null}
          </Stack>
        </Paper>
      </Stack>

      <JobRunLogsPanel customerId={customerId} jobRunId={jobRunId} status={jobRun.status} />

      <Paper sx={{ p: { xs: 2, md: 3 } }}>
        <Stack sx={{ gap: 2 }}>
          <SectionHeading
            description="Technische Dateipfade stammen aus dem Worker-Workspace und dienen nur der Nachvollziehbarkeit."
            icon={TimerIcon}
            title="Run Details"
          />
          <SummaryGrid
            items={[
              ["Run ID", jobRun.id],
              ["Action ID", jobRun.jobId],
              ["Control Host ID", jobRun.controlNodeId],
              ["Triggered by", jobRun.triggeredByUserId ?? "n/a"],
              ["Schedule", jobRun.scheduleId ?? "n/a"],
              ["Retried from", jobRun.retriedFromJobRunId ?? "n/a"],
              ["Cancellation requested", jobRun.cancellationRequestedAtUtc ? formatDateTime(jobRun.cancellationRequestedAtUtc) : "n/a"],
              ["Cancellation by", jobRun.cancellationRequestedByUserId ?? "n/a"],
              ["Cancellation reason", jobRun.cancellationReason ?? "n/a"],
            ]}
          />
          {pathRows.some(([, value]) => value) ? (
            <Stack divider={<Divider />} sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
              {pathRows.map(([label, value]) => value ? (
                <Stack key={label} sx={{ p: 1.5 }}>
                  <Typography color="text.secondary" variant="body2">{label}</Typography>
                  <Typography sx={{ fontFamily: "monospace", overflowWrap: "anywhere" }}>{value}</Typography>
                </Stack>
              ) : null)}
            </Stack>
          ) : null}
        </Stack>
      </Paper>
    </Stack>
  );
}

function LoadingCard({ label }: { label: string }) {
  return (
    <Paper sx={{ p: 3 }}>
      <LoadingInline label={label} />
    </Paper>
  );
}

function LoadingInline({ label }: { label: string }) {
  return (
    <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
      <CircularProgress size={20} />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  );
}

function SectionHeading({
  description,
  icon: Icon,
  title,
}: {
  description: string;
  icon: SvgIconComponent;
  title: string;
}) {
  return (
    <Stack direction="row" sx={{ alignItems: "flex-start", gap: 1.5 }}>
      <Icon color="primary" sx={{ mt: 0.25 }} />
      <Stack>
        <Typography component="h2" variant="h5">{title}</Typography>
        <Typography color="text.secondary" variant="body2">{description}</Typography>
      </Stack>
    </Stack>
  );
}

function SummaryGrid({ items }: { items: [string, ReactNode][] }) {
  return (
    <Box
      sx={{
        display: "grid",
        gap: 1,
        gridTemplateColumns: { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))", xl: "repeat(4, minmax(0, 1fr))" },
      }}
    >
      {items.map(([label, value]) => (
        <Box
          key={label}
          sx={{
            border: 1,
            borderColor: "divider",
            borderRadius: 1,
            minWidth: 0,
            p: 1.5,
          }}
        >
          <Typography color="text.secondary" variant="body2">{label}</Typography>
          <Typography component="div" sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>
            {value}
          </Typography>
        </Box>
      ))}
    </Box>
  );
}

function RelatedLink({
  href,
  icon: Icon,
  label,
}: {
  href: string;
  icon: SvgIconComponent;
  label: string;
}) {
  return (
    <Button
      component={Link}
      href={href}
      startIcon={<Icon />}
      sx={{ justifyContent: "flex-start" }}
      variant="outlined"
    >
      {label}
    </Button>
  );
}

function statusChipColor(status: JobRunStatus): ChipProps["color"] {
  if (status === "Running") {
    return "info";
  }

  if (status === "Succeeded") {
    return "success";
  }

  if (status === "Failed" || status === "TimedOut") {
    return "error";
  }

  if (status === "Cancelled" || status === "Cancelling") {
    return "warning";
  }

  return "default";
}

function statusBorderColor(status: JobRunStatus) {
  if (status === "Succeeded") {
    return "success.main";
  }

  if (status === "Running") {
    return "info.main";
  }

  if (status === "Failed" || status === "TimedOut") {
    return "error.main";
  }

  if (status === "Cancelled" || status === "Cancelling") {
    return "warning.main";
  }

  return "divider";
}

function contextValue(isLoading: boolean, isError: boolean, value: string | undefined) {
  if (isLoading) {
    return "Lädt...";
  }

  if (isError) {
    return "Nicht verfügbar";
  }

  return value ?? "Nicht gefunden";
}

function durationLabel(jobRun: JobRun) {
  const start = jobRun.startedAt ?? jobRun.queuedAt;
  const end = jobRun.finishedAt;

  if (!end) {
    return activeStatuses.includes(jobRun.status) ? `Seit ${formatDateTime(start)}` : "n/a";
  }

  const durationMs = new Date(end).getTime() - new Date(start).getTime();
  if (!Number.isFinite(durationMs) || durationMs < 0) {
    return "n/a";
  }

  const seconds = Math.floor(durationMs / 1000);
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  return minutes > 0 ? `${minutes}m ${remainingSeconds}s` : `${remainingSeconds}s`;
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}

function errorMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError && error.status === 403) {
    return "Du hast keine Berechtigung für diese Daten.";
  }

  if (error instanceof ApiError && error.status === 404) {
    return "Der angeforderte Run wurde nicht gefunden.";
  }

  if (error instanceof ApiError && error.status === 401) {
    return "Bitte melde dich an, um diese Daten zu sehen.";
  }

  return fallback;
}
