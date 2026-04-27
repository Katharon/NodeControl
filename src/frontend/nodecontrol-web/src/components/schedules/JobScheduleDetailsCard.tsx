"use client";

import ArchiveIcon from "@mui/icons-material/Archive";
import PauseIcon from "@mui/icons-material/Pause";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import { Alert, Button, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { JobScheduleForm } from "@/components/schedules/JobScheduleForm";
import { JobScheduleStatusChip } from "@/components/schedules/JobScheduleStatusChip";
import { getCustomer } from "@/lib/api/customers";
import { archiveSchedule, getSchedule, pauseSchedule, resumeSchedule, updateSchedule } from "@/lib/api/schedules";
import { hasPermission } from "@/lib/auth/permissions";

type JobScheduleDetailsCardProps = {
  customerId: string;
  scheduleId: string;
};

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : "Not set";
}

export function JobScheduleDetailsCard({ customerId, scheduleId }: JobScheduleDetailsCardProps) {
  const queryClient = useQueryClient();
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewSchedules = hasPermission(customerQuery.data?.permissions, "ViewSchedules");
  const scheduleQuery = useQuery({ queryKey: ["schedule", customerId, scheduleId], queryFn: () => getSchedule(customerId, scheduleId), enabled: canViewSchedules });
  const invalidateSchedule = async () => {
    await queryClient.invalidateQueries({ queryKey: ["schedule", customerId, scheduleId] });
    await queryClient.invalidateQueries({ queryKey: ["schedules", customerId] });
  };
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateSchedule>[2]) => updateSchedule(customerId, scheduleId, input),
    onSuccess: invalidateSchedule,
  });
  const pauseMutation = useMutation({ mutationFn: () => pauseSchedule(customerId, scheduleId), onSuccess: invalidateSchedule });
  const resumeMutation = useMutation({ mutationFn: () => resumeSchedule(customerId, scheduleId), onSuccess: invalidateSchedule });
  const archiveMutation = useMutation({ mutationFn: () => archiveSchedule(customerId, scheduleId), onSuccess: invalidateSchedule });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This schedule could not be loaded.</Alert>;
  }

  if (!canViewSchedules) {
    return <Alert severity="warning">You do not have permission to view schedules for this customer.</Alert>;
  }

  if (scheduleQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (scheduleQuery.isError) {
    return <Alert severity="error">This schedule could not be loaded.</Alert>;
  }

  const schedule = scheduleQuery.data;
  const canManageSchedules = hasPermission(customerQuery.data.permissions, "ManageSchedules");

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack sx={{ gap: 0.5 }}>
            <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
              <Typography component="h1" variant="h4">{schedule.name}</Typography>
              <JobScheduleStatusChip status={schedule.status} />
            </Stack>
            <Typography color="text.secondary">{schedule.slug}</Typography>
            {schedule.description ? <Typography>{schedule.description}</Typography> : null}
          </Stack>
          {canManageSchedules ? (
            <Stack direction="row" sx={{ alignSelf: { sm: "flex-start" }, gap: 1 }}>
              {schedule.status === "Active" ? (
                <Button disabled={pauseMutation.isPending} onClick={() => pauseMutation.mutate()} startIcon={<PauseIcon />} variant="outlined">
                  Pause
                </Button>
              ) : null}
              {schedule.status === "Paused" ? (
                <Button disabled={resumeMutation.isPending} onClick={() => resumeMutation.mutate()} startIcon={<PlayArrowIcon />} variant="outlined">
                  Resume
                </Button>
              ) : null}
              <Button color="warning" disabled={archiveMutation.isPending} onClick={() => archiveMutation.mutate()} startIcon={<ArchiveIcon />} variant="outlined">
                Archive
              </Button>
            </Stack>
          ) : null}
        </Stack>

        <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 3 }}>
          <Stack>
            <Typography color="text.secondary" variant="body2">Cron</Typography>
            <Typography>{schedule.cronExpression}</Typography>
          </Stack>
          <Stack>
            <Typography color="text.secondary" variant="body2">Time zone</Typography>
            <Typography>{schedule.timeZoneId}</Typography>
          </Stack>
          <Stack>
            <Typography color="text.secondary" variant="body2">Next run</Typography>
            <Typography>{formatDate(schedule.nextRunAtUtc)}</Typography>
          </Stack>
          <Stack>
            <Typography color="text.secondary" variant="body2">Last run</Typography>
            <Typography>{formatDate(schedule.lastRunAtUtc)}</Typography>
          </Stack>
        </Stack>

        <Stack>
          <Typography color="text.secondary" variant="body2">Last JobRun</Typography>
          {schedule.lastJobRunId ? (
            <Button href={`/customers/${customerId}/job-runs/${schedule.lastJobRunId}`} sx={{ alignSelf: "flex-start", px: 0 }} variant="text">
              {schedule.lastJobRunId}
            </Button>
          ) : (
            <Typography>Not set</Typography>
          )}
        </Stack>

        {canManageSchedules ? (
          <>
            <Divider />
            <JobScheduleForm
              customerId={customerId}
              onSubmit={async (input) => { await updateMutation.mutateAsync(input); }}
              schedule={schedule}
              submitLabel="Save schedule"
            />
          </>
        ) : null}
      </Stack>
    </Paper>
  );
}
