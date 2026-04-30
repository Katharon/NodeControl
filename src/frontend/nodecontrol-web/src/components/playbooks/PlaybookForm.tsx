"use client";

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import SaveIcon from "@mui/icons-material/Save";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Box, Button, Divider, IconButton, MenuItem, Stack, TextField, Typography } from "@mui/material";
import type { ChangeEvent } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import type { Playbook, PlaybookArtifactFile, PlaybookInput } from "@/lib/api/playbooks";

const slugPattern = /^[a-z0-9][a-z0-9-]{1,99}$/;
const defaultArtifactFiles: PlaybookArtifactFile[] = [
  { path: "site.yml", content: "- hosts: all\n  roles:\n    - app\n" },
  { path: "roles/app/tasks/main.yml", content: "- debug:\n    msg: hello\n" },
];

const artifactFileSchema = z.object({
  path: z.string().trim().min(1, "Path is required").max(500),
  content: z.string().max(200000),
});

const playbookSchema = z
  .object({
    name: z.string().trim().min(1).max(200),
    slug: z.string().trim().regex(slugPattern, "Use lowercase letters, numbers, and hyphens"),
    description: z.string().trim().max(1000).optional(),
    sourceType: z.enum(["InlineYaml", "ArtifactDirectory"]),
    inlineContent: z.string().max(200000).optional(),
    entryFilePath: z.string().trim().max(500).optional(),
    artifactFiles: z.array(artifactFileSchema).max(100, "At most 100 artifact files are supported").optional(),
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

    const files = values.artifactFiles ?? [];
    if (files.length === 0) {
      context.addIssue({ code: "custom", message: "Artifact files are required", path: ["artifactFiles"] });
      return;
    }

    const normalizedEntry = normalizeArtifactPath(values.entryFilePath);
    if (!normalizedEntry.ok) {
      context.addIssue({ code: "custom", message: normalizedEntry.message, path: ["entryFilePath"] });
      return;
    }

    const seenPaths = new Set<string>();
    let totalLength = 0;
    for (const [index, file] of files.entries()) {
      const normalizedPath = normalizeArtifactPath(file.path);
      if (!normalizedPath.ok) {
        context.addIssue({ code: "custom", message: normalizedPath.message, path: ["artifactFiles", index, "path"] });
        continue;
      }

      if (seenPaths.has(normalizedPath.path)) {
        context.addIssue({ code: "custom", message: "Artifact file paths must be unique", path: ["artifactFiles", index, "path"] });
      }

      seenPaths.add(normalizedPath.path);
      totalLength += file.content.length;
    }

    if (totalLength > 1000000) {
      context.addIssue({ code: "custom", message: "Artifact file content is too large", path: ["artifactFiles"] });
    }

    if (!seenPaths.has(normalizedEntry.path)) {
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
    getValues,
    setValue,
  } = useForm<PlaybookFormValues>({
    resolver: zodResolver(playbookSchema),
    defaultValues: {
      name: playbook?.name ?? "",
      slug: playbook?.slug ?? "",
      description: playbook?.description ?? "",
      sourceType: playbook?.sourceType === "ArtifactDirectory" ? "ArtifactDirectory" : "InlineYaml",
      inlineContent: playbook?.inlineContent ?? (playbook?.sourceType === "ArtifactDirectory" ? "" : "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n"),
      entryFilePath: playbook?.entryFilePath ?? "site.yml",
      artifactFiles: playbook?.artifactFiles?.length ? playbook.artifactFiles : defaultArtifactFiles,
    },
  });
  const { append, fields, remove } = useFieldArray({ control, name: "artifactFiles" });
  const sourceType = useWatch({ control, name: "sourceType" });

  async function importArtifactFiles(event: ChangeEvent<HTMLInputElement>) {
    const selectedFiles = Array.from(event.target.files ?? []);
    event.target.value = "";
    if (selectedFiles.length === 0) {
      return;
    }

    const existingFiles = getValues("artifactFiles") ?? [];
    const importedFiles = await Promise.all(
      selectedFiles.map(async (file) => ({
        path: getBrowserFilePath(file),
        content: await file.text(),
      })),
    );

    append(importedFiles);
    if (existingFiles.length === 0 && importedFiles[0]) {
      setValue("entryFilePath", importedFiles[0].path, { shouldDirty: true, shouldValidate: true });
    }
  }

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
          artifactFiles: values.sourceType === "ArtifactDirectory"
            ? (values.artifactFiles ?? []).map((file) => {
                const normalizedPath = normalizeArtifactPath(file.path);
                return {
                  path: normalizedPath.ok ? normalizedPath.path : file.path.trim(),
                  content: file.content,
                };
              })
            : null,
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
        <Stack sx={{ gap: 2 }}>
          <Alert severity={errors.artifactFiles ? "error" : "info"}>
            {errors.artifactFiles?.message ?? "Artifact files are stored as relative paths under the Worker playbook workspace."}
          </Alert>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
            <Button component="label" startIcon={<UploadFileIcon />} variant="outlined">
              Import files
              <input hidden multiple onChange={importArtifactFiles} type="file" />
            </Button>
            <Button
              onClick={() => append({ path: fields.length === 0 ? "site.yml" : "", content: "" })}
              startIcon={<AddIcon />}
              type="button"
              variant="outlined"
            >
              Add file
            </Button>
          </Stack>
          <Stack divider={<Divider />} sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
            {fields.map((field, index) => (
              <Box key={field.id} sx={{ p: 2 }}>
                <Stack sx={{ gap: 1.5 }}>
                  <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "flex-start" }, gap: 1 }}>
                    <TextField
                      error={Boolean(errors.artifactFiles?.[index]?.path)}
                      helperText={errors.artifactFiles?.[index]?.path?.message ?? "Relative path, for example roles/app/tasks/main.yml"}
                      label="Artifact path"
                      sx={{ flex: 1 }}
                      {...register(`artifactFiles.${index}.path`)}
                    />
                    <IconButton aria-label="Remove artifact file" color="warning" onClick={() => remove(index)} sx={{ mt: { sm: 1 } }}>
                      <DeleteIcon />
                    </IconButton>
                  </Stack>
                  <TextField
                    error={Boolean(errors.artifactFiles?.[index]?.content)}
                    helperText={errors.artifactFiles?.[index]?.content?.message}
                    label="File content"
                    minRows={8}
                    multiline
                    slotProps={{ input: { sx: { fontFamily: "monospace", fontSize: 14 } } }}
                    {...register(`artifactFiles.${index}.content`)}
                  />
                </Stack>
              </Box>
            ))}
            {fields.length === 0 ? (
              <Box sx={{ p: 2 }}>
                <Typography color="text.secondary">No artifact files added yet.</Typography>
              </Box>
            ) : null}
          </Stack>
        </Stack>
      )}
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}

function normalizeArtifactPath(value: string | undefined): { ok: true; path: string } | { ok: false; message: string } {
  const normalized = value?.trim().replaceAll("\\", "/") ?? "";
  if (!normalized) {
    return { ok: false, message: "Artifact file path is required" };
  }

  if (
    normalized.length > 500
    || normalized.startsWith("/")
    || normalized.endsWith("/")
    || /^[A-Za-z]:/.test(normalized)
    || normalized.split("/").some((part) => !part.trim() || part === "." || part === "..")
  ) {
    return { ok: false, message: "Artifact file path is invalid" };
  }

  return { ok: true, path: normalized };
}

function getBrowserFilePath(file: File) {
  const relativePath = (file as File & { webkitRelativePath?: string }).webkitRelativePath;
  return (relativePath || file.name).replaceAll("\\", "/");
}
