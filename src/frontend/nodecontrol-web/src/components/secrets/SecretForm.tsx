"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Button, MenuItem, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { CreateSecretInput, Secret, SecretKind, UpdateSecretInput } from "@/lib/api/secrets";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const secretSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  kind: z.enum(["Generic", "Password", "ApiToken", "SshPrivateKey", "Certificate", "ConnectionString"]),
  value: z.string().max(100000).optional(),
});

type SecretFormValues = z.infer<typeof secretSchema>;

type SecretFormProps = {
  secret?: Secret;
  requireValue: boolean;
  submitLabel: string;
  onSubmit: (input: CreateSecretInput | UpdateSecretInput) => Promise<void>;
};

export function SecretForm({ secret, requireValue, submitLabel, onSubmit }: SecretFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<SecretFormValues>({
    resolver: zodResolver(secretSchema),
    defaultValues: {
      name: secret?.name ?? "",
      slug: secret?.slug ?? "",
      description: secret?.description ?? "",
      kind: secret?.kind ?? "Generic",
      value: "",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) => {
        if (requireValue && !values.value?.trim()) {
          setError("value", { message: "Secret value is required." });
          return;
        }

        const metadata: UpdateSecretInput = {
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          kind: values.kind as SecretKind,
        };

        if (requireValue) {
          await onSubmit({ ...metadata, value: values.value ?? "" });
          return;
        }

        await onSubmit(metadata);
      })}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField label="Kind" select {...register("kind")}>
        <MenuItem value="Generic">Generic</MenuItem>
        <MenuItem value="Password">Password</MenuItem>
        <MenuItem value="ApiToken">API Token</MenuItem>
        <MenuItem value="SshPrivateKey">SSH Private Key</MenuItem>
        <MenuItem value="Certificate">Certificate</MenuItem>
        <MenuItem value="ConnectionString">Connection String</MenuItem>
      </TextField>
      {requireValue ? (
        <Stack sx={{ gap: 1 }}>
          <Alert severity="warning">Secret values cannot be viewed again after saving.</Alert>
          <TextField
            error={Boolean(errors.value)}
            helperText={errors.value?.message}
            label="Secret value"
            minRows={4}
            multiline
            type="password"
            {...register("value")}
          />
        </Stack>
      ) : null}
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
