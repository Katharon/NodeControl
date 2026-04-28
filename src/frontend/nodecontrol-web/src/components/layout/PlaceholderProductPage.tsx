import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { Chip, Paper, Stack, Typography } from "@mui/material";

type PlaceholderProductPageProps = {
  title: string;
  description: string;
  scopeNote?: string;
};

export function PlaceholderProductPage({
  title,
  description,
  scopeNote = "Diese Oberfläche ist bewusst als Roadmap-Bereich markiert und führt in der Demo nicht in einen halbfertigen Workflow.",
}: PlaceholderProductPageProps) {
  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, gap: 1.5 }}>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Chip color="info" label="Geplante Erweiterung" size="small" variant="outlined" />
      </Stack>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack sx={{ gap: 1.5 }}>
          <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
            <InfoOutlinedIcon color="info" fontSize="small" />
            <Typography component="h2" variant="h6">
              Für einen späteren Slice vorgesehen
            </Typography>
          </Stack>
          <Typography color="text.secondary">{description}</Typography>
          <Typography color="text.secondary" variant="body2">
            {scopeNote}
          </Typography>
        </Stack>
      </Paper>
    </Stack>
  );
}
