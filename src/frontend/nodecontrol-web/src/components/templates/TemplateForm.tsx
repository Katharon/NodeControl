"use client";

import CheckIcon from "@mui/icons-material/Check";
import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, MenuItem, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { Template, TemplateInput, TemplateValidationResult } from "@/lib/api/templates";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const templateSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  templateType: z.enum(["GenericText", "Jinja2", "AnsibleVars", "ShellScript", "ConfigFile"]),
  language: z.string().trim().max(100).optional(),
  content: z.string().min(1).max(200000),
});

type TemplateFormValues = z.infer<typeof templateSchema>;

type TemplateFormProps = {
  template?: Template;
  submitLabel: string;
  onSubmit: (input: TemplateInput) => Promise<void>;
  onValidate?: (input: Pick<TemplateInput, "templateType" | "content" | "language">) => Promise<TemplateValidationResult>;
  onValidationResult?: (result: TemplateValidationResult) => void;
};

export function TemplateForm({ template, submitLabel, onSubmit, onValidate, onValidationResult }: TemplateFormProps) {
  const {
    formState: { errors, isSubmitting },
    getValues,
    handleSubmit,
    register,
  } = useForm<TemplateFormValues>({
    resolver: zodResolver(templateSchema),
    defaultValues: {
      name: template?.name ?? "",
      slug: template?.slug ?? "",
      description: template?.description ?? "",
      templateType: template?.templateType ?? "Jinja2",
      language: template?.language ?? "",
      content: template?.content ?? "server_name {{ host }};\n",
    },
  });

  async function validateCurrentValues() {
    if (!onValidate) {
      return;
    }

    const values = getValues();
    const result = await onValidate({
      templateType: values.templateType,
      content: values.content,
      language: values.language || null,
    });
    onValidationResult?.(result);
  }

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          templateType: values.templateType,
          content: values.content,
          language: values.language || null,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField label="Type" select {...register("templateType")}>
        <MenuItem value="GenericText">Generic Text</MenuItem>
        <MenuItem value="Jinja2">Jinja2</MenuItem>
        <MenuItem value="AnsibleVars">Ansible Vars</MenuItem>
        <MenuItem value="ShellScript">Shell Script</MenuItem>
        <MenuItem value="ConfigFile">Config File</MenuItem>
      </TextField>
      <TextField error={Boolean(errors.language)} helperText={errors.language?.message} label="Language" {...register("language")} />
      <TextField
        error={Boolean(errors.content)}
        helperText={errors.content?.message ?? "Use secret://my-secret to reference a stored secret."}
        label="Content"
        minRows={18}
        multiline
        slotProps={{ input: { sx: { fontFamily: "monospace", fontSize: 14 } } }}
        {...register("content")}
      />
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
        <Button disabled={isSubmitting} startIcon={<SaveIcon />} type="submit" variant="contained">
          {submitLabel}
        </Button>
        {onValidate ? (
          <Button disabled={isSubmitting} onClick={validateCurrentValues} startIcon={<CheckIcon />} type="button" variant="outlined">
            Validate
          </Button>
        ) : null}
      </Stack>
    </Stack>
  );
}
