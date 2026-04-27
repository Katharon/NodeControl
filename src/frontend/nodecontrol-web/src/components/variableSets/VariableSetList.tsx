"use client";

import AddIcon from "@mui/icons-material/Add";
import ArchiveIcon from "@mui/icons-material/Archive";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import StorageIcon from "@mui/icons-material/Storage";
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
import { VariableSetForm } from "@/components/variableSets/VariableSetForm";
import { archiveVariableSet, createVariableSet, getVariableSets } from "@/lib/api/variableSets";

type VariableSetListProps = {
  customerId: string;
  canManagePlaybooks: boolean;
};

export function VariableSetList({ customerId, canManagePlaybooks }: VariableSetListProps) {
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const variableSetsQuery = useQuery({ queryKey: ["variable-sets", customerId], queryFn: () => getVariableSets(customerId) });
  const createMutation = useMutation({
    mutationFn: (input: Parameters<typeof createVariableSet>[1]) => createVariableSet(customerId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["variable-sets", customerId] });
      setCreateOpen(false);
    },
  });
  const archiveMutation = useMutation({
    mutationFn: (variableSetId: string) => archiveVariableSet(customerId, variableSetId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["variable-sets", customerId] }),
  });

  if (variableSetsQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (variableSetsQuery.isError) {
    return <Alert severity="error">Variablen konnten nicht geladen werden.</Alert>;
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 2 }}>
        <Typography component="h1" variant="h4">Variablen</Typography>
        {canManagePlaybooks ? (
          <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)} variant="contained">Neue Variablen</Button>
        ) : null}
      </Stack>
      {variableSetsQuery.data.length === 0 ? (
        <Alert severity="info">Noch keine Variablen definiert.</Alert>
      ) : (
        <Paper>
          <Stack divider={<Divider />}>
            {variableSetsQuery.data.map((variableSet) => (
              <Stack direction={{ xs: "column", sm: "row" }} key={variableSet.id} sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2, p: 2 }}>
                <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
                  <StorageIcon color="primary" />
                  <Stack>
                    <Typography sx={{ fontWeight: 700 }}>{variableSet.name}</Typography>
                    <Typography color="text.secondary" variant="body2">{variableSet.slug} · {variableSet.format}</Typography>
                  </Stack>
                </Stack>
                <Stack direction="row" sx={{ gap: 1 }}>
                  <Button endIcon={<OpenInNewIcon />} href={`/customers/${customerId}/variables/${variableSet.id}`} variant="outlined">Öffnen</Button>
                  {canManagePlaybooks ? (
                    <Button color="warning" disabled={archiveMutation.isPending} onClick={() => archiveMutation.mutate(variableSet.id)} startIcon={<ArchiveIcon />} variant="outlined">
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
        <DialogTitle>Neue Variablen</DialogTitle>
        <DialogContent>
          <VariableSetForm onSubmit={async (input) => { await createMutation.mutateAsync(input); }} submitLabel="Variablen anlegen" />
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
