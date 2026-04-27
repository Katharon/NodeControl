"use client";

import ReplayIcon from "@mui/icons-material/Replay";
import { Button } from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
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
    <Button disabled={mutation.isPending} onClick={() => mutation.mutate()} startIcon={<ReplayIcon />} variant="contained">
      Retry run
    </Button>
  );
}
