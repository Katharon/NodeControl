"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { GitRepositoryList } from "@/components/gitRepositories/GitRepositoryList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerGitRepositoryListSectionProps = {
  customerId: string;
};

export function CustomerGitRepositoryListSection({ customerId }: CustomerGitRepositoryListSectionProps) {
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
    return <Alert severity="warning">You do not have permission to view Git repositories for this customer.</Alert>;
  }

  return (
    <GitRepositoryList
      canManageGitRepositories={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")}
      customerId={customerId}
    />
  );
}
