"use client";

import CancelIcon from "@mui/icons-material/Cancel";
import { Button } from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { cancelJobRun, type JobRunStatus } from "@/lib/api/jobRuns";

type CancelJobRunButtonProps = {
  customerId: string;
  jobRunId: string;
  status: JobRunStatus;
};

export function CancelJobRunButton({ customerId, jobRunId, status }: CancelJobRunButtonProps) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => cancelJobRun(customerId, jobRunId, null),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["job-run", customerId, jobRunId] });
      await queryClient.invalidateQueries({ queryKey: ["job-runs", customerId] });
      await queryClient.invalidateQueries({ queryKey: ["job-run-logs", customerId, jobRunId] });
    },
  });

  return (
    <Button
      color="warning"
      disabled={mutation.isPending || status === "Cancelling"}
      onClick={() => mutation.mutate()}
      startIcon={<CancelIcon />}
      variant="outlined"
    >
      {status === "Cancelling" ? "Cancelling" : "Cancel"}
    </Button>
  );
}
