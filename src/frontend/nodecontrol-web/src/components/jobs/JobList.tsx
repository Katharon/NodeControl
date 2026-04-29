"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import WorkIcon from "@mui/icons-material/Work";
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
import { JobForm } from "@/components/jobs/JobForm";
import { RunJobButton } from "@/components/jobs/RunJobButton";
import { getCustomer } from "@/lib/api/customers";
import { archiveJob, createJob, getJobs } from "@/lib/api/jobs";
import { hasPermission } from "@/lib/auth/permissions";

type JobListProps = {
  customerId: string;
};

export function JobList({ customerId }: JobListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewJobs = hasPermission(customerQuery.data?.permissions, "ViewPlaybooks");
  const jobsQuery = useQuery({ queryKey: ["jobs", customerId], queryFn: () => getJobs(customerId), enabled: canViewJobs });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createJob>[1]) => createJob(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["jobs", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (jobId: string) => archiveJob(customerId, jobId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["jobs", customerId] }),
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  if (!canViewJobs) {
    return <Alert severity="warning">Du hast keine Berechtigung, Actions für diesen Kunden anzusehen.</Alert>;
  }

  if (jobsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobsQuery.isError) {
    return <Alert severity="error">Actions konnten nicht geladen werden.</Alert>;
  }

  const canManageJobs = hasPermission(customerQuery.data.permissions, "ManagePlaybooks");
  const canRunJobs = hasPermission(customerQuery.data.permissions, "RunJobs");

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Stack>
          <Typography component="h1" variant="h4">Actions</Typography>
          <Typography color="text.secondary">
            Wiederverwendbare Ausführungsdefinitionen für Runs.
          </Typography>
        </Stack>
        {canManageJobs ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">Neue Action</Button>
        ) : null}
      </Stack>

      {jobsQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Actions definiert.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {jobsQuery.data.map((job) => (
              <Stack direction={{ xs: "column", sm: "row" }} key={job.id} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <WorkIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{job.name}</Typography>
                    <Typography color="text.secondary" variant="body2">{job.slug}</Typography>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ gap: 1 }}>
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/actions/${job.id}`} variant="outlined">Öffnen</Button>
                  {canRunJobs ? <RunJobButton customerId={customerId} jobId={job.id} /> : null}
                  {canManageJobs ? (
                    <ConfirmActionButton
                      actionLabel="Archive Action"
                      message="Archived Actions disappear from normal selection and cannot be queued for new Runs."
                      onConfirm={() => archiveMutation.mutateAsync(job.id)}
                      pending={archiveMutation.isPending}
                      startIcon={<ArchiveIcon />}
                      title="Archive this Action?"
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
        <DialogTitle>Neue Action</DialogTitle>
        <DialogContent>
          <JobForm
            customerId={customerId}
            onSubmit={async (input) => { await createMutation.mutateAsync(input); }}
            submitLabel="Action anlegen"
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
