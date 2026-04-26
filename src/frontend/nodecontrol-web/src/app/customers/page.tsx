import { Box, Container } from "@mui/material";
import { CustomerList } from "@/components/customers/CustomerList";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function CustomersPage() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="md">
          <CustomerList />
        </Container>
      </Box>
    </AppProviders>
  );
}
