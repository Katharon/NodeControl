"use client";

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Box, Button, CircularProgress, Divider, IconButton, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { Controller, useFieldArray, useForm } from "react-hook-form";
import { z } from "zod";
import { getControlNodes } from "@/lib/api/controlNodes";
import { getInventoryGroups } from "@/lib/api/inventoryGroups";
import type { Job, JobInput } from "@/lib/api/jobs";
import { getPlaybooks } from "@/lib/api/playbooks";
import { getTemplates } from "@/lib/api/templates";
import { getVariableSets } from "@/lib/api/variableSets";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const templateArtifactSchema = z.object({
  templateId: z.string(),
  path: z.string().trim().max(500).optional(),
});

const jobSchema = z
  .object({
    name: z.string().trim().min(1).max(200),
    slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
    description: z.string().trim().max(1000).optional(),
    controlNodeId: z.string().uuid(),
    inventoryGroupId: z.string().uuid(),
    playbookId: z.string().uuid(),
    variableSetId: z.string(),
    templateArtifacts: z.array(templateArtifactSchema).max(20, "At most 20 template artifacts are supported"),
    defaultTimeoutSeconds: z.number().int().min(30).max(86400),
  })
  .superRefine((values, context) => {
    const seenPaths = new Set<string>();
    for (const [index, artifact] of values.templateArtifacts.entries()) {
      if (!artifact.templateId) {
        context.addIssue({ code: "custom", message: "Template is required", path: ["templateArtifacts", index, "templateId"] });
      }

      const normalizedPath = normalizeArtifactPath(artifact.path);
      if (!normalizedPath.ok) {
        context.addIssue({ code: "custom", message: normalizedPath.message, path: ["templateArtifacts", index, "path"] });
        continue;
      }

      if (seenPaths.has(normalizedPath.path)) {
        context.addIssue({ code: "custom", message: "Template artifact paths must be unique", path: ["templateArtifacts", index, "path"] });
      }

      seenPaths.add(normalizedPath.path);
    }
  });

type JobFormValues = z.infer<typeof jobSchema>;

type JobFormProps = {
  customerId: string;
  job?: Job;
  submitLabel: string;
  onSubmit: (input: JobInput) => Promise<void>;
};

