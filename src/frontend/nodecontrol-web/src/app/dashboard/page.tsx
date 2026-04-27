import BusinessIcon from "@mui/icons-material/Business";
import { Button, Stack, Typography } from "@mui/material";
import { CurrentUserCard } from "@/components/auth/CurrentUserCard";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

export default function DashboardPage() {
  return (
    <AppPage maxWidth="md">
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
          Kunden
        </Button>
      </Stack>
    </AppPage>
  );
}
