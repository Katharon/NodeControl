import { Box, Container, Stack, Typography } from "@mui/material";
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
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
