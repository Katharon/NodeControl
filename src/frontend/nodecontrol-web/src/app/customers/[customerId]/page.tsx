import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerDetailsCard } from "@/components/customers/CustomerDetailsCard";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type CustomerDetailsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerDetailsPage({ params }: CustomerDetailsPageProps) {
  const { customerId } = await params;

  return (
    <AppPage maxWidth="md">
      <Stack sx={{ gap: 2 }}>
        <Button
          href="/customers"
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunden
        </Button>
        <CustomerDetailsCard customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
