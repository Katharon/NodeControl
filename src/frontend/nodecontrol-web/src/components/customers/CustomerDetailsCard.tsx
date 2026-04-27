"use client";

import GroupsIcon from "@mui/icons-material/Groups";
import HistoryIcon from "@mui/icons-material/History";
import HubIcon from "@mui/icons-material/Hub";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import ScheduleIcon from "@mui/icons-material/Schedule";
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
  const canViewSchedules = hasPermission(customer.permissions, "ViewSchedules");
  const canViewAudit = hasPermission(customer.permissions, "ViewAuditLogs");

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
              <Button startIcon={<HubIcon />} href={`/customers/${customer.id}/hosts`} variant="outlined">
                Hosts
              </Button>
            ) : null}
            {canViewJobs ? (
              <Button startIcon={<WorkIcon />} href={`/customers/${customer.id}/actions`} variant="outlined">
                Actions
              </Button>
            ) : null}
            {canViewJobRuns ? (
              <Button startIcon={<ReceiptLongIcon />} href={`/customers/${customer.id}/runs`} variant="outlined">
                Runs
              </Button>
            ) : null}
            {canViewSchedules ? (
              <Button startIcon={<ScheduleIcon />} href={`/customers/${customer.id}/schedules`} variant="outlined">
                Schedules
              </Button>
            ) : null}
            {canViewAudit ? (
              <Button startIcon={<HistoryIcon />} href={`/customers/${customer.id}/audit`} variant="outlined">
                Audit
              </Button>
            ) : null}
            {canManageMemberships ? (
              <Button
                startIcon={<GroupsIcon />}
                href={`/customers/${customer.id}/memberships`}
                variant="outlined"
              >
                Benutzer
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
              submitLabel="Kunde speichern"
            />
          </>
        ) : null}
      </Stack>
    </Paper>
  );
}
