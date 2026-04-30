"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { GitArtifactImport } from "@/components/imports/GitArtifactImport";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerGitArtifactImportSectionProps = {
  customerId: string;
};

export function CustomerGitArtifactImportSection({ customerId }: CustomerGitArtifactImportSectionProps) {
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

  if (!hasPermission(customerQuery.data.permissions, "ViewPlaybooks")) {
    return <Alert severity="warning">You do not have permission to view imports for this customer.</Alert>;
  }

  return (
    <GitArtifactImport
      canManageArtifacts={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")
        || hasPermission(customerQuery.data.permissions, "ManageTemplates")}
      customerId={customerId}
    />
  );
}
