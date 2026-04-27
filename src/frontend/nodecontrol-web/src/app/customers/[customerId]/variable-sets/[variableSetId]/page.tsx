import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerVariableSetDetailsSection } from "@/components/variableSets/CustomerVariableSetDetailsSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type VariableSetDetailsPageProps = {
  params: Promise<{
    customerId: string;
    variableSetId: string;
  }>;
};

export default async function VariableSetDetailsPage({ params }: VariableSetDetailsPageProps) {
  const { customerId, variableSetId } = await params;

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
        <CustomerVariableSetDetailsSection customerId={customerId} variableSetId={variableSetId} />
      </Stack>
    </AppPage>
  );
}
