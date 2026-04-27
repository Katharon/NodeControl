"use client";

import { Alert, Stack } from "@mui/material";
import type { TemplateValidationResult } from "@/lib/api/templates";

type TemplateValidationPanelProps = {
  result: TemplateValidationResult | null;
};

export function TemplateValidationPanel({ result }: TemplateValidationPanelProps) {
  if (!result) {
    return null;
  }

  return (
    <Stack sx={{ gap: 1 }}>
      {result.isValid ? <Alert severity="success">Template validation passed.</Alert> : null}
      {result.errors.map((error) => (
        <Alert key={error} severity="error">{error}</Alert>
      ))}
      {result.warnings.map((warning) => (
        <Alert key={warning} severity="warning">{warning}</Alert>
      ))}
    </Stack>
  );
}
