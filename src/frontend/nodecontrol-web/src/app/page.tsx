import { Box, Container, Paper, Stack, Typography } from "@mui/material";
import { LoginButton } from "@/components/auth/LoginButton";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function Home() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 8 }}>
        <Container maxWidth="md">
          <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
            <Stack sx={{ alignItems: "flex-start", gap: 3 }}>
              <Stack sx={{ gap: 1 }}>
                <Typography component="h1" variant="h3">
                  NodeControl
                </Typography>
                <Typography color="text.secondary" variant="h6">
                  Run Ansible workflows safely through a customer-aware control plane.
                </Typography>
              </Stack>
              <Stack sx={{ alignItems: "flex-start", gap: 1 }}>
                <LoginButton />
                <Typography color="text.secondary" variant="body2">
                  In der lokalen Demo meldest du dich als Dev Admin an.
                </Typography>
              </Stack>
            </Stack>
          </Paper>
        </Container>
      </Box>
    </AppProviders>
  );
}
