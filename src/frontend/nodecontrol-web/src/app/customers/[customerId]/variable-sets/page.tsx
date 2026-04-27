import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerVariableSetListSection } from "@/components/variableSets/CustomerVariableSetListSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type VariableSetsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function VariableSetsPage({ params }: VariableSetsPageProps) {
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
        <CustomerVariableSetListSection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
