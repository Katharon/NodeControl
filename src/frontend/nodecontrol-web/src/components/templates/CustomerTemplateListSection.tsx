"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { TemplateList } from "@/components/templates/TemplateList";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerTemplateListSectionProps = {
  customerId: string;
};

export function CustomerTemplateListSection({ customerId }: CustomerTemplateListSectionProps) {
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

  const canViewTemplates = hasPermission(customerQuery.data.permissions, "ViewTemplates");

  if (!canViewTemplates) {
    return <Alert severity="warning">Du hast keine Berechtigung, Templates für diesen Kunden anzusehen.</Alert>;
  }

  return (
    <TemplateList
      canManageTemplates={hasPermission(customerQuery.data.permissions, "ManageTemplates")}
      customerId={customerId}
    />
  );
}
