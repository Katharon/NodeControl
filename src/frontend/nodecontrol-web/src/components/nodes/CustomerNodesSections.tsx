"use client";

import HubIcon from "@mui/icons-material/Hub";
import AddIcon from "@mui/icons-material/Add";
import { Alert, Box, Button, CircularProgress, Dialog, DialogContent, DialogTitle, Stack, Tab, Tabs, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { HostWizard } from "@/components/hosts/HostWizard";
import { InventoryGroupList } from "@/components/inventories/InventoryGroupList";
import { ControlNodeList } from "@/components/nodes/ControlNodeList";
import { ManagedNodeList } from "@/components/nodes/ManagedNodeList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerNodesSectionsProps = {
  customerId: string;
};

export function CustomerNodesSections({ customerId }: CustomerNodesSectionsProps) {
  const [tab, setTab] = useState(0);
  const [createOpen, setCreateOpen] = useState(false);
  const customerQuery = useQuery({
    queryKey: ["customer", customerId],
    queryFn: () => getCustomer(customerId),
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">This customer could not be loaded.</Alert>;
  }

  const customer = customerQuery.data;
  const canViewNodes = hasPermission(customer.permissions, "ViewNodes");
  const canManageNodes = hasPermission(customer.permissions, "ManageNodes");
  const canViewSecrets = hasPermission(customer.permissions, "ViewSecrets");

  if (!canViewNodes) {
    return <Alert severity="warning">You do not have permission to view nodes for this customer.</Alert>;
  }

  return (
    <Stack sx={{ gap: 3 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
          <HubIcon color="primary" />
          <Box>
            <Typography component="h1" variant="h4">
              Hosts
            </Typography>
            <Typography color="text.secondary">{customer.name} · Hosts, Control Hosts und Inventare.</Typography>
          </Box>
        </Stack>
        {canManageNodes ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            Neuer Host
          </Button>
        ) : null}
      </Stack>

      <Tabs onChange={(_, value: number) => setTab(value)} value={tab}>
        <Tab label="Control Hosts" />
        <Tab label="Hosts" />
        <Tab label="Inventare" />
      </Tabs>

      {tab === 0 ? (
        <ControlNodeList
          canManageNodes={canManageNodes}
          canViewSecrets={canViewSecrets}
          customerId={customerId}
          showCreateButton={false}
        />
      ) : null}
      {tab === 1 ? (
        <ManagedNodeList
          canManageNodes={canManageNodes}
          canViewSecrets={canViewSecrets}
          customerId={customerId}
          showCreateButton={false}
        />
      ) : null}
      {tab === 2 ? <InventoryGroupList canManageNodes={canManageNodes} customerId={customerId} /> : null}

      <Dialog fullWidth maxWidth="md" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Neuer Host</DialogTitle>
        <DialogContent>
          <HostWizard
            customerId={customerId}
            customerName={customer.name}
            onCreated={() => setCreateOpen(false)}
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
