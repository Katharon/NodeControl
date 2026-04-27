import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { CustomerVariableSetDetailsSection } from "@/components/variableSets/CustomerVariableSetDetailsSection";

export const dynamic = "force-dynamic";

type VariableDetailsPageProps = {
  params: Promise<{
    customerId: string;
    variableId: string;
  }>;
};

export default async function VariableDetailsPage({ params }: VariableDetailsPageProps) {
  const { customerId, variableId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/variables`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Variablen
        </Button>
        <CustomerVariableSetDetailsSection customerId={customerId} variableSetId={variableId} />
      </Stack>
    </AppPage>
  );
}
