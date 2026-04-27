import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { CustomerTemplateDetailsSection } from "@/components/templates/CustomerTemplateDetailsSection";

export const dynamic = "force-dynamic";

type TemplateDetailsPageProps = {
  params: Promise<{
    customerId: string;
    templateId: string;
  }>;
};

export default async function TemplateDetailsPage({ params }: TemplateDetailsPageProps) {
  const { customerId, templateId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/templates`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Templates
        </Button>
        <CustomerTemplateDetailsSection customerId={customerId} templateId={templateId} />
      </Stack>
    </AppPage>
  );
}
