import { Chip } from "@mui/material";
import type { SecretKind } from "@/lib/api/secrets";

const labels: Record<SecretKind, string> = {
  Generic: "Generic",
  Password: "Password",
  ApiToken: "API Token",
  SshPrivateKey: "SSH Private Key",
  Certificate: "Certificate",
  ConnectionString: "Connection String",
};

type SecretKindChipProps = {
  kind: SecretKind;
};

export function SecretKindChip({ kind }: SecretKindChipProps) {
  return <Chip label={labels[kind]} size="small" variant="outlined" />;
}
