import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerPlaybookDetailsSection } from "@/components/playbooks/CustomerPlaybookDetailsSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type PlaybookDetailsPageProps = {
  params: Promise<{
    customerId: string;
    playbookId: string;
  }>;
};

export default async function PlaybookDetailsPage({ params }: PlaybookDetailsPageProps) {
  const { customerId, playbookId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/playbooks`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Playbooks
        </Button>
        <CustomerPlaybookDetailsSection customerId={customerId} playbookId={playbookId} />
      </Stack>
    </AppPage>
  );
}
