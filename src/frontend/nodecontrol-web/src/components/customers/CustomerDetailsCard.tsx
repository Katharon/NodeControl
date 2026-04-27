"use client";

import GroupsIcon from "@mui/icons-material/Groups";
import HubIcon from "@mui/icons-material/Hub";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import WorkIcon from "@mui/icons-material/Work";
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
import { CustomerForm } from "@/components/customers/CustomerForm";
import { getCustomer, updateCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerDetailsCardProps = {
  customerId: string;
};

export function CustomerDetailsCard({ customerId }: CustomerDetailsCardProps) {
  const queryClient = useQueryClient();
  const customerQuery = useQuery({
    queryKey: ["customer", customerId],
    queryFn: () => getCustomer(customerId),
  });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateCustomer>[1]) =>
      updateCustomer(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customer", customerId] });
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });

  if (customerQuery.isPending) {
    return (
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography>Loading customer</Typography>
        </Stack>
      </Paper>
    );
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  const customer = customerQuery.data;
  const canManageCustomer = hasPermission(customer.permissions, "ManageCustomer");
  const canManageMemberships = hasPermission(customer.permissions, "ManageMemberships");
  const canViewNodes = hasPermission(customer.permissions, "ViewNodes");
  const canViewJobs = hasPermission(customer.permissions, "ViewPlaybooks");
  const canViewJobRuns = hasPermission(customer.permissions, "ViewJobRuns");

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          sx={{ alignItems: { sm: "flex-start" }, justifyContent: "space-between", gap: 2 }}
        >
          <Stack sx={{ gap: 0.5 }}>
            <Typography component="h1" variant="h4">
              {customer.name}
            </Typography>
            <Typography color="text.secondary">{customer.slug}</Typography>
            {customer.description ? <Typography>{customer.description}</Typography> : null}
          </Stack>
          <Stack direction="row" sx={{ gap: 1 }}>
            {canViewNodes ? (
              <Button startIcon={<HubIcon />} href={`/customers/${customer.id}/nodes`} variant="outlined">
                Nodes
              </Button>
            ) : null}
            {canViewJobs ? (
              <Button startIcon={<WorkIcon />} href={`/customers/${customer.id}/jobs`} variant="outlined">
                Jobs
              </Button>
            ) : null}
            {canViewJobRuns ? (
              <Button startIcon={<ReceiptLongIcon />} href={`/customers/${customer.id}/job-runs`} variant="outlined">
                Job Runs
              </Button>
            ) : null}
            {canManageMemberships ? (
              <Button
                startIcon={<GroupsIcon />}
                href={`/customers/${customer.id}/memberships`}
                variant="outlined"
              >
                Memberships
              </Button>
            ) : null}
          </Stack>
        </Stack>

        {canManageCustomer ? (
          <>
            <Divider />
            <CustomerForm
              customer={customer}
              onSubmit={async (input) => {
                await updateMutation.mutateAsync(input);
              }}
              submitLabel="Save customer"
            />
          </>
        ) : null}
      </Stack>
    </Paper>
  );
}
