"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, FormControlLabel, MenuItem, Stack, Switch, TextField } from "@mui/material";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import type { VariableSet, VariableSetInput } from "@/lib/api/variableSets";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const variableSetSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  format: z.enum(["Yaml", "Json"]),
  content: z.string().min(1).max(200000),
  containsSensitiveValues: z.boolean(),
});

type VariableSetFormValues = z.infer<typeof variableSetSchema>;

type VariableSetFormProps = {
  variableSet?: VariableSet;
  submitLabel: string;
  onSubmit: (input: VariableSetInput) => Promise<void>;
};

export function VariableSetForm({ variableSet, submitLabel, onSubmit }: VariableSetFormProps) {
  const {
    formState: { errors, isSubmitting },
    control,
    handleSubmit,
    register,
  } = useForm<VariableSetFormValues>({
    resolver: zodResolver(variableSetSchema),
    defaultValues: {
      name: variableSet?.name ?? "",
      slug: variableSet?.slug ?? "",
      description: variableSet?.description ?? "",
      format: variableSet?.format ?? "Yaml",
      content: variableSet?.content ?? "package_name: nginx\n",
      containsSensitiveValues: variableSet?.containsSensitiveValues ?? false,
    },
  });

  const containsSensitiveValues = useWatch({ control, name: "containsSensitiveValues" });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          format: values.format,
          content: values.content,
          containsSensitiveValues: values.containsSensitiveValues,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField label="Format" select {...register("format")}>
        <MenuItem value="Yaml">YAML</MenuItem>
        <MenuItem value="Json">JSON</MenuItem>
      </TextField>
      <FormControlLabel
        control={<Switch checked={Boolean(containsSensitiveValues)} {...register("containsSensitiveValues")} />}
        label="Enthält sensible Werte"
      />
      <TextField
        error={Boolean(errors.content)}
        helperText={errors.content?.message ?? "Use secret://my-secret to reference a stored secret."}
        label="Inhalt"
        minRows={14}
        multiline
        {...register("content")}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
