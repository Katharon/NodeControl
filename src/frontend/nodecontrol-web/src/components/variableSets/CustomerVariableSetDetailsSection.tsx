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
    return <Alert severity="error">Dieser Kunde konnte nicht geladen werden.</Alert>;
  }

  const canViewPlaybooks = hasPermission(customerQuery.data.permissions, "ViewPlaybooks");

  if (!canViewPlaybooks) {
    return <Alert severity="warning">Du hast keine Berechtigung, Variablen für diesen Kunden anzusehen.</Alert>;
  }

  return (
    <VariableSetDetailsCard
      canManagePlaybooks={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")}
      customerId={customerId}
      variableSetId={variableSetId}
    />
  );
}
