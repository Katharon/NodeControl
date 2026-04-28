import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { HostHealthOverview } from "@/components/hostHealth/HostHealthOverview";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type CustomerHostHealthPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerHostHealthPage({ params }: CustomerHostHealthPageProps) {
  const { customerId } = await params;

  return (
    <AppPage maxWidth="lg">
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunde
        </Button>
        <HostHealthOverview customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
