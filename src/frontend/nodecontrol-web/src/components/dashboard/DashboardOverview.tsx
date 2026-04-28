"use client";

import BusinessIcon from "@mui/icons-material/Business";
import { Alert, Button, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { CurrentUserCard } from "@/components/auth/CurrentUserCard";
import { ApiError } from "@/lib/api/apiClient";
import { getMyCustomers } from "@/lib/api/customers";

export function DashboardOverview() {
  const customersQuery = useQuery({
    queryKey: ["my-customers"],
    queryFn: getMyCustomers,
  });

  return (
    <Stack sx={{ gap: 3 }}>
      <Stack sx={{ gap: 0.5 }}>
        <Typography component="h1" variant="h4">
          Dashboard
        </Typography>
        <Typography color="text.secondary">
          Überblick für Kunden, Hosts, Actions und Runs.
        </Typography>
      </Stack>

      <CurrentUserCard />

      {customersQuery.isPending ? (
        <Paper sx={{ p: 3 }}>
          <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
            <CircularProgress size={22} />
            <Typography>Kunden werden geladen.</Typography>
          </Stack>
        </Paper>
      ) : null}

      {customersQuery.isError ? (
        <Alert
          action={
            customersQuery.error instanceof ApiError && customersQuery.error.status === 401 ? (
              <Button color="inherit" href="/auth/login" size="small">
                Sign in
              </Button>
            ) : undefined
          }
          severity={
            customersQuery.error instanceof ApiError && customersQuery.error.status === 401
              ? "info"
              : "error"
          }
        >
          {customersQuery.error instanceof ApiError && customersQuery.error.status === 401
            ? "Melde dich an, um das Dashboard zu sehen."
            : "Dashboard-Daten konnten nicht geladen werden."}
        </Alert>
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.length === 0 ? (
        <Paper variant="outlined" sx={{ bgcolor: "background.paper", p: 3 }}>
          <Stack sx={{ gap: 1.5 }}>
            <Typography component="h2" variant="h6">
              Noch keine Kunden vorhanden
            </Typography>
            <Typography color="text.secondary">
              Erstelle zuerst einen Kunden, um Hosts, Actions und Runs zu verwalten.
            </Typography>
            <Button
              href="/customers"
              startIcon={<BusinessIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="contained"
            >
              Kunden öffnen
            </Button>
          </Stack>
        </Paper>
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.length > 0 ? (
        <Paper variant="outlined" sx={{ bgcolor: "background.paper", p: 3 }}>
          <Stack sx={{ gap: 1.5 }}>
            <Typography component="h2" variant="h6">
              Kunden
            </Typography>
            <Typography color="text.secondary">
              {customersQuery.data.length} Kundenbereich
              {customersQuery.data.length === 1 ? "" : "e"} verfügbar.
            </Typography>
            <Button
              href="/customers"
              startIcon={<BusinessIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="contained"
            >
              Kunden öffnen
            </Button>
          </Stack>
        </Paper>
      ) : null}
    </Stack>
  );
}
