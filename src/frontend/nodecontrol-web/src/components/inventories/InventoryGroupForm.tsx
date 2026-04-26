"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { InventoryGroup, InventoryGroupInput } from "@/lib/api/inventoryGroups";

const inventoryName = /^[a-zA-Z][a-zA-Z0-9_-]{1,99}$/;

const inventoryGroupSchema = z.object({
  name: z.string().trim().regex(inventoryName, "Use an inventory-safe group name"),
  description: z.string().trim().max(1000).optional(),
});

type InventoryGroupFormValues = z.infer<typeof inventoryGroupSchema>;

type InventoryGroupFormProps = {
  inventoryGroup?: InventoryGroup;
  submitLabel: string;
  onSubmit: (input: InventoryGroupInput) => Promise<void>;
};

export function InventoryGroupForm({
  inventoryGroup,
  submitLabel,
  onSubmit,
}: InventoryGroupFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<InventoryGroupFormValues>({
    resolver: zodResolver(inventoryGroupSchema),
    defaultValues: {
      name: inventoryGroup?.name ?? "",
      description: inventoryGroup?.description ?? "",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) =>
        onSubmit({
          name: values.name,
          description: values.description || null,
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField error={Boolean(errors.name)} helperText={errors.name?.message} label="Name" {...register("name")} />
      <TextField label="Description" minRows={2} multiline {...register("description")} />
      <Button disabled={isSubmitting} startIcon={<SaveIcon />} sx={{ alignSelf: "flex-start" }} type="submit" variant="contained">
        {submitLabel}
      </Button>
    </Stack>
  );
}
