"use client";

import AddIcon from "@mui/icons-material/Add";
import LinkOffIcon from "@mui/icons-material/LinkOff";
import {
  Alert,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { InventoryPreviewCard } from "@/components/inventories/InventoryPreviewCard";
import { getManagedNodes } from "@/lib/api/managedNodes";
import {
  addManagedNodeToInventoryGroup,
  type InventoryGroup,
  removeManagedNodeFromInventoryGroup,
} from "@/lib/api/inventoryGroups";

type InventoryGroupDetailsProps = {
  customerId: string;
  inventoryGroup: InventoryGroup;
  canManageNodes: boolean;
};

export function InventoryGroupDetails({
  customerId,
  inventoryGroup,
  canManageNodes,
}: InventoryGroupDetailsProps) {
  const queryClient = useQueryClient();
  const [managedNodeId, setManagedNodeId] = useState("");
  const managedNodesQuery = useQuery({
    queryKey: ["managed-nodes", customerId],
    queryFn: () => getManagedNodes(customerId),
  });
  const addMutation = useMutation({
    mutationFn: () => addManagedNodeToInventoryGroup(customerId, inventoryGroup.id, managedNodeId),
    onSuccess: async () => {
      setManagedNodeId("");
      await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
      await queryClient.invalidateQueries({
        queryKey: ["inventory-preview", customerId, inventoryGroup.id],
      });
    },
  });
  const removeMutation = useMutation({
    mutationFn: (nodeId: string) =>
      removeManagedNodeFromInventoryGroup(customerId, inventoryGroup.id, nodeId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
      await queryClient.invalidateQueries({
        queryKey: ["inventory-preview", customerId, inventoryGroup.id],
      });
    },
  });

  if (managedNodesQuery.isError) {
    return <Alert severity="error">Hosts konnten für dieses Inventar nicht geladen werden.</Alert>;
  }

  const managedNodes = managedNodesQuery.data ?? [];
  const linkedNodes = managedNodes.filter((node) => inventoryGroup.managedNodeIds.includes(node.id));
  const unlinkedNodes = managedNodes.filter((node) => !inventoryGroup.managedNodeIds.includes(node.id));

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 2 }}>
        <Typography component="h3" variant="h6">
          {inventoryGroup.name}
        </Typography>
        {inventoryGroup.description ? <Typography>{inventoryGroup.description}</Typography> : null}

        {canManageNodes ? (
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1.5 }}>
            <FormControl fullWidth size="small">
              <InputLabel id={`${inventoryGroup.id}-node-label`}>Host</InputLabel>
              <Select
                label="Host"
                labelId={`${inventoryGroup.id}-node-label`}
                onChange={(event) => setManagedNodeId(event.target.value)}
                value={managedNodeId}
              >
                {unlinkedNodes.map((node) => (
                  <MenuItem key={node.id} value={node.id}>
                    {node.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              disabled={!managedNodeId || addMutation.isPending}
              onClick={() => addMutation.mutate()}
              startIcon={<AddIcon />}
              variant="contained"
            >
              Add
            </Button>
          </Stack>
        ) : null}

        <Stack sx={{ gap: 1 }}>
          {linkedNodes.length === 0 ? (
            <Alert severity="info">Diesem Inventar sind noch keine Hosts zugeordnet.</Alert>
          ) : (
            linkedNodes.map((node) => (
              <Stack
                direction="row"
                key={node.id}
                sx={{ alignItems: "center", justifyContent: "space-between", gap: 2 }}
              >
                <Typography>{node.name}</Typography>
                {canManageNodes ? (
                  <Button
                    color="warning"
                    disabled={removeMutation.isPending}
                    onClick={() => removeMutation.mutate(node.id)}
                    startIcon={<LinkOffIcon />}
                    variant="outlined"
                  >
                    Remove
                  </Button>
                ) : null}
              </Stack>
            ))
          )}
        </Stack>

        <InventoryPreviewCard customerId={customerId} inventoryGroupId={inventoryGroup.id} />
      </Stack>
    </Paper>
  );
}
