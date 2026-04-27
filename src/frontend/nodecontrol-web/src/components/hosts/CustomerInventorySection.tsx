"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { InventoryGroupList } from "@/components/inventories/InventoryGroupList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerInventorySectionProps = {
  customerId: string;
};

export function CustomerInventorySection({ customerId }: CustomerInventorySectionProps) {
  const customerQuery = useQuery({
    queryKey: ["customer", customerId],
    queryFn: () => getCustomer(customerId),
  });

  if (customerQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (customerQuery.isError) {
    return <Alert severity="error">Dieser Kunde konnte nicht geladen werden.</Alert>;
  }

  const canViewNodes = hasPermission(customerQuery.data.permissions, "ViewNodes");

  if (!canViewNodes) {
    return <Alert severity="warning">Du hast keine Berechtigung, Inventare für diesen Kunden anzusehen.</Alert>;
  }

  return (
    <InventoryGroupList
      canManageNodes={hasPermission(customerQuery.data.permissions, "ManageNodes")}
      customerId={customerId}
    />
  );
}
