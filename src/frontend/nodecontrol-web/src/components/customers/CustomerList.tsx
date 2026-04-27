"use client";

import AddIcon from "@mui/icons-material/Add";
import BusinessIcon from "@mui/icons-material/Business";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { CustomerForm } from "@/components/customers/CustomerForm";
import { ApiError } from "@/lib/api/apiClient";
import { createCustomer, getCustomers } from "@/lib/api/customers";
import { getCurrentUser } from "@/lib/auth/currentUser";

export function CustomerList() {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const customersQuery = useQuery({
    queryKey: ["customers"],
    queryFn: getCustomers,
  });
  const currentUserQuery = useQuery({
    queryKey: ["current-user"],
    queryFn: getCurrentUser,
  });
  const createMutation = useMutation({
    mutationFn: createCustomer,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
      setCreateOpen(false);
    },
  });

  if (customersQuery.isPending) {
    return (
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography>Loading customers</Typography>
        </Stack>
      </Paper>
    );
  }

  if (customersQuery.isError) {
    const unauthorized =
      customersQuery.error instanceof ApiError && customersQuery.error.status === 401;
    return (
      <Alert severity={unauthorized ? "info" : "error"}>
        {unauthorized ? "Melde dich an, um Kunden zu sehen." : "Kunden konnten nicht geladen werden."}
      </Alert>
    );
  }

  const canCreate = currentUserQuery.data?.isPlatformAdmin === true;

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2 }}
      >
        <Box>
          <Typography component="h1" variant="h4">
            Kunden
          </Typography>
          <Typography color="text.secondary">
            Kundenbereiche, auf die dein Account Zugriff hat.
          </Typography>
        </Box>
        {canCreate ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            Neuer Kunde
          </Button>
        ) : null}
      </Stack>

      {customersQuery.data.length === 0 ? (
        <Alert severity="info">Für deinen Account sind keine Kunden verfügbar.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {customersQuery.data.map((customer) => (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                key={customer.id}
                sx={{
                  alignItems: { sm: "center" },
                  justifyContent: "space-between",
                  gap: 2,
                  p: 2,
                }}
              >
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <BusinessIcon color="primary" />
                  <Box>
                    <Typography sx={{ fontWeight: 700 }}>{customer.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {customer.slug}
                    </Typography>
                  </Box>
                </Stack>
                <Button
                  endIcon={<OpenInNewIcon />}
                  href={`/customers/${customer.id}`}
                  variant="outlined"
                >
                  Öffnen
                </Button>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      <Dialog fullWidth maxWidth="sm" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Neuer Kunde</DialogTitle>
        <DialogContent>
          <CustomerForm
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
            submitLabel="Kunde anlegen"
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
