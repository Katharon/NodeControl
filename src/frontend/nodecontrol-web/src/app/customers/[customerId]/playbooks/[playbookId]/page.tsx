import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { CustomerPlaybookDetailsSection } from "@/components/playbooks/CustomerPlaybookDetailsSection";
import { AppProviders } from "@/lib/app/AppProviders";

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
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
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
        </Container>
      </Box>
    </AppProviders>
  );
}
