import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { CustomerPlaybookListSection } from "@/components/playbooks/CustomerPlaybookListSection";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

type PlaybooksPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function PlaybooksPage({ params }: PlaybooksPageProps) {
  const { customerId } = await params;

  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Customer
            </Button>
            <CustomerPlaybookListSection customerId={customerId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
