"use client";

import BusinessIcon from "@mui/icons-material/Business";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import { Alert, Button, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/apiClient";
import { getMyCustomers } from "@/lib/api/customers";

type ProductRoutePickerProps = {
  title: string;
  description: string;
  customerPath: string;
};

export function ProductRoutePicker({ title, description, customerPath }: ProductRoutePickerProps) {
  const customersQuery = useQuery({ queryKey: ["my-customers"], queryFn: getMyCustomers });
  const unauthorized = customersQuery.error instanceof ApiError && customersQuery.error.status === 401;

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Typography color="text.secondary">{description}</Typography>
      </Stack>

      {customersQuery.isPending ? (
        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
            <CircularProgress size={22} />
            <Typography>Kundenbereiche werden geladen</Typography>
          </Stack>
        </Paper>
      ) : null}

      {customersQuery.isError ? (
        <Alert
          action={
            unauthorized ? (
              <Button color="inherit" href="/auth/login" size="small">
                Sign in
              </Button>
            ) : (
              <Button color="inherit" onClick={() => void customersQuery.refetch()} size="small">
                Erneut laden
              </Button>
            )
          }
          severity={unauthorized ? "info" : "error"}
        >
          {unauthorized
            ? "Melde dich an, um Kundenbereiche zu öffnen."
            : "Kundenbereiche konnten nicht geladen werden."}
        </Alert>
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.length === 0 ? (
        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack sx={{ gap: 1.5 }}>
            <Typography component="h2" variant="h6">
              Kein Kundenbereich verfügbar
            </Typography>
            <Typography color="text.secondary">
              Dieser Bereich ist kundengebunden. Sobald dein Account Zugriff auf einen Kunden hat,
              erscheint hier der passende Einstieg.
            </Typography>
            <Button href="/customers" sx={{ alignSelf: "flex-start" }} variant="outlined">
              Kunden öffnen
            </Button>
          </Stack>
        </Paper>
      ) : customersQuery.isSuccess ? (
        <Paper variant="outlined">
          <Stack>
            {customersQuery.data.map((customer) => (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                key={customer.id}
                sx={{
                  alignItems: { sm: "center" },
                  borderBottom: 1,
                  borderColor: "divider",
                  justifyContent: "space-between",
                  gap: 2,
                  p: 2,
                  "&:last-child": { borderBottom: 0 },
                }}
              >
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <BusinessIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{customer.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {customer.slug}
                    </Typography>
                  </Stack>
                </Stack>
                <Button
                  endIcon={<OpenInNewIcon />}
                  href={`/customers/${customer.id}/${customerPath}`}
                  variant="outlined"
                >
                  Öffnen
                </Button>
              </Stack>
            ))}
          </Stack>
        </Paper>
      ) : null}
    </Stack>
  );
}
