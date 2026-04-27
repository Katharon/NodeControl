"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { SecretDetailsCard } from "@/components/secrets/SecretDetailsCard";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerSecretDetailsSectionProps = {
  customerId: string;
  secretId: string;
};

export function CustomerSecretDetailsSection({ customerId, secretId }: CustomerSecretDetailsSectionProps) {
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

  const canViewSecrets = hasPermission(customerQuery.data.permissions, "ViewSecrets");

  if (!canViewSecrets) {
    return <Alert severity="warning">Du hast keine Berechtigung, Secrets für diesen Kunden anzusehen.</Alert>;
  }

  return (
    <SecretDetailsCard
      canManageSecrets={hasPermission(customerQuery.data.permissions, "ManageSecrets")}
      customerId={customerId}
      secretId={secretId}
    />
  );
}
