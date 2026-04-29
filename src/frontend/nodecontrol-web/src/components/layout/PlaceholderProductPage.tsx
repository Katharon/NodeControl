import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { Button, Chip, Divider, Paper, Stack, Typography } from "@mui/material";

type PlaceholderProductPageProps = {
  title: string;
  description: string;
  currentBoundary?: string;
  scopeNote?: string;
};

export function PlaceholderProductPage({
  title,
  description,
  currentBoundary = "Der aktuelle MVP konzentriert sich auf Kunden, Hosts, Inventare, Playbooks, Variablen, Actions, Runs, Schedules, Secrets, Templates und Audit.",
  scopeNote = "Diese Route bleibt erreichbar, ist aber bewusst nicht Teil der primären Demo-Navigation.",
}: PlaceholderProductPageProps) {
  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, gap: 1.5 }}>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Chip color="info" label="Post-MVP" size="small" variant="outlined" />
      </Stack>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack sx={{ gap: 1.5 }}>
          <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
            <InfoOutlinedIcon color="info" fontSize="small" />
            <Typography component="h2" variant="h6">
              Noch nicht implementiert
            </Typography>
          </Stack>
          <Typography color="text.secondary">{description}</Typography>
          <Divider />
          <Typography sx={{ fontWeight: 700 }}>MVP-Grenze</Typography>
          <Typography color="text.secondary" variant="body2">
            {currentBoundary}
          </Typography>
          <Typography color="text.secondary" variant="body2">
            {scopeNote}
          </Typography>
          <Button href="/dashboard" sx={{ alignSelf: "flex-start", mt: 0.5 }} variant="outlined">
            Zurück zum Dashboard
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
}
