"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, CircularProgress, MenuItem, Stack, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { getControlNodes } from "@/lib/api/controlNodes";
import { getInventoryGroups } from "@/lib/api/inventoryGroups";
import type { Job, JobInput } from "@/lib/api/jobs";
import { getPlaybooks } from "@/lib/api/playbooks";
import { getTemplates } from "@/lib/api/templates";
import { getVariableSets } from "@/lib/api/variableSets";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const jobSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  controlNodeId: z.string().uuid(),
  inventoryGroupId: z.string().uuid(),
  playbookId: z.string().uuid(),
  variableSetId: z.string(),
  templateArtifactTemplateId: z.string(),
  templateArtifactPath: z.string().trim().max(500).optional(),
  defaultTimeoutSeconds: z.number().int().min(30).max(86400),
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
    handleSubmit,
    register,
  } = useForm<JobFormValues>({
    resolver: zodResolver(jobSchema),
    defaultValues: {
      name: job?.name ?? "",
      slug: job?.slug ?? "",
      description: job?.description ?? "",
      controlNodeId: job?.controlNodeId ?? "",
      inventoryGroupId: job?.inventoryGroupId ?? "",
      playbookId: job?.playbookId ?? "",
      variableSetId: job?.variableSetId ?? "",
      templateArtifactTemplateId: job?.templateArtifacts[0]?.templateId ?? "",
      templateArtifactPath: job?.templateArtifacts[0]?.path ?? "",
      defaultTimeoutSeconds: job?.defaultTimeoutSeconds ?? 1800,
    },
  });

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
          templateArtifacts: values.templateArtifactTemplateId && values.templateArtifactPath
            ? [{ templateId: values.templateArtifactTemplateId, path: values.templateArtifactPath }]
            : [],
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField error={Boolean(errors.controlNodeId)} helperText={errors.controlNodeId?.message} label="Control Host" select {...register("controlNodeId")}>
        {controlNodesQuery.data.map((controlNode) => <MenuItem key={controlNode.id} value={controlNode.id}>{controlNode.name}</MenuItem>)}
      </TextField>
      <TextField error={Boolean(errors.inventoryGroupId)} helperText={errors.inventoryGroupId?.message} label="Inventar" select {...register("inventoryGroupId")}>
        {inventoryGroupsQuery.data.map((group) => <MenuItem key={group.id} value={group.id}>{group.name}</MenuItem>)}
      </TextField>
      <TextField error={Boolean(errors.playbookId)} helperText={errors.playbookId?.message} label="Playbook" select {...register("playbookId")}>
        {playbooksQuery.data.map((playbook) => <MenuItem key={playbook.id} value={playbook.id}>{playbook.name}</MenuItem>)}
      </TextField>
      <TextField label="Variablen" select {...register("variableSetId")}>
        <MenuItem value="">None</MenuItem>
        {variableSetsQuery.data.map((variableSet) => <MenuItem key={variableSet.id} value={variableSet.id}>{variableSet.name}</MenuItem>)}
      </TextField>
      <TextField label="Template Artifact" select {...register("templateArtifactTemplateId")}>
        <MenuItem value="">None</MenuItem>
        {templatesQuery.data.map((template) => <MenuItem key={template.id} value={template.id}>{template.name}</MenuItem>)}
      </TextField>
      <TextField
        error={Boolean(errors.templateArtifactPath)}
        helperText={errors.templateArtifactPath?.message}
        label="Template Artifact Path"
        {...register("templateArtifactPath")}
      />
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
