"use client";

import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import { Button } from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { runJob } from "@/lib/api/jobs";

type RunJobButtonProps = {
  customerId: string;
  jobId: string;
  disabled?: boolean;
};

export function RunJobButton({ customerId, jobId, disabled = false }: RunJobButtonProps) {
  const queryClient = useQueryClient();
  const router = useRouter();
  const mutation = useMutation({
    mutationFn: () => runJob(customerId, jobId),
    onSuccess: async (jobRun) => {
      await queryClient.invalidateQueries({ queryKey: ["job-runs", customerId] });
      router.push(`/customers/${customerId}/job-runs/${jobRun.id}`);
    },
  });

  return (
    <Button
      disabled={disabled || mutation.isPending}
      onClick={() => mutation.mutate()}
      startIcon={<PlayArrowIcon />}
      variant="contained"
    >
      Run job
    </Button>
  );
}
