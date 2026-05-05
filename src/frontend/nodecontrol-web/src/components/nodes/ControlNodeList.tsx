"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import DnsIcon from "@mui/icons-material/Dns";
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
import { ControlNodeForm } from "@/components/nodes/ControlNodeForm";
import {
  archiveControlNode,
  type ControlNode,
  createControlNode,
  getControlNodes,
  updateControlNode,
} from "@/lib/api/controlNodes";
import { getSecrets } from "@/lib/api/secrets";

type ControlNodeListProps = {
  customerId: string;
  canManageNodes: boolean;
  canViewSecrets?: boolean;
  showCreateButton?: boolean;
};

export function ControlNodeList({ customerId, canManageNodes, canViewSecrets = false, showCreateButton = true }: ControlNodeListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editingControlNode, setEditingControlNode] = useState<ControlNode | null>(null);
  const controlNodesQuery = useQuery({
    queryKey: ["control-nodes", customerId],
    queryFn: () => getControlNodes(customerId),
  });
  const secretsQuery = useQuery({
    enabled: canManageNodes && canViewSecrets,
    queryKey: ["secrets", customerId],
    queryFn: () => getSecrets(customerId),
  });
  const sshPrivateKeySecrets = (secretsQuery.data ?? []).filter((secret) => secret.kind === "SshPrivateKey" && secret.status === "Active");
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createControlNode>[1]) =>
      createControlNode(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["control-nodes", customerId] });
      setCreateOpen(false);
    },
  });
  const updateMutation = useMutation({
    mutationFn: ({ controlNodeId, input }: { controlNodeId: string; input: Parameters<typeof updateControlNode>[2] }) =>
      updateControlNode(customerId, controlNodeId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["control-nodes", customerId] });
      setEditingControlNode(null);
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
    return <Alert severity="error">Control Hosts konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h2" variant="h5">
          Control Hosts
        </Typography>
        {canManageNodes && showCreateButton ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            Neuer Control Host
          </Button>
        ) : null}
      </Stack>

      {controlNodesQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Control Hosts definiert.</Alert>
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
                      Hostname: {controlNode.hostname}:{controlNode.sshPort}
                    </Typography>
                    <Typography color="text.secondary" variant="body2">
                      Typ: Control Host · Plattform: Ansible Control · Status: {controlNode.status}
                    </Typography>
                    <Typography color="text.secondary" variant="body2">
                      Dispatch: {dispatchLabel(controlNode)}
                    </Typography>
                  </Stack>
                </Stack>
                {canManageNodes ? (
                  <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
                    <ConnectionCheckButton
                      customerId={customerId}
                      targetId={controlNode.id}
                      targetType="ControlNode"
                    />
                    <Button
                      disabled={updateMutation.isPending}
                      onClick={() => setEditingControlNode(controlNode)}
                      startIcon={<EditIcon />}
                      variant="outlined"
                    >
                      Configure
                    </Button>
                    <Button
                      color="warning"
                      disabled={archiveMutation.isPending}
                      onClick={() => archiveMutation.mutate(controlNode.id)}
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
        <DialogTitle>Neuer Control Host</DialogTitle>
        <DialogContent>
          <ControlNodeForm
            canValidateSecretReferences={canViewSecrets}
            onSubmit={async (input) => {
              await createMutation.mutateAsync(input);
            }}
            sshPrivateKeySecrets={sshPrivateKeySecrets}
            submitLabel="Control Host anlegen"
          />
        </DialogContent>
      </Dialog>

      <Dialog fullWidth maxWidth="sm" onClose={() => setEditingControlNode(null)} open={Boolean(editingControlNode)}>
        <DialogTitle>Control Host konfigurieren</DialogTitle>
        <DialogContent>
          {editingControlNode ? (
            <ControlNodeForm
              canValidateSecretReferences={canViewSecrets}
              controlNode={editingControlNode}
              onSubmit={async (input) => {
                await updateMutation.mutateAsync({ controlNodeId: editingControlNode.id, input });
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

function dispatchLabel(controlNode: ControlNode) {
  if (isLocalControlHost(controlNode.hostname)) {
    return "Local/dev fallback";
  }

  if (controlNode.sshUsername && controlNode.sshPrivateKeySecretId && controlNode.remoteWorkspaceRoot) {
    return `SSH remote as ${controlNode.sshUsername}`;
  }

  return "Remote SSH not configured";
}

function isLocalControlHost(hostname: string) {
  const normalized = hostname.trim().toLowerCase();
  return normalized === "localhost" || normalized === "127.0.0.1" || normalized === "::1";
}
