"use client";

import NetworkCheckIcon from "@mui/icons-material/NetworkCheck";
import { Button } from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  queueControlNodeConnectionCheck,
  queueManagedNodeConnectionCheck,
  type HostConnectionTargetType,
} from "@/lib/api/hostConnectionChecks";

type ConnectionCheckButtonProps = {
  customerId: string;
  targetType: HostConnectionTargetType;
  targetId: string;
  disabled?: boolean;
  onQueued?: () => void;
};

export function ConnectionCheckButton({
  customerId,
  targetType,
  targetId,
  disabled = false,
  onQueued,
}: ConnectionCheckButtonProps) {
  const queryClient = useQueryClient();
  const queueMutation = useMutation({
    mutationFn: () =>
      targetType === "ControlNode"
        ? queueControlNodeConnectionCheck(customerId, targetId)
        : queueManagedNodeConnectionCheck(customerId, targetId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["host-health", customerId] }),
        queryClient.invalidateQueries({ queryKey: ["host-connection-checks", customerId] }),
      ]);
      onQueued?.();
    },
  });

  return (
    <Button
      disabled={disabled || queueMutation.isPending}
      color={queueMutation.isError ? "error" : "primary"}
      onClick={() => queueMutation.mutate()}
      startIcon={<NetworkCheckIcon />}
      variant="outlined"
    >
      {queueMutation.isPending
        ? "Wird gestartet..."
        : queueMutation.isError
          ? "Start fehlgeschlagen"
          : "Check starten"}
    </Button>
  );
}
