"use client";

import HubIcon from "@mui/icons-material/Hub";
import { Alert, Box, CircularProgress, Stack, Tab, Tabs, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
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

  if (!canViewNodes) {
    return <Alert severity="warning">You do not have permission to view nodes for this customer.</Alert>;
  }

  return (
    <Stack sx={{ gap: 3 }}>
      <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
        <HubIcon color="primary" />
        <Box>
          <Typography component="h1" variant="h4">
            {customer.name} Nodes
          </Typography>
          <Typography color="text.secondary">Target structure and inventory preview.</Typography>
        </Box>
      </Stack>

      <Tabs onChange={(_, value: number) => setTab(value)} value={tab}>
        <Tab label="Control Nodes" />
        <Tab label="Managed Nodes" />
        <Tab label="Inventory Groups" />
      </Tabs>

      {tab === 0 ? <ControlNodeList canManageNodes={canManageNodes} customerId={customerId} /> : null}
      {tab === 1 ? <ManagedNodeList canManageNodes={canManageNodes} customerId={customerId} /> : null}
      {tab === 2 ? <InventoryGroupList canManageNodes={canManageNodes} customerId={customerId} /> : null}
    </Stack>
  );
}
