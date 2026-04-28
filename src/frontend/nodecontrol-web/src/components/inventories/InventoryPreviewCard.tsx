import { Alert, Paper, Stack, Typography } from "@mui/material";
import type { InventoryPreview } from "@/lib/api/inventoryGroups";

type InventoryPreviewCardProps = {
  preview: InventoryPreview | null;
  hasError: boolean;
};

export function InventoryPreviewCard({ preview, hasError }: InventoryPreviewCardProps) {
  if (hasError) {
    return <Alert severity="error">Inventarvorschau konnte nicht geladen werden.</Alert>;
  }

  if (!preview) {
    return <Alert severity="info">Noch keine Inventarvorschau geladen.</Alert>;
  }

  const content = preview.content.trim();

  if (!content) {
    return <Alert severity="info">Diese Inventarvorschau enthält noch keine Hosts.</Alert>;
  }

  return (
    <Paper variant="outlined" sx={{ bgcolor: "background.paper", color: "text.primary", p: 2 }}>
      <Stack sx={{ gap: 1 }}>
        <Typography color="text.secondary" variant="body2">
          Format: {preview.format}
        </Typography>
        <Typography
          component="pre"
          sx={{
            bgcolor: "background.default",
            border: 1,
            borderColor: "divider",
            borderRadius: 1,
            color: "text.primary",
            fontFamily: "monospace",
            fontSize: 14,
            m: 0,
            overflow: "auto",
            p: 2,
            whiteSpace: "pre",
          }}
        >
          {content}
        </Typography>
      </Stack>
    </Paper>
  );
}
