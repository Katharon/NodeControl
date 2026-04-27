import { Alert, Stack, Typography } from "@mui/material";

type PlaceholderProductPageProps = {
  title: string;
};

export function PlaceholderProductPage({ title }: PlaceholderProductPageProps) {
  return (
    <Stack sx={{ gap: 2 }}>
      <Typography component="h1" variant="h4">
        {title}
      </Typography>
      <Alert severity="info">Noch nicht implementiert.</Alert>
    </Stack>
  );
}
