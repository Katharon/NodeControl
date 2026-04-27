"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { Playbook, PlaybookInput } from "@/lib/api/playbooks";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;

const playbookSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
  description: z.string().trim().max(1000).optional(),
  inlineContent: z.string().min(1).max(200000),
  entryFilePath: z.string().trim().max(500).optional(),
});

type PlaybookFormValues = z.infer<typeof playbookSchema>;

type PlaybookFormProps = {
  playbook?: Playbook;
  submitLabel: string;
  onSubmit: (input: PlaybookInput) => Promise<void>;
};

export function PlaybookForm({ playbook, submitLabel, onSubmit }: PlaybookFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<PlaybookFormValues>({
    resolver: zodResolver(playbookSchema),
    defaultValues: {
      name: playbook?.name ?? "",
      slug: playbook?.slug ?? "",
      description: playbook?.description ?? "",
      inlineContent: playbook?.inlineContent ?? "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n",
      entryFilePath: playbook?.entryFilePath ?? "site.yml",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          sourceType: "InlineYaml",
          inlineContent: values.inlineContent,
          entryFilePath: values.entryFilePath || null,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField
        error={Boolean(errors.entryFilePath)}
        helperText={errors.entryFilePath?.message}
        label="Entry file path"
        {...register("entryFilePath")}
      />
      <TextField
        error={Boolean(errors.inlineContent)}
        helperText={errors.inlineContent?.message}
        label="Inline YAML"
        minRows={14}
        multiline
        {...register("inlineContent")}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
