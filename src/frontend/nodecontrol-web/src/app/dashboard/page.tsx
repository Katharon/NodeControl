import BusinessIcon from "@mui/icons-material/Business";
import { Box, Button, Container, Stack, Typography } from "@mui/material";
import { CurrentUserCard } from "@/components/auth/CurrentUserCard";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function DashboardPage() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="md">
          <Stack sx={{ gap: 3 }}>
            <Typography component="h1" variant="h4">
              Dashboard
            </Typography>
            <CurrentUserCard />
            <Button
              href="/customers"
              startIcon={<BusinessIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="contained"
            >
              Customers
            </Button>
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
