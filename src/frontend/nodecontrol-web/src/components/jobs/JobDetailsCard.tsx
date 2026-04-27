"use client";

import { Alert, Button, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { JobForm } from "@/components/jobs/JobForm";
import { RunJobButton } from "@/components/jobs/RunJobButton";
import { getCustomer } from "@/lib/api/customers";
import { getJob, updateJob } from "@/lib/api/jobs";
import { hasPermission } from "@/lib/auth/permissions";

type JobDetailsCardProps = {
  customerId: string;
  jobId: string;
};

export function JobDetailsCard({ customerId, jobId }: JobDetailsCardProps) {
  const queryClient = useQueryClient();
  const customerQuery = useQuery({ queryKey: ["customer", customerId], queryFn: () => getCustomer(customerId) });
  const canViewJobs = hasPermission(customerQuery.data?.permissions, "ViewPlaybooks");
  const jobQuery = useQuery({ queryKey: ["job", customerId, jobId], queryFn: () => getJob(customerId, jobId), enabled: canViewJobs });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateJob>[2]) => updateJob(customerId, jobId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["job", customerId, jobId] });
      await queryClient.invalidateQueries({ queryKey: ["jobs", customerId] });
    },
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">Diese Action konnte nicht geladen werden.</Alert>;
  }

  if (!canViewJobs) {
    return <Alert severity="warning">Du hast keine Berechtigung, Actions für diesen Kunden anzusehen.</Alert>;
  }

  if (jobQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (jobQuery.isError) {
    return <Alert severity="error">Diese Action konnte nicht geladen werden.</Alert>;
  }

  const canManageJobs = hasPermission(customerQuery.data.permissions, "ManagePlaybooks");
  const canRunJobs = hasPermission(customerQuery.data.permissions, "RunJobs");

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack>
            <Typography component="h1" variant="h4">{jobQuery.data.name}</Typography>
            <Typography color="text.secondary">{jobQuery.data.slug}</Typography>
          </Stack>
          {canRunJobs ? <RunJobButton customerId={customerId} jobId={jobId} /> : null}
        </Stack>
        {canManageJobs ? (
          <JobForm
            customerId={customerId}
            job={jobQuery.data}
            onSubmit={async (input) => { await updateMutation.mutateAsync(input); }}
            submitLabel="Save job"
          />
        ) : (
          <Button href={`/customers/${customerId}/runs`} sx={{ alignSelf: "flex-start" }} variant="outlined">
            Runs
          </Button>
        )}
      </Stack>
    </Paper>
  );
}
