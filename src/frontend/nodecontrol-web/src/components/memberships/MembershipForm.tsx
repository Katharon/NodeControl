"use client";

import PersonAddIcon from "@mui/icons-material/PersonAdd";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, MenuItem, Stack, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  type CreateCustomerMembershipInput,
  customerRoles,
} from "@/lib/api/memberships";

const membershipSchema = z.object({
  userId: z.string().uuid(),
  role: z.enum(customerRoles),
});

type MembershipFormValues = z.infer<typeof membershipSchema>;

type MembershipFormProps = {
  onSubmit: (input: CreateCustomerMembershipInput) => Promise<void>;
};

export function MembershipForm({ onSubmit }: MembershipFormProps) {
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    reset,
  } = useForm<MembershipFormValues>({
    resolver: zodResolver(membershipSchema),
    defaultValues: {
      userId: "",
      role: "Viewer",
    },
  });

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit(async (values) => {
        await onSubmit(values);
        reset();
      })}
      sx={{ gap: 2 }}
    >
      <TextField
        error={Boolean(errors.userId)}
        helperText={errors.userId?.message}
        label="User ID"
        {...register("userId")}
      />
      <TextField label="Role" select {...register("role")}>
        {customerRoles.map((role) => (
          <MenuItem key={role} value={role}>
            {role}
          </MenuItem>
        ))}
      </TextField>
      <Button
        disabled={isSubmitting}
        startIcon={<PersonAddIcon />}
        sx={{ alignSelf: "flex-start" }}
        type="submit"
        variant="contained"
      >
        Add membership
      </Button>
    </Stack>
  );
}
