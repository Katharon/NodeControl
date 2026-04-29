"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import ScheduleIcon from "@mui/icons-material/Schedule";
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ConfirmActionButton } from "@/components/guardrails/ConfirmActionButton";
import { JobScheduleForm } from "@/components/schedules/JobScheduleForm";
import { JobScheduleStatusChip } from "@/components/schedules/JobScheduleStatusChip";
import { getCustomer } from "@/lib/api/customers";
import { archiveSchedule, createSchedule, getSchedules } from "@/lib/api/schedules";
import { hasPermission } from "@/lib/auth/permissions";

type JobScheduleListProps = {
  customerId: string;
};

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : "Not set";
}

export function JobScheduleList({ customerId }: JobScheduleListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewSchedules = hasPermission(customerQuery.data?.permissions, "ViewSchedules");
  const schedulesQuery = useQuery({ queryKey: ["schedules", customerId], queryFn: () => getSchedules(customerId), enabled: canViewSchedules });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createSchedule>[1]) => createSchedule(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["schedules", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (scheduleId: string) => archiveSchedule(customerId, scheduleId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["schedules", customerId] }),
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  if (!canViewSchedules) {
    return <Alert severity="warning">You do not have permission to view schedules for this customer.</Alert>;
  }

  if (schedulesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (schedulesQuery.isError) {
    return <Alert severity="error">Schedules could not be loaded.</Alert>;
  }

  const canManageSchedules = hasPermission(customerQuery.data.permissions, "ManageSchedules");

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">Schedules</Typography>
        {canManageSchedules ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">Neuer Schedule</Button>
        ) : null}
      </Stack>

      {schedulesQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Schedules definiert.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {schedulesQuery.data.map((schedule) => (
              <Stack direction={{ xs: "column", md: "row" }} key={schedule.id} sx={{ alignItems: { md: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <ScheduleIcon color="primary" />
                  <Stack>
                    <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
                      <Typography sx={{ fontWeight: 700 }}>{schedule.name}</Typography>
                      <JobScheduleStatusChip status={schedule.status} />
                    </Stack>
                    <Typography color="text.secondary" variant="body2">{schedule.cronExpression} · {schedule.timeZoneId}</Typography>
                    <Typography color="text.secondary" variant="body2">Next: {formatDate(schedule.nextRunAtUtc)}</Typography>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ gap: 1 }}>
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/schedules/${schedule.id}`} variant="outlined">Öffnen</Button>
                  {canManageSchedules ? (
                    <ConfirmActionButton
                      actionLabel="Archive Schedule"
                      message="Archived Schedules will no longer create scheduled Runs."
                      onConfirm={() => archiveMutation.mutateAsync(schedule.id)}
                      pending={archiveMutation.isPending}
                      startIcon={<ArchiveIcon />}
                      title="Archive this Schedule?"
                    >
                      Archive
                    </ConfirmActionButton>
                  ) : null}
                </Stack>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      <Dialog fullWidth maxWidth="md" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Neuer Schedule</DialogTitle>
        <DialogContent>
          <JobScheduleForm
            customerId={customerId}
            onSubmit={async (input) => { await createMutation.mutateAsync(input); }}
            submitLabel="Schedule anlegen"
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
