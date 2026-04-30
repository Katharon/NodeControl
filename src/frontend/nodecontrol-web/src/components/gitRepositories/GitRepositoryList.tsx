"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import GitHubIcon from "@mui/icons-material/GitHub";
import { Alert, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle, Divider, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { GitRepositoryForm } from "@/components/gitRepositories/GitRepositoryForm";
import {
  archiveGitRepository,
  createGitRepository,
  getGitRepositories,
  updateGitRepository,
  type GitRepository,
} from "@/lib/api/gitRepositories";

type GitRepositoryListProps = {
  customerId: string;
  canManageGitRepositories: boolean;
};

export function GitRepositoryList({ customerId, canManageGitRepositories }: GitRepositoryListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editingRepository, setEditingRepository] = useState<GitRepository | null>(null);
  const repositoriesQuery = useQuery({ queryKey: ["git-repositories", customerId], queryFn: () => getGitRepositories(customerId) });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createGitRepository>[1]) => createGitRepository(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["git-repositories", customerId] });
      setCreateOpen(false);
    },
  });
  const updateMutation = useMutation({
    mutationFn: ({ repositoryId, input }: { repositoryId: string; input: Parameters<typeof updateGitRepository>[2] }) =>
      updateGitRepository(customerId, repositoryId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["git-repositories", customerId] });
      setEditingRepository(null);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (repositoryId: string) => archiveGitRepository(customerId, repositoryId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["git-repositories", customerId] }),
  });

  if (repositoriesQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (repositoriesQuery.isError) {
    return <Alert severity="error">Git repositories could not be loaded.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
        <Stack>
          <Typography component="h1" variant="h4">Git-Repos</Typography>
          <Typography color="text.secondary">Customer-scoped sources for one-time artifact imports.</Typography>
        </Stack>
        {canManageGitRepositories ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">
            New Git repo
          </Button>
        ) : null}
      </Stack>

      {repositoriesQuery.data.length === 0 ? (
        <Alert severity="info">No Git repositories defined yet.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {repositoriesQuery.data.map((repository) => (
              <Stack direction={{ xs: "column", md: "row" }} key={repository.id} sx={{ justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "flex-start", gap: 1.5 }}>
                  <GitHubIcon color="primary" />
                  <Stack sx={{ gap: 0.75 }}>
                    <Typography sx={{ fontWeight: 700 }}>{repository.name}</Typography>
                    <Typography color="text.secondary" sx={{ overflowWrap: "anywhere" }} variant="body2">
                      {repository.repositoryUrl}
                    </Typography>
                    <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
                      <Chip label={`Branch: ${repository.branch ?? "n/a"}`} size="small" variant="outlined" />
                      <Chip label={`Revision: ${repository.revision ?? "n/a"}`} size="small" variant="outlined" />
                      <Chip label={`Subpath: ${repository.subpath ?? "/"}`} size="small" variant="outlined" />
                    </Stack>
                  </Stack>
                </Stack>
                {canManageGitRepositories ? (
                  <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
                    <Button onClick={() => setEditingRepository(repository)} variant="outlined">Edit</Button>
                    <Button
                      color="warning"
                      disabled={archiveMutation.isPending}
                      onClick={() => archiveMutation.mutate(repository.id)}
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

      <Dialog fullWidth maxWidth="md" onClose={() => setCreateOpen(false)} open={createOpen}>
        <DialogTitle>Create Git repo</DialogTitle>
        <DialogContent>
          <GitRepositoryForm onSubmit={async (input) => { await createMutation.mutateAsync(input); }} submitLabel="Create Git repo" />
        </DialogContent>
      </Dialog>

      <Dialog fullWidth maxWidth="md" onClose={() => setEditingRepository(null)} open={Boolean(editingRepository)}>
        <DialogTitle>Edit Git repo</DialogTitle>
        <DialogContent>
          {editingRepository ? (
            <GitRepositoryForm
              repository={editingRepository}
              onSubmit={async (input) => {
                await updateMutation.mutateAsync({ repositoryId: editingRepository.id, input });
              }}
              submitLabel="Save Git repo"
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
