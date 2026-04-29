"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import BookIcon from "@mui/icons-material/Book";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Alert,
  Button,
  Chip,
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
import { PlaybookForm } from "@/components/playbooks/PlaybookForm";
import { archivePlaybook, createPlaybook, getPlaybooks } from "@/lib/api/playbooks";

type PlaybookListProps = {
  customerId: string;
  canManagePlaybooks: boolean;
};

export function PlaybookList({ customerId, canManagePlaybooks }: PlaybookListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const playbooksQuery = useQuery({ queryKey: ["playbooks", customerId], queryFn: () => getPlaybooks(customerId) });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createPlaybook>[1]) => createPlaybook(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["playbooks", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (playbookId: string) => archivePlaybook(customerId, playbookId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["playbooks", customerId] }),
  });

  if (playbooksQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (playbooksQuery.isError) {
    return <Alert severity="error">Playbooks could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">
          Playbooks
        </Typography>
        {canManagePlaybooks ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            New playbook
          </Button>
        ) : null}
      </Stack>

      {playbooksQuery.data.length === 0 ? (
        <Alert severity="info">No playbooks are defined.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {playbooksQuery.data.map((playbook) => (
              <Stack direction={{ xs: "column", sm: "row" }} key={playbook.id} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <BookIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{playbook.name}</Typography>
                    <Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
                      <Typography color="text.secondary" variant="body2">{playbook.slug}</Typography>
                      <Chip label={playbookTypeLabel(playbook.sourceType)} size="small" variant="outlined" />
                      <Chip label={playbook.entryFilePath ?? "site.yml"} size="small" variant="outlined" />
                    </Stack>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ gap: 1 }}>
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/playbooks/${playbook.id}`} variant="outlined">Öffnen</Button>
                  {canManagePlaybooks ? (
                    <Button color="warning" disabled={archiveMutation.isPending} onClick={() => archiveMutation.mutate(playbook.id)} startIcon={<ArchiveIcon />} variant="outlined">
                      Archive
                    </Button>
                  ) : null}
                </Stack>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}

      <Dialog fullWidth maxWidth="md" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Create playbook</DialogTitle>
        <DialogContent>
          <PlaybookForm onSubmit={async (input) => { await createMutation.mutateAsync(input); }} submitLabel="Create playbook" />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}

function playbookTypeLabel(sourceType: string) {
  return sourceType === "ArtifactDirectory" ? "Artifact directory" : "Inline YAML";
}
