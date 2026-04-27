import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { CustomerNodesSections } from "@/components/nodes/CustomerNodesSections";

export const dynamic = "force-dynamic";

type CustomerHostsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerHostsPage({ params }: CustomerHostsPageProps) {
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
        <CustomerNodesSections customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
