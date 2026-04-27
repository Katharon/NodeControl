"use client";

import { Alert, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getInventoryPreview } from "@/lib/api/inventoryGroups";

type InventoryPreviewCardProps = {
  customerId: string;
  inventoryGroupId: string;
};

export function InventoryPreviewCard({ customerId, inventoryGroupId }: InventoryPreviewCardProps) {
  const previewQuery = useQuery({
    queryKey: ["inventory-preview", customerId, inventoryGroupId],
    queryFn: () => getInventoryPreview(customerId, inventoryGroupId),
  });

  if (previewQuery.isPending) {
    return (
      <Paper variant="outlined" sx={{ bgcolor: "background.paper", p: 2 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography color="text.secondary">Inventarvorschau wird geladen.</Typography>
        </Stack>
      </Paper>
    );
  }

  if (previewQuery.isError) {
    return <Alert severity="error">Inventarvorschau konnte nicht geladen werden.</Alert>;
  }

  const content = previewQuery.data.content.trim();

  if (!content) {
    return <Alert severity="info">Diese Inventarvorschau enthält noch keine Hosts.</Alert>;
  }

  return (
    <Paper variant="outlined" sx={{ bgcolor: "background.paper", color: "text.primary", p: 2 }}>
      <Stack sx={{ gap: 1 }}>
        <Typography color="text.secondary" variant="body2">
          {previewQuery.data.format}
        </Typography>
        <Typography
          component="pre"
          sx={{
            bgcolor: "action.hover",
            borderRadius: 1,
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
