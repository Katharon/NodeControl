"use client";

import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Button,
  CircularProgress,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AuditOutcomeChip } from "@/components/audit/AuditOutcomeChip";
import { getAuditLogs, type AuditOutcome } from "@/lib/api/audit";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type AuditLogListProps = {
  customerId: string;
};

const outcomes: AuditOutcome[] = ["Succeeded", "Failed", "Denied"];

function actorLabel(actorDisplayName: string | null, actorType: string) {
  return actorDisplayName ?? actorType;
}

function entityLabel(entityType: string, entityDisplayName: string | null, entityId: string | null) {
  return entityDisplayName ?? (entityId ? `${entityType} ${entityId.slice(0, 8)}` : entityType);
}

export function AuditLogList({ customerId }: AuditLogListProps) {
  const [action, setAction] = useState("");
  const [entityType, setEntityType] = useState("");
  const [outcome, setOutcome] = useState<AuditOutcome | "">("");

  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewAudit = hasPermission(customerQuery.data?.permissions, "ViewAuditLogs");
  const auditQuery = useQuery({
    queryKey: ["audit-logs", customerId, action, entityType, outcome],
    queryFn: () => getAuditLogs(customerId, { action, entityType, outcome }),
    enabled: canViewAudit,
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  if (!canViewAudit) {
    return <Alert severity="warning">You do not have permission to view audit logs for this customer.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">
          Audit
        </Typography>
        <Button
          disabled={auditQuery.isFetching}
          onClick={() => void auditQuery.refetch()}
          startIcon={<RefreshIcon />}
          sx={{ alignSelf: { xs: "flex-start", sm: "center" } }}
          variant="outlined"
        >
          Refresh
        </Button>
      </Stack>

      <Paper sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} sx={{ gap: 2 }}>
          <TextField
            label="Action"
            onChange={(event) => setAction(event.target.value)}
            size="small"
            value={action}
          />
          <TextField
            label="Entity type"
            onChange={(event) => setEntityType(event.target.value)}
            size="small"
            value={entityType}
          />
          <TextField
            label="Outcome"
            onChange={(event) => setOutcome(event.target.value as AuditOutcome | "")}
            select
            size="small"
            sx={{ minWidth: 180 }}
            value={outcome}
          >
            <MenuItem value="">Any</MenuItem>
            {outcomes.map((item) => (
              <MenuItem key={item} value={item}>
                {item}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </Paper>

      {auditQuery.isPending ? <CircularProgress size={22} /> : null}
      {auditQuery.isError ? <Alert severity="error">Audit logs could not be loaded.</Alert> : null}
      {auditQuery.data?.items.length === 0 ? <Alert severity="info">No audit entries match the current filters.</Alert> : null}

      {auditQuery.data && auditQuery.data.items.length > 0 ? (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Time</TableCell>
                <TableCell>Actor</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Entity</TableCell>
                <TableCell>Outcome</TableCell>
                <TableCell>Message</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {auditQuery.data.items.map((entry) => (
                <TableRow key={entry.id}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {new Date(entry.createdAtUtc).toLocaleString()}
                  </TableCell>
                  <TableCell>{actorLabel(entry.actorDisplayName, entry.actorType)}</TableCell>
                  <TableCell>{entry.action}</TableCell>
                  <TableCell>{entityLabel(entry.entityType, entry.entityDisplayName, entry.entityId)}</TableCell>
                  <TableCell>
                    <AuditOutcomeChip outcome={entry.outcome} />
                  </TableCell>
                  <TableCell>{entry.message}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}
    </Stack>
  );
}
