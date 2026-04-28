import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { RunWizard } from "@/components/runWizard/RunWizard";

export const dynamic = "force-dynamic";

type CustomerRunWizardPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerRunWizardPage({ params }: CustomerRunWizardPageProps) {
  const { customerId } = await params;

  return (
    <AppPage maxWidth="xl">
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunde
        </Button>
        <RunWizard initialCustomerId={customerId} />
      </Stack>
    </AppPage>
  );
}
