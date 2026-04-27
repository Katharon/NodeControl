"use client";

import ArchiveIcon from "@mui/icons-material/Archive";
import CheckIcon from "@mui/icons-material/Check";
import { Alert, Box, Button, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { TemplateForm } from "@/components/templates/TemplateForm";
import { TemplateStatusChip } from "@/components/templates/TemplateStatusChip";
import { TemplateTypeChip } from "@/components/templates/TemplateTypeChip";
import { TemplateValidationPanel } from "@/components/templates/TemplateValidationPanel";
import {
  archiveTemplate,
  getTemplate,
  type TemplateValidationResult,
  updateTemplate,
  validateStoredTemplate,
  validateTemplate,
} from "@/lib/api/templates";

type TemplateDetailsCardProps = {
  customerId: string;
  templateId: string;
  canManageTemplates: boolean;
};

export function TemplateDetailsCard({ customerId, templateId, canManageTemplates }: TemplateDetailsCardProps) {
  const queryClient = useQueryClient();
  const [validationResult, setValidationResult] = useState<TemplateValidationResult | null>(null);
  const templateQuery = useQuery({ queryKey: ["template", customerId, templateId], queryFn: () => getTemplate(customerId, templateId) });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateTemplate>[2]) => updateTemplate(customerId, templateId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["template", customerId, templateId] });
      await queryClient.invalidateQueries({ queryKey: ["templates", customerId] });
    },
  });
  const archiveMutation = useMutation({
    mutationFn: () => archiveTemplate(customerId, templateId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["template", customerId, templateId] });
      await queryClient.invalidateQueries({ queryKey: ["templates", customerId] });
    },
  });
  const validateStoredMutation = useMutation({
    mutationFn: () => validateStoredTemplate(customerId, templateId),
    onSuccess: setValidationResult,
  });

  if (templateQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (templateQuery.isError) {
    return <Alert severity="error">This template could not be loaded.</Alert>;
  }

  const template = templateQuery.data;

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack sx={{ gap: 1 }}>
            <Typography component="h1" variant="h4">{template.name}</Typography>
            <Typography color="text.secondary">{template.slug}</Typography>
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
              <TemplateTypeChip templateType={template.templateType} />
              <TemplateStatusChip status={template.status} />
              {template.language ? <Typography color="text.secondary" variant="body2">{template.language}</Typography> : null}
            </Stack>
          </Stack>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, gap: 1 }}>
            <Button disabled={validateStoredMutation.isPending} onClick={() => validateStoredMutation.mutate()} startIcon={<CheckIcon />} variant="outlined">
              Validate Stored
            </Button>
            {canManageTemplates && template.status === "Active" ? (
              <Button color="warning" disabled={archiveMutation.isPending} onClick={() => archiveMutation.mutate()} startIcon={<ArchiveIcon />} variant="outlined">
                Archive
              </Button>
            ) : null}
          </Stack>
        </Stack>
        {template.description ? <Typography>{template.description}</Typography> : null}
        <TemplateValidationPanel result={validationResult} />
        <Divider />
        {canManageTemplates && template.status === "Active" ? (
          <TemplateForm
            template={template}
            onSubmit={async (input) => { await updateMutation.mutateAsync(input); }}
            onValidate={(input) => validateTemplate(customerId, input)}
            onValidationResult={setValidationResult}
            submitLabel="Save Template"
          />
        ) : (
          <Box sx={{ bgcolor: "background.default", border: 1, borderColor: "divider", borderRadius: 1, p: 2, whiteSpace: "pre-wrap" }}>
            <Typography component="pre" sx={{ fontFamily: "monospace", fontSize: 14, m: 0 }}>
              {template.content}
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  );
}
