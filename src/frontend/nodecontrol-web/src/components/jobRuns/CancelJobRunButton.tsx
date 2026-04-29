"use client";

import CancelIcon from "@mui/icons-material/Cancel";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ConfirmActionButton } from "@/components/guardrails/ConfirmActionButton";
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
    <ConfirmActionButton
      actionLabel="Cancel run"
      color="warning"
      disabled={status === "Cancelling"}
      disabledReason="Cancellation has already been requested for this run."
      message={status === "Queued"
        ? "This queued run will be cancelled before the Worker starts it."
        : "The Worker will be asked to stop this running job. Partial changes on target hosts may already have happened."}
      onConfirm={() => mutation.mutateAsync()}
      pending={mutation.isPending}
      startIcon={<CancelIcon />}
      title="Cancel this run?"
      variant="outlined"
    >
      {status === "Cancelling" ? "Cancelling" : "Cancel run"}
    </ConfirmActionButton>
  );
}
