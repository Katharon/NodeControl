import { Box, Container, Stack, Typography } from "@mui/material";
import { LoginButton } from "@/components/auth/LoginButton";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

export default function LoginPage() {
  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 8 }}>
        <Container maxWidth="sm">
          <Stack sx={{ alignItems: "flex-start", gap: 3 }}>
            <Typography component="h1" variant="h4">
              Sign in to NodeControl
            </Typography>
            <LoginButton />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
