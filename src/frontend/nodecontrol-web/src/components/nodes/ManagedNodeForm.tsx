"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, MenuItem, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { ManagedNode, ManagedNodeInput } from "@/lib/api/managedNodes";
import type { Secret } from "@/lib/api/secrets";

const inventoryName = /^[a-zA-Z][a-zA-Z0-9_-]{1,99}$/;

const managedNodeSchema = z.object({
  name: z.string().trim().regex(inventoryName, "Use an inventory-safe name"),
  hostname: z.string().trim().min(1).max(253).refine((value) => !/\s/.test(value), {
    message: "Hostname must not contain whitespace",
  }),
  sshPort: z.number().int().min(1).max(65535),
  sshUsername: z.string().trim().max(100).refine((value) => !/\s/.test(value), {
    message: "SSH username must not contain whitespace",
  }).optional(),
  sshPrivateKeySecretId: z.string().trim().optional(),
  operatingSystem: z.string().trim().max(100).optional(),
  environment: z.string().trim().max(100).optional(),
  description: z.string().trim().max(1000).optional(),
});

type ManagedNodeFormValues = z.infer<typeof managedNodeSchema>;

type ManagedNodeFormProps = {
  managedNode?: ManagedNode;
  sshPrivateKeySecrets?: Secret[];
  submitLabel: string;
  onSubmit: (input: ManagedNodeInput) => Promise<void>;
};

export function ManagedNodeForm({ managedNode, sshPrivateKeySecrets = [], submitLabel, onSubmit }: ManagedNodeFormProps) {
  const configuredSecretIsNotListed = Boolean(managedNode?.sshPrivateKeySecretId)
    && !sshPrivateKeySecrets.some((secret) => secret.id === managedNode?.sshPrivateKeySecretId);
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
      sshUsername: managedNode?.sshUsername ?? "",
      sshPrivateKeySecretId: managedNode?.sshPrivateKeySecretId ?? "",
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
          sshUsername: values.sshUsername || null,
          sshPrivateKeySecretId: values.sshPrivateKeySecretId || null,
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
      <TextField
        error={Boolean(errors.sshUsername)}
        helperText={errors.sshUsername?.message ?? "Optional Ansible SSH user for this target host."}
        label="SSH username"
        {...register("sshUsername")}
      />
      {sshPrivateKeySecrets.length === 0 ? (
        <Alert severity="info">Create an active SSH private key Secret before assigning a host key.</Alert>
      ) : null}
      <TextField
        error={Boolean(errors.sshPrivateKeySecretId)}
        helperText={errors.sshPrivateKeySecretId?.message ?? "Only the Secret reference is stored here; the key value is never shown."}
        label="SSH private key Secret"
        select
        {...register("sshPrivateKeySecretId")}
      >
        <MenuItem value="">None</MenuItem>
        {configuredSecretIsNotListed ? (
          <MenuItem value={managedNode!.sshPrivateKeySecretId!}>Configured SSH key Secret</MenuItem>
        ) : null}
        {sshPrivateKeySecrets.map((secret) => (
          <MenuItem key={secret.id} value={secret.id}>
            {secret.name} · secret://{secret.slug}
          </MenuItem>
        ))}
      </TextField>
      <TextField label="Operating system" {...register("operatingSystem")} />
      <TextField label="Environment" {...register("environment")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
