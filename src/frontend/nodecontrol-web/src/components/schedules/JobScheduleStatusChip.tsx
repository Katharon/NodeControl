"use client";

import { Chip } from "@mui/material";
import type { JobScheduleStatus } from "@/lib/api/schedules";

type JobScheduleStatusChipProps = {
  status: JobScheduleStatus;
};

export function JobScheduleStatusChip({ status }: JobScheduleStatusChipProps) {
  const color = status === "Active" ? "success" : status === "Paused" ? "warning" : "default";
  return <Chip color={color} label={status} size="small" variant="outlined" />;
}
