"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, MenuItem, Stack, TextField } from "@mui/material";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import type { ControlNode, ControlNodeInput } from "@/lib/api/controlNodes";
import type { Secret } from "@/lib/api/secrets";

const controlNodeSchema = z.object({
  name: z.string().trim().min(1).max(200),
  hostname: z.string().trim().min(1).max(253).refine((value) => !/\s/.test(value), {
    message: "Hostname must not contain whitespace",
  }),
  sshPort: z.number().int().min(1).max(65535),
  remoteDispatchMode: z.enum(["local", "ssh"]),
  sshUsername: z.string().trim().max(100).optional(),
  sshPrivateKeySecretId: z.string().trim().optional(),
  remoteWorkspaceRoot: z.string().trim().max(500).optional(),
  description: z.string().trim().max(1000).optional(),
}).superRefine((values, context) => {
  if (values.remoteDispatchMode !== "ssh") {
    return;
  }

  if (!values.sshUsername) {
    context.addIssue({ code: "custom", message: "SSH username is required.", path: ["sshUsername"] });
  }

  if (!values.sshPrivateKeySecretId) {
    context.addIssue({ code: "custom", message: "Select an SSH private key Secret.", path: ["sshPrivateKeySecretId"] });
  }

  if (!values.remoteWorkspaceRoot || !values.remoteWorkspaceRoot.startsWith("/") || /\s/.test(values.remoteWorkspaceRoot)) {
    context.addIssue({
      code: "custom",
      message: "Use an absolute Unix path without whitespace.",
      path: ["remoteWorkspaceRoot"],
    });
  }
});

type ControlNodeFormValues = z.infer<typeof controlNodeSchema>;

type ControlNodeFormProps = {
  controlNode?: ControlNode;
  sshPrivateKeySecrets?: Secret[];
  canValidateSecretReferences?: boolean;
  submitLabel: string;
  onSubmit: (input: ControlNodeInput) => Promise<void>;
};

export function ControlNodeForm({
  controlNode,
  sshPrivateKeySecrets = [],
  canValidateSecretReferences = true,
  submitLabel,
  onSubmit,
}: ControlNodeFormProps) {
  const configuredSecretIsNotListed = Boolean(controlNode?.sshPrivateKeySecretId)
    && !sshPrivateKeySecrets.some((secret) => secret.id === controlNode?.sshPrivateKeySecretId);
  const configuredSecretLooksBroken = canValidateSecretReferences && configuredSecretIsNotListed;
  const {
    formState: { errors, isSubmitting },
    control,
    handleSubmit,
    register,
    reset,
    setError,
  } = useForm<ControlNodeFormValues>({
    resolver: zodResolver(controlNodeSchema),
    defaultValues: getControlNodeFormDefaults(controlNode),
  });

  useEffect(() => {
    reset(getControlNodeFormDefaults(controlNode));
  }, [controlNode, reset]);

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) => {
        if (
          values.remoteDispatchMode === "ssh"
          && canValidateSecretReferences
          && values.sshPrivateKeySecretId
          && !sshPrivateKeySecrets.some((secret) => secret.id === values.sshPrivateKeySecretId)
        ) {
          setError("sshPrivateKeySecretId", {
            message: "Select an active SSH private key Secret or switch dispatch back to local/dev.",
          });
          return;
        }

        await onSubmit({
          name: values.name,
          hostname: values.hostname,
          sshPort: values.sshPort,
          sshUsername: values.remoteDispatchMode === "ssh" ? values.sshUsername || null : null,
          sshPrivateKeySecretId: values.remoteDispatchMode === "ssh" ? values.sshPrivateKeySecretId || null : null,
          remoteWorkspaceRoot: values.remoteDispatchMode === "ssh" ? values.remoteWorkspaceRoot || null : null,
          description: values.description || null,
        });
      })}
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
      <Controller
        control={control}
        name="remoteDispatchMode"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.remoteDispatchMode)}
            helperText={errors.remoteDispatchMode?.message ?? "Local hosts keep using the Worker local/dev fallback. Non-local hosts can use SSH dispatch."}
            label="Dispatch"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? "local"}
          >
            <MenuItem value="local">Local/dev fallback or not configured</MenuItem>
            <MenuItem disabled={sshPrivateKeySecrets.length === 0 && !controlNode?.sshPrivateKeySecretId} value="ssh">SSH private key</MenuItem>
          </TextField>
        )}
      />
      {sshPrivateKeySecrets.length === 0 && !controlNode?.sshPrivateKeySecretId ? (
        <Alert severity="info">Create an active SSH private key Secret before enabling SSH dispatch.</Alert>
      ) : null}
      {configuredSecretLooksBroken ? (
        <Alert severity="warning">
          The configured dispatch Secret is missing, archived, or not an SSH private key. Select an active SSH private key Secret or switch dispatch back to local/dev.
        </Alert>
      ) : null}
      <TextField
        error={Boolean(errors.sshUsername)}
        helperText={errors.sshUsername?.message}
        label="SSH username"
        {...register("sshUsername")}
      />
      <Controller
        control={control}
        name="sshPrivateKeySecretId"
        render={({ field }) => (
          <TextField
            error={Boolean(errors.sshPrivateKeySecretId)}
            helperText={errors.sshPrivateKeySecretId?.message ?? "Only the Secret reference is stored here; the key value is never shown."}
            label="SSH private key Secret"
            onBlur={field.onBlur}
            onChange={field.onChange}
            select
            value={field.value ?? ""}
          >
            <MenuItem value="">None</MenuItem>
            {configuredSecretIsNotListed ? (
              <MenuItem value={controlNode!.sshPrivateKeySecretId!}>Configured SSH key Secret</MenuItem>
            ) : null}
            {sshPrivateKeySecrets.map((secret) => (
              <MenuItem key={secret.id} value={secret.id}>
                {secret.name} · secret://{secret.slug}
              </MenuItem>
            ))}
          </TextField>
        )}
      />
      <TextField
        error={Boolean(errors.remoteWorkspaceRoot)}
        helperText={errors.remoteWorkspaceRoot?.message ?? "Remote base path used for staged run workspaces."}
        label="Remote workspace root"
        {...register("remoteWorkspaceRoot")}
      />
      <TextField
        error={Boolean(errors.description)}
        helperText={errors.description?.message}
        label="Description"
        minRows={2}
        multiline
        {...register("description")}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}

function getControlNodeFormDefaults(controlNode?: ControlNode): ControlNodeFormValues {
  return {
    name: controlNode?.name ?? "",
    hostname: controlNode?.hostname ?? "",
    sshPort: controlNode?.sshPort ?? 22,
    remoteDispatchMode: controlNode?.sshPrivateKeySecretId ? "ssh" : "local",
    sshUsername: controlNode?.sshUsername ?? "",
    sshPrivateKeySecretId: controlNode?.sshPrivateKeySecretId ?? "",
    remoteWorkspaceRoot: controlNode?.remoteWorkspaceRoot ?? "/var/lib/nodecontrol/remote-runs",
    description: controlNode?.description ?? "",
  };
}
