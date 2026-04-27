"use client";

import { Chip } from "@mui/material";
import type { JobRunStatus } from "@/lib/api/jobRuns";

type JobRunStatusChipProps = {
  status: JobRunStatus;
};

export function JobRunStatusChip({ status }: JobRunStatusChipProps) {
  const color = status === "Running"
    ? "info"
    : status === "Cancelling"
      ? "warning"
    : status === "Succeeded"
      ? "success"
      : status === "Failed" || status === "TimedOut"
        ? "error"
        : status === "Cancelled"
          ? "warning"
          : "default";

  return <Chip color={color} label={status} size="small" />;
}
