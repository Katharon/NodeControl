import { Chip } from "@mui/material";
import type { HostConnectionCheckStatus } from "@/lib/api/hostConnectionChecks";

type HostConnectionStatusChipProps = {
  status?: HostConnectionCheckStatus | null;
};

export function HostConnectionStatusChip({ status }: HostConnectionStatusChipProps) {
  if (!status) {
    return <Chip label="Nicht geprüft" size="small" variant="outlined" />;
  }

  const chip = {
    Queued: { color: "info" as const, label: "Queued" },
    Running: { color: "warning" as const, label: "Running" },
    Succeeded: { color: "success" as const, label: "Succeeded" },
    Failed: { color: "error" as const, label: "Failed" },
    TimedOut: { color: "error" as const, label: "Timed out" },
  }[status];

  return <Chip color={chip.color} label={chip.label} size="small" />;
}
