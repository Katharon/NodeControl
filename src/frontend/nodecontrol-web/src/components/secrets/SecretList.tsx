"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { SecretForm } from "@/components/secrets/SecretForm";
import { SecretKindChip } from "@/components/secrets/SecretKindChip";
import { SecretStatusChip } from "@/components/secrets/SecretStatusChip";
import { archiveSecret, createSecret, getSecrets } from "@/lib/api/secrets";

type SecretListProps = {
  customerId: string;
  canManageSecrets: boolean;
};

export function SecretList({ customerId, canManageSecrets }: SecretListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const secretsQuery = useQuery({ queryKey: ["secrets", customerId], queryFn: () => getSecrets(customerId) });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createSecret>[1]) => createSecret(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["secrets", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (secretId: string) => archiveSecret(customerId, secretId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["secrets", customerId] }),
  });

  if (secretsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (secretsQuery.isError) {
    return <Alert severity="error">Secrets could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">Secrets</Typography>
        {canManageSecrets ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">New Secret</Button>
        ) : null}
      </Stack>
      {secretsQuery.data.length === 0 ? (
        <Alert severity="info">No secrets defined yet.</Alert>
      ) : (
        <Paper sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Slug</TableCell>
                <TableCell>Kind</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Last rotated</TableCell>
                <TableCell>Updated</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {secretsQuery.data.map((secret) => (
                <TableRow key={secret.id}>
                  <TableCell sx={{ fontWeight: 700 }}>{secret.name}</TableCell>
                  <TableCell>{secret.slug}</TableCell>
                  <TableCell><SecretKindChip kind={secret.kind} /></TableCell>
                  <TableCell><SecretStatusChip status={secret.status} /></TableCell>
                  <TableCell>{secret.lastRotatedAtUtc ? new Date(secret.lastRotatedAtUtc).toLocaleString() : "n/a"}</TableCell>
                  <TableCell>{new Date(secret.updatedAt ?? secret.createdAt).toLocaleString()}</TableCell>
                  <TableCell align="right">
                    <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1 }}>
                      <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/secrets/${secret.id}`} size="small" variant="outlined">
                        Open
                      </Button>
                      {canManageSecrets ? (
                        <Button
                          color="warning"
                          disabled={archiveMutation.isPending}
                          onClick={() => archiveMutation.mutate(secret.id)}
                          size="small"
                          startIcon={<ArchiveIcon />}
                          variant="outlined"
                        >
                          Archive
                        </Button>
                      ) : null}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
      <Dialog fullWidth maxWidth="md" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>New Secret</DialogTitle>
        <DialogContent>
          <Stack sx={{ pt: 1 }}>
            <SecretForm
              onSubmit={async (input) => { await createMutation.mutateAsync(input as Parameters<typeof createSecret>[1]); }}
              requireValue
              submitLabel="Create Secret"
            />
          </Stack>
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
