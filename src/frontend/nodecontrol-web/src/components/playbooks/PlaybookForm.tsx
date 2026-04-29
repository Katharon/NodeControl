"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, MenuItem, Stack, TextField } from "@mui/material";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import type { Playbook, PlaybookArtifactFile, PlaybookInput } from "@/lib/api/playbooks";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;
const artifactFilesExample = JSON.stringify(
  [
    { path: "site.yml", content: "- hosts: all\n  roles:\n    - app\n" },
    { path: "roles/app/tasks/main.yml", content: "- debug:\n    msg: hello\n" },
  ],
  null,
  2,
);

const playbookSchema = z
  .object({
    name: z.string().trim().min(1).max(200),
    slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
    description: z.string().trim().max(1000).optional(),
    sourceType: z.enum(["InlineYaml", "ArtifactDirectory"]),
    inlineContent: z.string().max(200000).optional(),
    entryFilePath: z.string().trim().max(500).optional(),
    artifactFilesText: z.string().max(1000000).optional(),
  })
  .superRefine((values, context) => {
    if (values.sourceType === "InlineYaml") {
      if (!values.inlineContent?.trim()) {
        context.addIssue({ code: "custom", message: "Inline YAML is required", path: ["inlineContent"] });
      }

      return;
    }

    if (!values.entryFilePath?.trim()) {
      context.addIssue({ code: "custom", message: "Entry file path is required", path: ["entryFilePath"] });
    }

    if (values.inlineContent?.trim()) {
      context.addIssue({ code: "custom", message: "Inline YAML must be empty for artifact-directory playbooks", path: ["inlineContent"] });
    }

    const parsed = parseArtifactFiles(values.artifactFilesText);
    if (!parsed.ok) {
      context.addIssue({ code: "custom", message: parsed.message, path: ["artifactFilesText"] });
      return;
    }

    if (!parsed.files.some((file) => file.path === values.entryFilePath?.trim())) {
      context.addIssue({ code: "custom", message: "Artifact files must include the configured entry file", path: ["entryFilePath"] });
    }
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
    control,
  } = useForm<PlaybookFormValues>({
    resolver: zodResolver(playbookSchema),
    defaultValues: {
      name: playbook?.name ?? "",
      slug: playbook?.slug ?? "",
      description: playbook?.description ?? "",
      sourceType: playbook?.sourceType === "ArtifactDirectory" ? "ArtifactDirectory" : "InlineYaml",
      inlineContent: playbook?.inlineContent ?? (playbook?.sourceType === "ArtifactDirectory" ? "" : "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n"),
      entryFilePath: playbook?.entryFilePath ?? "site.yml",
      artifactFilesText: playbook?.artifactFiles?.length ? JSON.stringify(playbook.artifactFiles, null, 2) : artifactFilesExample,
    },
  });
  const sourceType = useWatch({ control, name: "sourceType" });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          slug: values.slug,
          description: values.description || null,
          sourceType: values.sourceType,
          inlineContent: values.sourceType === "InlineYaml" ? values.inlineContent : null,
          entryFilePath: values.entryFilePath || null,
          artifactFiles: values.sourceType === "ArtifactDirectory" ? parseArtifactFiles(values.artifactFilesText).files : null,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField error={Boolean(errors.slug)} helperText={errors.slug?.message} label="Slug" {...register("slug")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <TextField
        error={Boolean(errors.sourceType)}
        helperText={errors.sourceType?.message}
        label="Playbook type"
        select
        {...register("sourceType")}
      >
        <MenuItem value="InlineYaml">Inline YAML</MenuItem>
        <MenuItem value="ArtifactDirectory">Artifact directory / multi-file</MenuItem>
      </TextField>
      <TextField
        error={Boolean(errors.entryFilePath)}
        helperText={errors.entryFilePath?.message}
        label="Entry file path"
        {...register("entryFilePath")}
      />
      {sourceType === "InlineYaml" ? (
        <TextField
          error={Boolean(errors.inlineContent)}
          helperText={errors.inlineContent?.message}
          label="Inline YAML"
          minRows={14}
          multiline
          {...register("inlineContent")}
        />
      ) : (
        <TextField
          error={Boolean(errors.artifactFilesText)}
          helperText={errors.artifactFilesText?.message ?? "JSON array of files with path and content. Paths are relative to the playbook directory."}
          label="Artifact files"
          minRows={16}
          multiline
          {...register("artifactFilesText")}
        />
      )}
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}

function parseArtifactFiles(value: string | undefined): { ok: true; files: PlaybookArtifactFile[]; message?: never } | { ok: false; files: PlaybookArtifactFile[]; message: string } {
  if (!value?.trim()) {
    return { ok: false, files: [], message: "Artifact files are required" };
  }

  try {
    const parsed = JSON.parse(value) as unknown;
    if (!Array.isArray(parsed) || parsed.length === 0) {
      return { ok: false, files: [], message: "Artifact files must be a non-empty JSON array" };
    }

    const files = parsed.map((item) => {
      if (!isArtifactFile(item)) {
        throw new Error("Each artifact file needs path and content strings");
      }

      return { path: item.path.trim(), content: item.content };
    });

    return { ok: true, files };
  } catch (error) {
    return { ok: false, files: [], message: error instanceof Error ? error.message : "Artifact files JSON is invalid" };
  }
}

function isArtifactFile(value: unknown): value is PlaybookArtifactFile {
  return Boolean(
    value
      && typeof value === "object"
      && "path" in value
      && "content" in value
      && typeof value.path === "string"
      && value.path.trim().length > 0
      && typeof value.content === "string",
  );
}
