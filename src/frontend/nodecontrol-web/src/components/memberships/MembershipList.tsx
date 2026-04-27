"use client";

import BlockIcon from "@mui/icons-material/Block";
import {
  Alert,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/apiClient";
import {
  createCustomerMembership,
  deactivateCustomerMembership,
  getCustomerMemberships,
} from "@/lib/api/memberships";
import { MembershipForm } from "@/components/memberships/MembershipForm";

type MembershipListProps = {
  customerId: string;
};

export function MembershipList({ customerId }: MembershipListProps) {
  const queryClient = useQueryClient();
  const membershipsQuery = useQuery({
    queryKey: ["customer-memberships", customerId],
    queryFn: () => getCustomerMemberships(customerId),
  });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createCustomerMembership>[1]) =>
      createCustomerMembership(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["customer-memberships", customerId],
      });
    },
  });
  const deactivateMutation = useMutation({
    mutationFn: (membershipId: string) =>
      deactivateCustomerMembership(customerId, membershipId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["customer-memberships", customerId],
      });
    },
  });

  if (membershipsQuery.isPending) {
    return (
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography>Lade Benutzer</Typography>
        </Stack>
      </Paper>
    );
  }

  if (membershipsQuery.isError) {
    const forbidden =
      membershipsQuery.error instanceof ApiError && membershipsQuery.error.status === 403;
    return (
      <Alert severity={forbidden ? "warning" : "error"}>
        {forbidden
          ? "Du hast keine Berechtigung, Benutzer für diesen Kunden zu verwalten."
          : "Benutzer konnten nicht geladen werden."}
      </Alert>
    );
  }

  return (
    <Stack sx={{ gap: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Stack sx={{ gap: 2 }}>
          <Typography component="h1" variant="h4">
            Benutzer
          </Typography>
          <MembershipForm
            customerId={customerId}
            existingUserIds={membershipsQuery.data.map((membership) => membership.userId)}
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
          />
          {createMutation.isError ? (
            <Alert severity="error">
              Mitgliedschaft konnte nicht angelegt werden. Prüfe, ob der Benutzer bereits
              Mitglied ist.
            </Alert>
          ) : null}
        </Stack>
      </Paper>

      <Paper>
        <Stack divider={<Divider />}>
          {membershipsQuery.data.map((membership) => (
            <Stack
              direction={{ xs: "column", sm: "row" }}
              key={membership.id}
              sx={{
                alignItems: { sm: "center" },
                justifyContent: "space-between",
                gap: 2,
                p: 2,
              }}
            >
              <Stack sx={{ gap: 0.5 }}>
                <Typography sx={{ fontWeight: 700 }}>{membership.userDisplayName}</Typography>
                <Typography color="text.secondary" variant="body2">
                  {membership.userEmail}
                </Typography>
                <Typography variant="body2">
                  {membership.role} · {membership.isActive ? "Active" : "Inactive"}
                </Typography>
              </Stack>
              {membership.isActive ? (
                <Button
                  color="warning"
                  disabled={deactivateMutation.isPending}
                  onClick={() => deactivateMutation.mutate(membership.id)}
                  startIcon={<BlockIcon />}
                  variant="outlined"
                >
                  Deactivate
                </Button>
              ) : null}
            </Stack>
          ))}
        </Stack>
      </Paper>
    </Stack>
  );
}
