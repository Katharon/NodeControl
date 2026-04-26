import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { MembershipList } from "@/components/memberships/MembershipList";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

type MembershipsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function MembershipsPage({ params }: MembershipsPageProps) {
  const { customerId } = await params;

  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="md">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Customer
            </Button>
            <MembershipList customerId={customerId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
