"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import DnsIcon from "@mui/icons-material/Dns";
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
import { ControlNodeForm } from "@/components/nodes/ControlNodeForm";
import {
  archiveControlNode,
  createControlNode,
  getControlNodes,
} from "@/lib/api/controlNodes";

type ControlNodeListProps = {
  customerId: string;
  canManageNodes: boolean;
};

export function ControlNodeList({ customerId, canManageNodes }: ControlNodeListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const controlNodesQuery = useQuery({
    queryKey: ["control-nodes", customerId],
    queryFn: () => getControlNodes(customerId),
  });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createControlNode>[1]) =>
      createControlNode(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["control-nodes", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (controlNodeId: string) => archiveControlNode(customerId, controlNodeId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["control-nodes", customerId] });
    },
  });

  if (controlNodesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (controlNodesQuery.isError) {
    return <Alert severity="error">Control nodes could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h2" variant="h5">
          Control Nodes
        </Typography>
        {canManageNodes ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            New control node
          </Button>
        ) : null}
      </Stack>

      {controlNodesQuery.data.length === 0 ? (
        <Alert severity="info">No control nodes are defined.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {controlNodesQuery.data.map((controlNode) => (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                key={controlNode.id}
                sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}
              >
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <DnsIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{controlNode.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {controlNode.hostname}:{controlNode.sshPort}
                    </Typography>
                  </Stack>
                </Stack>
                {canManageNodes ? (
                  <Button
                    color="warning"
                    disabled={archiveMutation.isPending}
                    onClick={() => archiveMutation.mutate(controlNode.id)}
                    startIcon={<ArchiveIcon />}
                    variant="outlined"
                  >
                    Archive
                  </Button>
                ) : null}
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      <Dialog fullWidth maxWidth="sm" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Create control node</DialogTitle>
        <DialogContent>
          <ControlNodeForm
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
            submitLabel="Create control node"
          />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
