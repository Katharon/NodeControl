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
    return <CircularProgress size={22} />;
  }

  if (previewQuery.isError) {
    return <Alert severity="error">Inventory preview could not be loaded.</Alert>;
  }

  return (
    <Paper variant="outlined" sx={{ bgcolor: "grey.950", color: "grey.100", p: 2 }}>
      <Stack sx={{ gap: 1 }}>
        <Typography color="grey.400" variant="body2">
          {previewQuery.data.format}
        </Typography>
        <Typography
          component="pre"
          sx={{
            fontFamily: "monospace",
            fontSize: 14,
            m: 0,
            overflow: "auto",
            whiteSpace: "pre",
          }}
        >
          {previewQuery.data.content}
        </Typography>
      </Stack>
    </Paper>
  );
}
