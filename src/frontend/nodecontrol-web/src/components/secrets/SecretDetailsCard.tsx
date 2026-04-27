"use client";

import ArchiveIcon from "@mui/icons-material/Archive";
import AutorenewIcon from "@mui/icons-material/Autorenew";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import { Alert, Box, Button, CircularProgress, Divider, Paper, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { RotateSecretDialog } from "@/components/secrets/RotateSecretDialog";
import { SecretForm } from "@/components/secrets/SecretForm";
import { SecretKindChip } from "@/components/secrets/SecretKindChip";
import { SecretStatusChip } from "@/components/secrets/SecretStatusChip";
import { archiveSecret, getSecret, rotateSecret, updateSecret } from "@/lib/api/secrets";

type SecretDetailsCardProps = {
  customerId: string;
  secretId: string;
  canManageSecrets: boolean;
};

export function SecretDetailsCard({ customerId, secretId, canManageSecrets }: SecretDetailsCardProps) {
  const queryClient = useQueryClient();
  const [rotateOpen, setRotateOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const secretQuery = useQuery({ queryKey: ["secret", customerId, secretId], queryFn: () => getSecret(customerId, secretId) });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateSecret>[2]) => updateSecret(customerId, secretId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["secret", customerId, secretId] });
      await queryClient.invalidateQueries({ queryKey: ["secrets", customerId] });
    },
  });
  const rotateMutation = useMutation({
    mutationFn: (value: string) => rotateSecret(customerId, secretId, { value }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["secret", customerId, secretId] });
      await queryClient.invalidateQueries({ queryKey: ["secrets", customerId] });
    },
  });
  const archiveMutation = useMutation({
    mutationFn: () => archiveSecret(customerId, secretId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["secret", customerId, secretId] });
      await queryClient.invalidateQueries({ queryKey: ["secrets", customerId] });
    },
  });

  if (secretQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (secretQuery.isError) {
    return <Alert severity="error">This secret could not be loaded.</Alert>;
  }

  const secret = secretQuery.data;
  const reference = `secret://${secret.slug}`;

  async function copyReference() {
    await navigator.clipboard.writeText(reference);
    setCopied(true);
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack sx={{ gap: 1 }}>
            <Typography component="h1" variant="h4">{secret.name}</Typography>
            <Typography color="text.secondary">{secret.slug}</Typography>
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
              <SecretKindChip kind={secret.kind} />
              <SecretStatusChip status={secret.status} />
              <Typography color="text.secondary" variant="body2">
                {secret.hasValue ? "Value stored" : "No value"}
              </Typography>
            </Stack>
          </Stack>
          {canManageSecrets && secret.status === "Active" ? (
            <Stack direction={{ xs: "column", sm: "row" }} sx={{ alignItems: { sm: "center" }, gap: 1 }}>
              <Button onClick={() => setRotateOpen(true)} startIcon={<AutorenewIcon />} variant="outlined">
                Rotate
              </Button>
              <Button color="warning" disabled={archiveMutation.isPending} onClick={() => archiveMutation.mutate()} startIcon={<ArchiveIcon />} variant="outlined">
                Archive
              </Button>
            </Stack>
          ) : null}
        </Stack>

        {secret.description ? <Typography>{secret.description}</Typography> : null}

        <Stack direction={{ xs: "column", md: "row" }} sx={{ gap: 2 }}>
          <Box>
            <Typography color="text.secondary" variant="body2">Last rotated</Typography>
            <Typography>{secret.lastRotatedAtUtc ? new Date(secret.lastRotatedAtUtc).toLocaleString() : "n/a"}</Typography>
          </Box>
          <Box>
            <Typography color="text.secondary" variant="body2">Created</Typography>
            <Typography>{new Date(secret.createdAt).toLocaleString()}</Typography>
          </Box>
          <Box>
            <Typography color="text.secondary" variant="body2">Updated</Typography>
            <Typography>{secret.updatedAt ? new Date(secret.updatedAt).toLocaleString() : "n/a"}</Typography>
          </Box>
          <Box>
            <Typography color="text.secondary" variant="body2">Archived</Typography>
            <Typography>{secret.archivedAt ? new Date(secret.archivedAt).toLocaleString() : "n/a"}</Typography>
          </Box>
        </Stack>

        <Stack sx={{ gap: 1 }}>
          <Typography sx={{ fontWeight: 700 }}>Reference</Typography>
          <Stack direction={{ xs: "column", sm: "row" }} sx={{ gap: 1 }}>
            <TextField
              fullWidth
              slotProps={{ input: { readOnly: true, sx: { fontFamily: "monospace" } } }}
              value={reference}
            />
            <Button onClick={copyReference} startIcon={<ContentCopyIcon />} variant="outlined">
              {copied ? "Copied" : "Copy"}
            </Button>
          </Stack>
        </Stack>

        <Divider />

        {canManageSecrets && secret.status === "Active" ? (
          <SecretForm
            secret={secret}
            onSubmit={async (input) => { await updateMutation.mutateAsync(input as Parameters<typeof updateSecret>[2]); }}
            requireValue={false}
            submitLabel="Save Metadata"
          />
        ) : null}

        <RotateSecretDialog
          onClose={() => setRotateOpen(false)}
          onRotate={async (value) => { await rotateMutation.mutateAsync(value); }}
          open={rotateOpen}
        />
      </Stack>
    </Paper>
  );
}
