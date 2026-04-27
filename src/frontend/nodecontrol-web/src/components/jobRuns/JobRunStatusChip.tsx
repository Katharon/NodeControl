"use client";

import { Chip } from "@mui/material";
import type { JobRunStatus } from "@/lib/api/jobRuns";

type JobRunStatusChipProps = {
  status: JobRunStatus;
};

export function JobRunStatusChip({ status }: JobRunStatusChipProps) {
  const color = status === "Queued"
    ? "default"
    : status === "Running"
      ? "info"
      : status === "Succeeded"
        ? "success"
        : status === "Failed" || status === "TimedOut"
          ? "error"
          : "warning";

  return <Chip color={color} label={status} size="small" />;
}
