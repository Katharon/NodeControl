"use client";

import BusinessIcon from "@mui/icons-material/Business";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import { Alert, Button, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getMyCustomers } from "@/lib/api/customers";

type ProductRoutePickerProps = {
  title: string;
  description: string;
  customerPath: string;
};

export function ProductRoutePicker({ title, description, customerPath }: ProductRoutePickerProps) {
  const customersQuery = useQuery({ queryKey: ["my-customers"], queryFn: getMyCustomers });

  if (customersQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customersQuery.isError) {
    return <Alert severity="error">Kunden konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Typography color="text.secondary">{description}</Typography>
      </Stack>

      {customersQuery.data.length === 0 ? (
        <Alert severity="info">Für deinen Account sind noch keine Kunden verfügbar.</Alert>
      ) : (
        <Paper>
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
      )}
    </Stack>
  );
}
