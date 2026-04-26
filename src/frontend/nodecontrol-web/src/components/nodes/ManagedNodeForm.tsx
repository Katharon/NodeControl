"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { ManagedNode, ManagedNodeInput } from "@/lib/api/managedNodes";

const inventoryName = /^[a-zA-Z][a-zA-Z0-9_-]{1,99}$/;

const managedNodeSchema = z.object({
  name: z.string().trim().regex(inventoryName, "Use an inventory-safe name"),
  hostname: z.string().trim().min(1).max(253).refine((value) => !/\s/.test(value), {
    message: "Hostname must not contain whitespace",
  }),
  sshPort: z.number().int().min(1).max(65535),
  operatingSystem: z.string().trim().max(100).optional(),
  environment: z.string().trim().max(100).optional(),
  description: z.string().trim().max(1000).optional(),
});

type ManagedNodeFormValues = z.infer<typeof managedNodeSchema>;

type ManagedNodeFormProps = {
  managedNode?: ManagedNode;
  submitLabel: string;
  onSubmit: (input: ManagedNodeInput) => Promise<void>;
};

export function ManagedNodeForm({ managedNode, submitLabel, onSubmit }: ManagedNodeFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<ManagedNodeFormValues>({
    resolver: zodResolver(managedNodeSchema),
    defaultValues: {
      name: managedNode?.name ?? "",
      hostname: managedNode?.hostname ?? "",
      sshPort: managedNode?.sshPort ?? 22,
      operatingSystem: managedNode?.operatingSystem ?? "",
      environment: managedNode?.environment ?? "",
      description: managedNode?.description ?? "",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          hostname: values.hostname,
          sshPort: values.sshPort,
          operatingSystem: values.operatingSystem || null,
          environment: values.environment || null,
          description: values.description || null,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField
        error={Boolean(errors.hostname)}
        helperText={errors.hostname?.message}
        label="Hostname"
        {...register("hostname")}
      />
      <TextField
        error={Boolean(errors.sshPort)}
        helperText={errors.sshPort?.message}
        label="SSH port"
        type="number"
        {...register("sshPort", { valueAsNumber: true })}
      />
      <TextField label="Operating system" {...register("operatingSystem")} />
      <TextField label="Environment" {...register("environment")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
