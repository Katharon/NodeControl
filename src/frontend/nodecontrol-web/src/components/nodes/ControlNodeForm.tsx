"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { ControlNode, ControlNodeInput } from "@/lib/api/controlNodes";

const controlNodeSchema = z.object({
  name: z.string().trim().min(1).max(200),
  hostname: z.string().trim().min(1).max(253).refine((value) => !/\s/.test(value), {
    message: "Hostname must not contain whitespace",
  }),
  sshPort: z.number().int().min(1).max(65535),
  description: z.string().trim().max(1000).optional(),
});

type ControlNodeFormValues = z.infer<typeof controlNodeSchema>;

type ControlNodeFormProps = {
  controlNode?: ControlNode;
  submitLabel: string;
  onSubmit: (input: ControlNodeInput) => Promise<void>;
};

export function ControlNodeForm({ controlNode, submitLabel, onSubmit }: ControlNodeFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<ControlNodeFormValues>({
    resolver: zodResolver(controlNodeSchema),
    defaultValues: {
      name: controlNode?.name ?? "",
      hostname: controlNode?.hostname ?? "",
      sshPort: controlNode?.sshPort ?? 22,
      description: controlNode?.description ?? "",
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
