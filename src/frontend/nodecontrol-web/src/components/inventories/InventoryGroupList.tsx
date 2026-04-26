"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import InventoryIcon from "@mui/icons-material/Inventory";
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { InventoryGroupDetails } from "@/components/inventories/InventoryGroupDetails";
import { InventoryGroupForm } from "@/components/inventories/InventoryGroupForm";
import {
  archiveInventoryGroup,
  createInventoryGroup,
  getInventoryGroups,
} from "@/lib/api/inventoryGroups";

type InventoryGroupListProps = {
  customerId: string;
  canManageNodes: boolean;
};

export function InventoryGroupList({ customerId, canManageNodes }: InventoryGroupListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const groupsQuery = useQuery({
    queryKey: ["inventory-groups", customerId],
    queryFn: () => getInventoryGroups(customerId),
  });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createInventoryGroup>[1]) =>
      createInventoryGroup(customerId, input),
    onSuccess: async (group) => {
      await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
      setSelectedGroupId(group.id);
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (inventoryGroupId: string) => archiveInventoryGroup(customerId, inventoryGroupId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
    },
  });

  if (groupsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (groupsQuery.isError) {
    return <Alert severity="error">Inventory groups could not be loaded.</Alert>;
  }

  const selectedGroup =
    groupsQuery.data.find((group) => group.id === selectedGroupId) ?? groupsQuery.data[0];

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h2" variant="h5">
          Inventory Groups
        </Typography>
        {canManageNodes ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            New group
          </Button>
        ) : null}
      </Stack>

      {groupsQuery.data.length === 0 ? (
        <Alert severity="info">No inventory groups are defined.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {groupsQuery.data.map((group) => (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                key={group.id}
                sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}
              >
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <InventoryIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{group.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {group.managedNodeIds.length} managed nodes
                    </Typography>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ gap: 1 }}>
                  <Button onClick={() => setSelectedGroupId(group.id)} variant="outlined">
                    Preview
                  </Button>
                  {canManageNodes ? (
                    <Button
                      color="warning"
                      disabled={archiveMutation.isPending}
                      onClick={() => archiveMutation.mutate(group.id)}
                      startIcon={<ArchiveIcon />}
                      variant="outlined"
                    >
                      Archive
                    </Button>
                  ) : null}
                </Stack>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      {selectedGroup ? (
        <InventoryGroupDetails
          canManageNodes={canManageNodes}
          customerId={customerId}
          inventoryGroup={selectedGroup}
        />
      ) : null}

      <Dialog fullWidth maxWidth="sm" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Create inventory group</DialogTitle>
        <DialogContent>
          <InventoryGroupForm
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
            submitLabel="Create group"
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
