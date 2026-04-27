import { Chip } from "@mui/material";
import type { TemplateStatus } from "@/lib/api/templates";

type TemplateStatusChipProps = {
  status: TemplateStatus;
};

export function TemplateStatusChip({ status }: TemplateStatusChipProps) {
  return (
    <Chip
      color={status === "Active" ? "success" : "default"}
      label={status}
      size="small"
      variant={status === "Active" ? "filled" : "outlined"}
    />
  );
}
