"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { PlaybookList } from "@/components/playbooks/PlaybookList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerPlaybookListSectionProps = {
  customerId: string;
};

export function CustomerPlaybookListSection({ customerId }: CustomerPlaybookListSectionProps) {
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
    return <Alert severity="warning">You do not have permission to view playbooks for this customer.</Alert>;
  }

  return (
    <PlaybookList
      canManagePlaybooks={hasPermission(customerQuery.data.permissions, "ManagePlaybooks")}
      customerId={customerId}
    />
  );
}
