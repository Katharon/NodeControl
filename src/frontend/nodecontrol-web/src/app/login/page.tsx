import { Box, Container, Paper, Stack, Typography } from "@mui/material";
import { LoginButton } from "@/components/auth/LoginButton";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function LoginPage() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 8 }}>
        <Container maxWidth="sm">
          <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
            <Stack sx={{ alignItems: "flex-start", gap: 2.5 }}>
              <Stack sx={{ gap: 0.5 }}>
                <Typography component="h1" variant="h4">
                  Sign in to NodeControl
                </Typography>
                <Typography color="text.secondary">
                  Nutze den konfigurierten OIDC- oder Fake-Auth-Einstieg für die lokale Demo.
                </Typography>
              </Stack>
              <LoginButton />
            </Stack>
          </Paper>
        </Container>
      </Box>
    </AppProviders>
  );
}
