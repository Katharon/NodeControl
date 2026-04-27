import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerInventorySection } from "@/components/hosts/CustomerInventorySection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type CustomerInventoriesPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerInventoriesPage({ params }: CustomerInventoriesPageProps) {
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
        <CustomerInventorySection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
