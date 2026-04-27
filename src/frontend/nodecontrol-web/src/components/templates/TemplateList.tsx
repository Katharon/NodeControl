"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { TemplateForm } from "@/components/templates/TemplateForm";
import { TemplateStatusChip } from "@/components/templates/TemplateStatusChip";
import { TemplateTypeChip } from "@/components/templates/TemplateTypeChip";
import { TemplateValidationPanel } from "@/components/templates/TemplateValidationPanel";
import {
  archiveTemplate,
  createTemplate,
  getTemplates,
  type TemplateValidationResult,
  validateTemplate,
} from "@/lib/api/templates";

type TemplateListProps = {
  customerId: string;
  canManageTemplates: boolean;
};

export function TemplateList({ customerId, canManageTemplates }: TemplateListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [validationResult, setValidationResult] = useState<TemplateValidationResult | null>(null);
  const templatesQuery = useQuery({ queryKey: ["templates", customerId], queryFn: () => getTemplates(customerId) });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createTemplate>[1]) => createTemplate(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["templates", customerId] });
      setValidationResult(null);
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (templateId: string) => archiveTemplate(customerId, templateId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["templates", customerId] }),
  });

  if (templatesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (templatesQuery.isError) {
    return <Alert severity="error">Templates could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">Templates</Typography>
        {canManageTemplates ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">New Template</Button>
        ) : null}
      </Stack>
      {templatesQuery.data.length === 0 ? (
        <Alert severity="info">No templates defined yet.</Alert>
      ) : (
        <Paper sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Slug</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Language</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Updated</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {templatesQuery.data.map((template) => (
                <TableRow key={template.id}>
                  <TableCell sx={{ fontWeight: 700 }}>{template.name}</TableCell>
                  <TableCell>{template.slug}</TableCell>
                  <TableCell><TemplateTypeChip templateType={template.templateType} /></TableCell>
                  <TableCell>{template.language || "n/a"}</TableCell>
                  <TableCell><TemplateStatusChip status={template.status} /></TableCell>
                  <TableCell>{new Date(template.updatedAt ?? template.createdAt).toLocaleString()}</TableCell>
                  <TableCell align="right">
                    <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1 }}>
                      <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/templates/${template.id}`} size="small" variant="outlined">
                        Open
                      </Button>
                      {canManageTemplates ? (
                        <Button
                          color="warning"
                          disabled={archiveMutation.isPending}
                          onClick={() => archiveMutation.mutate(template.id)}
                          size="small"
                          startIcon={<ArchiveIcon />}
                          variant="outlined"
                        >
                          Archive
                        </Button>
                      ) : null}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
      <Dialog fullWidth maxWidth="lg" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>New Template</DialogTitle>
        <DialogContent>
          <Stack sx={{ gap: 2, pt: 1 }}>
            <TemplateValidationPanel result={validationResult} />
            <TemplateForm
              onSubmit={async (input) => { await createMutation.mutateAsync(input); }}
              onValidate={(input) => validateTemplate(customerId, input)}
              onValidationResult={setValidationResult}
              submitLabel="Create Template"
            />
          </Stack>
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
