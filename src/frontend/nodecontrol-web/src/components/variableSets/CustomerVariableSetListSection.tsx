"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { VariableSetList } from "@/components/variableSets/VariableSetList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerVariableSetListSectionProps = {
  customerId: string;
};

export function CustomerVariableSetListSection({ customerId }: CustomerVariableSetListSectionProps) {
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

  const canViewPlaybooks = hasPermission(customerQuery.data.permissions, "ViewPlaybooks");

  if (!canViewPlaybooks) {
    return <Alert severity="warning">You do not have permission to view variable sets for this customer.</Alert>;
  }

  return (
    <VariableSetList
      canManagePlaybooks={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")}
      customerId={customerId}
    />
  );
}
