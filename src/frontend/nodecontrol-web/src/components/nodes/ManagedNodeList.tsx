"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import ComputerIcon from "@mui/icons-material/Computer";
import EditIcon from "@mui/icons-material/Edit";
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
import { ConnectionCheckButton } from "@/components/hostHealth/ConnectionCheckButton";
import { ManagedNodeForm } from "@/components/nodes/ManagedNodeForm";
import {
  archiveManagedNode,
  type ManagedNode,
  createManagedNode,
  getManagedNodes,
  updateManagedNode,
} from "@/lib/api/managedNodes";
import { getSecrets } from "@/lib/api/secrets";

type ManagedNodeListProps = {
  customerId: string;
  canManageNodes: boolean;
  canViewSecrets?: boolean;
  showCreateButton?: boolean;
};

export function ManagedNodeList({ customerId, canManageNodes, canViewSecrets = false, showCreateButton = true }: ManagedNodeListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editingManagedNode, setEditingManagedNode] = useState<ManagedNode | null>(null);
  const managedNodesQuery = useQuery({
    queryKey: ["managed-nodes", customerId],
    queryFn: () => getManagedNodes(customerId),
  });
  const secretsQuery = useQuery({
    enabled: canManageNodes && canViewSecrets,
    queryKey: ["secrets", customerId],
    queryFn: () => getSecrets(customerId),
  });
  const sshPrivateKeySecrets = (secretsQuery.data ?? []).filter((secret) => secret.kind === "SshPrivateKey" && secret.status === "Active");
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createManagedNode>[1]) =>
      createManagedNode(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["managed-nodes", customerId] });
      setCreateOpen(false);
    },
  });
  const updateMutation = useMutation({
    mutationFn: ({ managedNodeId, input }: { managedNodeId: string; input: Parameters<typeof updateManagedNode>[2] }) =>
      updateManagedNode(customerId, managedNodeId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["managed-nodes", customerId] });
      setEditingManagedNode(null);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (managedNodeId: string) => archiveManagedNode(customerId, managedNodeId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["managed-nodes", customerId] });
      await queryClient.invalidateQueries({ queryKey: ["inventory-groups", customerId] });
    },
  });

  if (managedNodesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (managedNodesQuery.isError) {
    return <Alert severity="error">Hosts konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h2" variant="h5">
          Hosts
        </Typography>
        {canManageNodes && showCreateButton ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            Neuer Host
          </Button>
        ) : null}
      </Stack>

      {managedNodesQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Hosts definiert.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {managedNodesQuery.data.map((managedNode) => (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                key={managedNode.id}
                sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}
              >
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <ComputerIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{managedNode.name}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      Hostname: {managedNode.hostname}:{managedNode.sshPort}
                    </Typography>
                    <Typography color="text.secondary" variant="body2">
                      Typ: Host · Plattform: {managedNode.operatingSystem ?? "Nicht gesetzt"} · Status: {managedNode.status}
                    </Typography>
                    <Typography color="text.secondary" variant="body2">
                      SSH: {sshLabel(managedNode)}
                    </Typography>
                  </Stack>
                </Stack>
                {canManageNodes ? (
                  <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
                    <ConnectionCheckButton
                      customerId={customerId}
                      targetId={managedNode.id}
                      targetType="ManagedNode"
                    />
                    <Button
                      disabled={updateMutation.isPending}
                      onClick={() => setEditingManagedNode(managedNode)}
                      startIcon={<EditIcon />}
                      variant="outlined"
                    >
                      Configure
                    </Button>
                    <Button
                      color="warning"
                      disabled={archiveMutation.isPending}
                      onClick={() => archiveMutation.mutate(managedNode.id)}
                      startIcon={<ArchiveIcon />}
                      variant="outlined"
                    >
                      Archive
                    </Button>
                  </Stack>
                ) : null}
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      <Dialog fullWidth maxWidth="sm" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Neuer Host</DialogTitle>
        <DialogContent>
          <ManagedNodeForm
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
            sshPrivateKeySecrets={sshPrivateKeySecrets}
            submitLabel="Host anlegen"
          />
        </DialogContent>
      </Dialog>

      <Dialog fullWidth maxWidth="sm" onClose={() => setEditingManagedNode(null)} open={Boolean(editingManagedNode)}>
        <DialogTitle>Host konfigurieren</DialogTitle>
        <DialogContent>
          {editingManagedNode ? (
            <ManagedNodeForm
              managedNode={editingManagedNode}
              onSubmit={async (input) => {
                await updateMutation.mutateAsync({ managedNodeId: editingManagedNode.id, input });
              }}
              sshPrivateKeySecrets={sshPrivateKeySecrets}
              submitLabel="Konfiguration speichern"
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </Stack>
  );
}

function sshLabel(managedNode: ManagedNode) {
  const user = managedNode.sshUsername ? `user ${managedNode.sshUsername}` : "default user";
  return managedNode.sshPrivateKeySecretId ? `${user}, key-backed` : `${user}, basic`;
}
