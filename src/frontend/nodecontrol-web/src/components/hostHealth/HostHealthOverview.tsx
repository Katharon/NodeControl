"use client";

import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ConnectionCheckButton } from "@/components/hostHealth/ConnectionCheckButton";
import { HostConnectionStatusChip } from "@/components/hostHealth/HostConnectionStatusChip";
import { getCustomer } from "@/lib/api/customers";
import { getHostHealth } from "@/lib/api/hostConnectionChecks";
import { hasPermission } from "@/lib/auth/permissions";

type HostHealthOverviewProps = {
  customerId: string;
};

export function HostHealthOverview({ customerId }: HostHealthOverviewProps) {
  const customerQuery = useQuery({
    queryKey: ["customer", customerId],
    queryFn: () => getCustomer(customerId),
  });
  const hostHealthQuery = useQuery({
    queryKey: ["host-health", customerId],
    queryFn: () => getHostHealth(customerId),
    refetchInterval: 5000,
  });

  if (customerQuery.isPending || hostHealthQuery.isPending) {
    return (
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography>Hostzustand wird geladen</Typography>
        </Stack>
      </Paper>
    );
  }

  if (customerQuery.isError) {
    return <Alert severity="error">Dieser Kunde konnte nicht geladen werden.</Alert>;
  }

  if (hostHealthQuery.isError) {
    return <Alert severity="error">Hostzustand konnte nicht geladen werden.</Alert>;
  }

  const customer = customerQuery.data;
  const canViewNodes = hasPermission(customer.permissions, "ViewNodes");
  const canManageNodes = hasPermission(customer.permissions, "ManageNodes");

  if (!canViewNodes) {
    return <Alert severity="warning">Du hast keine Berechtigung, den Hostzustand für diesen Kunden anzusehen.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2 }}
      >
        <Box>
          <Typography component="h1" variant="h4">
            Hostzustand
          </Typography>
          <Typography color="text.secondary">
            {customer.name} · Letzte TCP-Erreichbarkeitsprüfung je Host.
          </Typography>
        </Box>
        <Button
          disabled={hostHealthQuery.isFetching}
          onClick={() => hostHealthQuery.refetch()}
          startIcon={<RefreshIcon />}
          variant="outlined"
        >
          Aktualisieren
        </Button>
      </Stack>

      {hostHealthQuery.data.targets.length === 0 ? (
        <Alert severity="info">Noch keine Hosts oder Control Hosts definiert.</Alert>
      ) : (
        <Paper sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Host</TableCell>
                <TableCell>Typ</TableCell>
                <TableCell>Endpoint</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Zuletzt geprüft</TableCell>
                <TableCell>Ergebnis</TableCell>
                <TableCell align="right">Aktion</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {hostHealthQuery.data.targets.map((target) => {
                const latestCheck = target.latestCheck;
                const message =
                  latestCheck?.resultMessage ??
                  latestCheck?.errorMessage ??
                  "Noch keine Prüfung durchgeführt.";
                return (
                  <TableRow key={`${target.targetType}-${target.targetId}`}>
                    <TableCell sx={{ fontWeight: 700 }}>{target.name}</TableCell>
                    <TableCell>{target.targetType === "ControlNode" ? "Control Host" : "Host"}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      {target.hostname}:{target.port}
                    </TableCell>
                    <TableCell>
                      <HostConnectionStatusChip status={latestCheck?.status} />
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      {latestCheck?.finishedAtUtc
                        ? new Date(latestCheck.finishedAtUtc).toLocaleString()
                        : latestCheck?.queuedAtUtc
                          ? new Date(latestCheck.queuedAtUtc).toLocaleString()
                          : "n/a"}
                    </TableCell>
                    <TableCell sx={{ maxWidth: 360, overflowWrap: "anywhere" }}>
                      {message}
                    </TableCell>
                    <TableCell align="right">
                      {canManageNodes ? (
                        <ConnectionCheckButton
                          customerId={customerId}
                          onQueued={() => hostHealthQuery.refetch()}
                          targetId={target.targetId}
                          targetType={target.targetType}
                        />
                      ) : null}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </Paper>
      )}
    </Stack>
  );
}
