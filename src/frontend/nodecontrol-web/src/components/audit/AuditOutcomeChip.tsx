"use client";

import { Chip } from "@mui/material";
import type { AuditOutcome } from "@/lib/api/audit";

type AuditOutcomeChipProps = {
  outcome: AuditOutcome;
};

export function AuditOutcomeChip({ outcome }: AuditOutcomeChipProps) {
  const color = outcome === "Succeeded" ? "success" : outcome === "Denied" ? "warning" : "error";

  return <Chip color={color} label={outcome} size="small" variant="outlined" />;
}
