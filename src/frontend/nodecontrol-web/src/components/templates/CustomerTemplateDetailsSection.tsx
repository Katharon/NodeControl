"use client";

import { Alert, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { TemplateDetailsCard } from "@/components/templates/TemplateDetailsCard";
import { getCustomer } from "@/lib/api/customers";
import { hasPermission } from "@/lib/auth/permissions";

type CustomerTemplateDetailsSectionProps = {
  customerId: string;
  templateId: string;
};

export function CustomerTemplateDetailsSection({ customerId, templateId }: CustomerTemplateDetailsSectionProps) {
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
    <TemplateDetailsCard
      canManageTemplates={hasPermission(customerQuery.data.permissions, "ManageTemplates")}
      customerId={customerId}
      templateId={templateId}
    />
  );
}
