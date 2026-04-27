import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { CustomerVariableSetDetailsSection } from "@/components/variableSets/CustomerVariableSetDetailsSection";
import { AppProviders } from "@/lib/app/AppProviders";

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
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}/variable-sets`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Variable Sets
            </Button>
            <CustomerVariableSetDetailsSection customerId={customerId} variableSetId={variableSetId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
