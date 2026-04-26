import DashboardIcon from "@mui/icons-material/Dashboard";
import { Box, Button, Container, Stack, Typography } from "@mui/material";
import { LoginButton } from "@/components/auth/LoginButton";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function Home() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 8 }}>
        <Container maxWidth="md">
          <Stack sx={{ alignItems: "flex-start", gap: 3 }}>
            <Typography component="h1" variant="h3">
              NodeControl
            </Typography>
            <Typography color="text.secondary" variant="h6">
              Run Ansible workflows safely through a customer-aware control
              plane.
            </Typography>
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 2 }}>
              <LoginButton />
              <Button
                href="/dashboard"
                startIcon={<DashboardIcon />}
                variant="outlined"
              >
                Dashboard
              </Button>
            </Stack>
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
