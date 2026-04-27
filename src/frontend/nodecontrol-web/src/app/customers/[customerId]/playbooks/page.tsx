import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerPlaybookListSection } from "@/components/playbooks/CustomerPlaybookListSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type PlaybooksPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function PlaybooksPage({ params }: PlaybooksPageProps) {
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
        <CustomerPlaybookListSection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
