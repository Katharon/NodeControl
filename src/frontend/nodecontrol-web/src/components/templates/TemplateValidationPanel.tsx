"use client";

import { Alert, Chip, Stack, Typography } from "@mui/material";
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
      {result.secretReferences.length > 0 ? (
        <Stack sx={{ gap: 1 }}>
          <Typography sx={{ fontWeight: 700 }} variant="body2">Secret references</Typography>
          <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
            {result.secretReferences.map((reference) => (
              <Chip
                color={reference.found && reference.status === "Active" ? "success" : "error"}
                key={reference.slug}
                label={`secret://${reference.slug} · ${reference.found ? reference.status : "Missing"}`}
                size="small"
                variant="outlined"
              />
            ))}
          </Stack>
        </Stack>
      ) : null}
    </Stack>
  );
}
