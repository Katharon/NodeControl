import { Alert, Chip, Paper, Stack, Typography } from "@mui/material";

type PlaceholderProductPageProps = {
  title: string;
  description: string;
};

export function PlaceholderProductPage({ title, description }: PlaceholderProductPageProps) {
  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, gap: 1.5 }}>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Chip color="info" label="Geplant" size="small" />
      </Stack>
      <Paper sx={{ p: 3 }}>
        <Stack sx={{ gap: 2 }}>
          <Alert severity="info">Noch nicht implementiert.</Alert>
          <Typography color="text.secondary">{description}</Typography>
        </Stack>
      </Paper>
    </Stack>
  );
}
