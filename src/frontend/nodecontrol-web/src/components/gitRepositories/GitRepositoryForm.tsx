"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { GitRepository, GitRepositoryInput } from "@/lib/api/gitRepositories";

const gitRepositorySchema = z.object({
  name: z.string().trim().min(1).max(200),
  repositoryUrl: z.string().trim().min(1).max(1000),
  branch: z.string().trim().max(200).optional(),
  revision: z.string().trim().max(200).optional(),
  subpath: z.string().trim().max(500).optional(),
});

type GitRepositoryFormValues = z.infer<typeof gitRepositorySchema>;

type GitRepositoryFormProps = {
  repository?: GitRepository;
  submitLabel: string;
  onSubmit: (input: GitRepositoryInput) => Promise<void>;
};

export function GitRepositoryForm({ repository, submitLabel, onSubmit }: GitRepositoryFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<GitRepositoryFormValues>({
    resolver: zodResolver(gitRepositorySchema),
    defaultValues: {
      name: repository?.name ?? "",
      repositoryUrl: repository?.repositoryUrl ?? "",
      branch: repository?.branch ?? "main",
      revision: repository?.revision ?? "",
      subpath: repository?.subpath ?? "",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          repositoryUrl: values.repositoryUrl,
          branch: values.branch || null,
          revision: values.revision || null,
          subpath: values.subpath || null,
        }),
      )}
      sx={{ gap: 2, pt: 1 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField
        error={Boolean(errors.repositoryUrl)}
        helperText={errors.repositoryUrl?.message ?? "HTTPS GitHub URLs and SSH-style Git URLs can be stored. Browser import currently supports public GitHub repositories."}
        label="Repository URL"
        {...register("repositoryUrl")}
      />
      <TextField
        error={Boolean(errors.branch)}
        helperText={errors.branch?.message ?? "Used when no revision is specified."}
        label="Branch"
        {...register("branch")}
      />
      <TextField
        error={Boolean(errors.revision)}
        helperText={errors.revision?.message ?? "Optional commit SHA, tag, or ref for one-time imports."}
        label="Revision"
        {...register("revision")}
      />
      <TextField
        error={Boolean(errors.subpath)}
        helperText={errors.subpath?.message ?? "Optional folder within the repository, for example ansible/playbooks."}
        label="Subpath"
        {...register("subpath")}
      />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
