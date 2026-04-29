"use client";

import ReplayIcon from "@mui/icons-material/Replay";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { ConfirmActionButton } from "@/components/guardrails/ConfirmActionButton";
import { retryJobRun } from "@/lib/api/jobRuns";

type RetryJobRunButtonProps = {
  customerId: string;
  jobRunId: string;
};

export function RetryJobRunButton({ customerId, jobRunId }: RetryJobRunButtonProps) {
  const queryClient = useQueryClient();
  const router = useRouter();
  const mutation = useMutation({
    mutationFn: () => retryJobRun(customerId, jobRunId),
    onSuccess: async (jobRun) => {
      await queryClient.invalidateQueries({ queryKey: ["job-runs", customerId] });
      router.push(`/customers/${customerId}/runs/${jobRun.id}`);
    },
  });

  return (
    <ConfirmActionButton
      actionLabel="Retry run"
      color="primary"
      message="This creates a new queued run using the same Action definition and execution path."
      onConfirm={() => mutation.mutateAsync()}
      pending={mutation.isPending}
      startIcon={<ReplayIcon />}
      title="Retry this run?"
      variant="contained"
    >
      Retry run
    </ConfirmActionButton>
  );
}
