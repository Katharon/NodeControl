"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { VariableSetDetailsCard } from "@/components/variableSets/VariableSetDetailsCard";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerVariableSetDetailsSectionProps = {
  customerId: string;
  variableSetId: string;
};

export function CustomerVariableSetDetailsSection({ customerId, variableSetId }: CustomerVariableSetDetailsSectionProps) {
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
    <VariableSetDetailsCard
      canManagePlaybooks={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")}
      customerId={customerId}
      variableSetId={variableSetId}
    />
  );
}
