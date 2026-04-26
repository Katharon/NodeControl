import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { CustomerDetailsCard } from "@/components/customers/CustomerDetailsCard";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

type CustomerDetailsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function CustomerDetailsPage({ params }: CustomerDetailsPageProps) {
  const { customerId } = await params;

  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="md">
          <Stack sx={{ gap: 2 }}>
            <Button
              href="/customers"
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Customers
            </Button>
            <CustomerDetailsCard customerId={customerId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
