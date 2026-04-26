"use client";

import SaveIcon from "@mui/icons-material/Save";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { Customer, CustomerInput } from "@/lib/api/customers";

const customerSchema = z.object({
  name: z.string().trim().min(1).max(200),
  slug: z.string().trim().min(1).max(100),
  description: z.string().trim().max(1000).optional(),
});

type CustomerFormValues = z.infer<typeof customerSchema>;

type CustomerFormProps = {
  customer?: Customer;
  submitLabel: string;
  onSubmit: (input: CustomerInput) => Promise<void>;
};

export function CustomerForm({ customer, submitLabel, onSubmit }: CustomerFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      name: customer?.name ?? "",
      slug: customer?.slug ?? "",
      description: customer?.description ?? "",
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
        }),
      )}
      sx={{ gap: 2 }}
    >
      <TextField
        error={Boolean(errors.name)}
        helperText={errors.name?.message}
        label="Name"
        {...register("name")}
      />
      <TextField
        error={Boolean(errors.slug)}
        helperText={errors.slug?.message}
        label="Slug"
        {...register("slug")}
      />
      <TextField
        error={Boolean(errors.description)}
        helperText={errors.description?.message}
        label="Description"
        minRows={3}
        multiline
        {...register("description")}
      />
      <Button
        disabled={isSubmitting}
        startIcon={<SaveIcon />}
        sx={{ alignSelf: "flex-start" }}
        type="submit"
        variant="contained"
      >
        {submitLabel}
      </Button>
    </Stack>
  );
}
