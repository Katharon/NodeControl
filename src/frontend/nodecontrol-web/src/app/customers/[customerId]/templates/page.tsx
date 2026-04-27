import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { CustomerTemplateListSection } from "@/components/templates/CustomerTemplateListSection";

export const dynamic = "force-dynamic";

type TemplatesPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function TemplatesPage({ params }: TemplatesPageProps) {
  const { customerId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunde
        </Button>
        <CustomerTemplateListSection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
