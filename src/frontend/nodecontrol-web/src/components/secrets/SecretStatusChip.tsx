import { Chip } from "@mui/material";
import type { SecretStatus } from "@/lib/api/secrets";

type SecretStatusChipProps = {
  status: SecretStatus;
};

export function SecretStatusChip({ status }: SecretStatusChipProps) {
  return (
    <Chip
      color={status === "Active" ? "success" : "default"}
      label={status}
      size="small"
      variant={status === "Active" ? "filled" : "outlined"}
    />
  );
}
