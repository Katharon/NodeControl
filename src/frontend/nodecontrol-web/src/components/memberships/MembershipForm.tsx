"use client";

import PersonAddIcon from "@mui/icons-material/PersonAdd";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  CircularProgress,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import {
  type CreateCustomerMembershipInput,
  customerRoles,
} from "@/lib/api/memberships";
import {
  searchUsersForCustomerMembership,
  type UserLookupResult,
} from "@/lib/api/userLookup";

const membershipSchema = z.object({
  userId: z.string().uuid(),
  role: z.enum(customerRoles),
});

type MembershipFormValues = z.infer<typeof membershipSchema>;

type MembershipFormProps = {
  customerId: string;
  existingUserIds: string[];
  onSubmit: (input: CreateCustomerMembershipInput) => Promise<void>;
};

export function MembershipForm({ customerId, existingUserIds, onSubmit }: MembershipFormProps) {
  const [userSearch, setUserSearch] = useState("");
  const existingUserIdSet = useMemo(() => new Set(existingUserIds), [existingUserIds]);
  const usersQuery = useQuery({
    queryKey: ["customer-user-lookup", customerId, userSearch],
    queryFn: () => searchUsersForCustomerMembership(customerId, userSearch),
  });
  const hasAvailableUsers =
    usersQuery.data?.some((user) => !existingUserIdSet.has(user.id)) ?? false;
  const {
    control,
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
      <Controller
        control={control}
        name="userId"
        render={({ field }) => {
          const selectedUser =
            usersQuery.data?.find((user) => user.id === field.value) ?? null;
          const options =
            usersQuery.data?.filter((user) => !existingUserIdSet.has(user.id)) ?? [];

          return (
            <Autocomplete<UserLookupResult>
              filterOptions={(optionsToFilter) => optionsToFilter}
              getOptionLabel={(option) => `${option.displayName} (${option.email})`}
              inputValue={userSearch}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              loading={usersQuery.isPending}
              noOptionsText="Keine passenden Benutzer gefunden."
              onChange={(_, value) => field.onChange(value?.id ?? "")}
              onInputChange={(_, value) => setUserSearch(value)}
              options={options}
              renderInput={(params) => (
                <TextField
                  {...params}
                  error={Boolean(errors.userId)}
                  helperText={
                    errors.userId?.message ??
                    "Suche nach E-Mail-Adresse oder Anzeigename."
                  }
                  inputRef={field.ref}
                  label="Benutzer"
                  slotProps={{
                    input: {
                      ...params.slotProps.input,
                      endAdornment: (
                        <>
                          {usersQuery.isPending ? <CircularProgress size={18} /> : null}
                          {params.slotProps.input.endAdornment}
                        </>
                      ),
                    },
                    htmlInput: params.slotProps.htmlInput,
                  }}
                />
              )}
              renderOption={({ key, ...props }, option) => (
                <Box component="li" key={key} {...props}>
                  <Stack sx={{ minWidth: 0 }}>
                    <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
                      <Typography sx={{ fontWeight: 700 }}>{option.displayName}</Typography>
                      {option.isPlatformAdmin ? (
                        <Chip label="Platform Admin" size="small" />
                      ) : null}
                    </Stack>
                    <Typography color="text.secondary" variant="body2">
                      {option.email}
                    </Typography>
                  </Stack>
                </Box>
              )}
              value={selectedUser}
            />
          );
        }}
      />
      {usersQuery.isError ? (
        <Alert severity="error">Benutzer konnten nicht geladen werden.</Alert>
      ) : null}
      {usersQuery.isSuccess && usersQuery.data.length === 0 ? (
        <Alert severity="info">
          Es gibt noch keine auswählbaren Benutzer. Benutzerverwaltung und Einladungen
          folgen in einem späteren Slice.
        </Alert>
      ) : null}
      {usersQuery.isSuccess && usersQuery.data.length > 0 && !hasAvailableUsers ? (
        <Alert severity="info">
          Alle gefundenen Benutzer sind bereits Mitglied. Weitere Benutzerverwaltung und
          Einladungen folgen später.
        </Alert>
      ) : null}
      <TextField label="Role" select {...register("role")}>
        {customerRoles.map((role) => (
          <MenuItem key={role} value={role}>
            {role}
          </MenuItem>
        ))}
      </TextField>
      <Button
        disabled={isSubmitting || usersQuery.isPending}
        startIcon={<PersonAddIcon />}
        sx={{ alignSelf: "flex-start" }}
        type="submit"
        variant="contained"
      >
        Mitglied hinzufügen
      </Button>
    </Stack>
  );
}