export function JobForm({ customerId, job, submitLabel, onSubmit }: JobFormProps) {
  const controlNodesQuery = useQuery({ queryKey: ["control-nodes", customerId], queryFn: () => getControlNodes(customerId) });
  const inventoryGroupsQuery = useQuery({ queryKey: ["inventory-groups", customerId], queryFn: () => getInventoryGroups(customerId) });
  const playbooksQuery = useQuery({ queryKey: ["playbooks", customerId], queryFn: () => getPlaybooks(customerId) });
  const variableSetsQuery = useQuery({ queryKey: ["variable-sets", customerId], queryFn: () => getVariableSets(customerId) });
  const templatesQuery = useQuery({ queryKey: ["templates", customerId], queryFn: () => getTemplates(customerId) });
  const {
    formState: { errors, isSubmitting },
    control,
    handleSubmit,
    register,
    reset,
  } = useForm<JobFormValues>({
    resolver: zodResolver(jobSchema),
    defaultValues: getJobFormDefaults(job),
  });
  const { append, fields, remove } = useFieldArray({ control, name: "templateArtifacts" });

  useEffect(() => {
    reset(getJobFormDefaults(job));
  }, [job, reset]);

  if (controlNodesQuery.isPending || inventoryGroupsQuery.isPending || playbooksQuery.isPending || variableSetsQuery.isPending || templatesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (controlNodesQuery.isError || inventoryGroupsQuery.isError || playbooksQuery.isError || variableSetsQuery.isError || templatesQuery.isError) {
    return <Alert severity="error">Action-Formulardaten konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          controlNodeId: values.controlNodeId,
          inventoryGroupId: values.inventoryGroupId,
          playbookId: values.playbookId,
          variableSetId: values.variableSetId || null,
          defaultTimeoutSeconds: values.defaultTimeoutSeconds,
          templateArtifacts: values.templateArtifacts.map((artifact) => {
            const normalizedPath = normalizeArtifactPath(artifact.path);
            return {
              templateId: artifact.templateId,
              path: normalizedPath.ok ? normalizedPath.path : artifact.path?.trim() ?? "",
            };
          }),
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <Controller
        control={control}
        name="controlNodeId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.controlNodeId)}
            helperText={errors.controlNodeId?.message}
            label="Control Host"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">Select Control Host</MenuItem>
            {job?.controlNodeId && !controlNodesQuery.data.some((controlNode) => controlNode.id === job.controlNodeId) ? (
              <MenuItem value={job.controlNodeId}>Configured Control Host</MenuItem>
            ) : null}
            {controlNodesQuery.data.map((controlNode) => <MenuItem key={controlNode.id} value={controlNode.id}>{controlNode.name}</MenuItem>)}
          </TextField>
        )}
      />
      <Controller
        control={control}
        name="inventoryGroupId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.inventoryGroupId)}
            helperText={errors.inventoryGroupId?.message}
            label="Inventar"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">Select inventory</MenuItem>
            {job?.inventoryGroupId && !inventoryGroupsQuery.data.some((group) => group.id === job.inventoryGroupId) ? (
              <MenuItem value={job.inventoryGroupId}>Configured inventory</MenuItem>
            ) : null}
            {inventoryGroupsQuery.data.map((group) => <MenuItem key={group.id} value={group.id}>{group.name}</MenuItem>)}
          </TextField>
        )}
      />
      <Controller
        control={control}
        name="playbookId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.playbookId)}
            helperText={errors.playbookId?.message}
            label="Playbook"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">Select playbook</MenuItem>
            {job?.playbookId && !playbooksQuery.data.some((playbook) => playbook.id === job.playbookId) ? (
              <MenuItem value={job.playbookId}>Configured playbook</MenuItem>
            ) : null}
            {playbooksQuery.data.map((playbook) => <MenuItem key={playbook.id} value={playbook.id}>{playbook.name}</MenuItem>)}
          </TextField>
        )}
      />
      <Controller
        control={control}
        name="variableSetId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.variableSetId)}
            helperText={errors.variableSetId?.message}
            label="Variablen"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">None</MenuItem>
            {job?.variableSetId && !variableSetsQuery.data.some((variableSet) => variableSet.id === job.variableSetId) ? (
              <MenuItem value={job.variableSetId}>Configured variable set</MenuItem>
            ) : null}
            {variableSetsQuery.data.map((variableSet) => <MenuItem key={variableSet.id} value={variableSet.id}>{variableSet.name}</MenuItem>)}
          </TextField>
        )}
      />
      <Box sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
        <Stack divider={<Divider />}>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 1, p: 2 }}>
            <Stack>
              <Typography sx={{ fontWeight: 700 }}>Template artifacts</Typography>
              <Typography color="text.secondary" variant="body2">Materialized by the Worker under the playbook workspace.</Typography>
            </Stack>
            <Button
              onClick={() => append({ templateId: "", path: "" })}
              startIcon={<AddIcon />}
              type="button"
              variant="outlined"
            >
              Add mapping
            </Button>
          </Stack>
          {fields.length === 0 ? (
            <Box sx={{ p: 2 }}>
              <Typography color="text.secondary">No template artifacts mapped.</Typography>
            </Box>
          ) : (
            fields.map((field, index) => (
              <Stack direction={{ xs: "column", md: "row" }} key={field.id} sx={{ alignItems: { md: "flex-start" }, gap: 1, p: 2 }}>
                <Controller
                  control={control}
                  name={`templateArtifacts.${index}.templateId`}
                  render={({ field: templateField }) => (
                    <TextField
                      error={Boolean(errors.templateArtifacts?.[index]?.templateId)}
                      helperText={errors.templateArtifacts?.[index]?.templateId?.message}
                      label="Template"
                      onBlur={templateField.onBlur}
                      onChange={templateField.onChange}
                      select
                      sx={{ flex: 1 }}
                      value={templateField.value ?? ""}
                    >
                      <MenuItem value="">Select template</MenuItem>
                      {templateField.value && !templatesQuery.data.some((template) => template.id === templateField.value) ? (
                        <MenuItem value={templateField.value}>Configured template</MenuItem>
                      ) : null}
                      {templatesQuery.data.map((template) => <MenuItem key={template.id} value={template.id}>{template.name}</MenuItem>)}
                    </TextField>
                  )}
                />
                <TextField
                  error={Boolean(errors.templateArtifacts?.[index]?.path)}
                  helperText={errors.templateArtifacts?.[index]?.path?.message ?? "Relative path, for example templates/app.conf"}
                  label="Workspace path"
                  sx={{ flex: 1 }}
                  {...register(`templateArtifacts.${index}.path`)}
                />
                <IconButton aria-label="Remove template artifact" color="warning" onClick={() => remove(index)} sx={{ mt: { md: 1 } }}>
                  <DeleteIcon />
                </IconButton>
              </Stack>
            ))
          )}
        </Stack>
      </Box>
      <TextField
        error={Boolean(errors.defaultTimeoutSeconds)}
        helperText={errors.defaultTimeoutSeconds?.message}
        label="Default timeout seconds"
        type="number"
        {...register("defaultTimeoutSeconds", { valueAsNumber: true })}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}

function getJobFormDefaults(job?: Job): JobFormValues {
  return {
    name: job?.name ?? "",
    slug: job?.slug ?? "",
    description: job?.description ?? "",
    controlNodeId: job?.controlNodeId ?? "",
    inventoryGroupId: job?.inventoryGroupId ?? "",
    playbookId: job?.playbookId ?? "",
    variableSetId: job?.variableSetId ?? "",
    templateArtifacts: job?.templateArtifacts ?? [],
    defaultTimeoutSeconds: job?.defaultTimeoutSeconds ?? 1800,
  };
}

function normalizeArtifactPath(value: string | undefined): { ok: true; path: string } | { ok: false; message: string } {
  const normalized = value?.trim().replaceAll("\\", "/") ?? "";
  if (!normalized) {
    return { ok: false, message: "Template artifact path is required" };
  }

  if (
    normalized.length > 500
    || normalized.startsWith("/")
    || normalized.endsWith("/")
    || /^[A-Za-z]:/.test(normalized)
    || normalized.split("/").some((part) => !part.trim() || part === "." || part === "..")
  ) {
    return { ok: false, message: "Template artifact path is invalid" };
  }

  return { ok: true, path: normalized };
}
