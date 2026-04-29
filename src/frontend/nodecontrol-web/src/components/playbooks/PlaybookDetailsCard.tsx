"use client";

import CheckIcon from "@mui/icons-material/Check";
import { Alert, Button, Chip, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { PlaybookForm } from "@/components/playbooks/PlaybookForm";
import { PlaybookValidationResult } from "@/components/playbooks/PlaybookValidationResult";
import {
  getPlaybook,
  type PlaybookValidationResult as Result,
  updatePlaybook,
  validatePlaybook,
} from "@/lib/api/playbooks";

type PlaybookDetailsCardProps = {
  customerId: string;
  playbookId: string;
  canManagePlaybooks: boolean;
};

export function PlaybookDetailsCard({ customerId, playbookId, canManagePlaybooks }: PlaybookDetailsCardProps) {
  const queryClient = useQueryClient();
  const [validationResult, setValidationResult] = useState<Result | null>(null);
  const playbookQuery = useQuery({ queryKey: ["playbook", customerId, playbookId], queryFn: () => getPlaybook(customerId, playbookId) });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updatePlaybook>[2]) => updatePlaybook(customerId, playbookId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["playbook", customerId, playbookId] });
      await queryClient.invalidateQueries({ queryKey: ["playbooks", customerId] });
    },
  });
  const validateMutation = useMutation({
    mutationFn: () => validatePlaybook(customerId, playbookId),
    onSuccess: setValidationResult,
  });

  if (playbookQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (playbookQuery.isError) {
    return <Alert severity="error">This playbook could not be loaded.</Alert>;
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack>
            <Typography component="h1" variant="h4">{playbookQuery.data.name}</Typography>
            <Typography color="text.secondary">{playbookQuery.data.slug}</Typography>
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1, mt: 1 }}>
              <Chip label={playbookQuery.data.sourceType === "ArtifactDirectory" ? "Artifact directory" : "Inline YAML"} size="small" variant="outlined" />
              <Chip label={`Entry: ${playbookQuery.data.entryFilePath ?? "site.yml"}`} size="small" variant="outlined" />
              {playbookQuery.data.sourceType === "ArtifactDirectory" ? (
                <Chip label={`${playbookQuery.data.artifactFiles.length} files`} size="small" variant="outlined" />
              ) : null}
            </Stack>
          </Stack>
          <Button disabled={validateMutation.isPending} onClick={() => validateMutation.mutate()} startIcon={<CheckIcon />} variant="outlined">
            Validate
          </Button>
        </Stack>
        <PlaybookValidationResult result={validationResult} />
        {canManagePlaybooks ? (
          <PlaybookForm
            playbook={playbookQuery.data}
            onSubmit={async (input) => { await updateMutation.mutateAsync(input); }}
            submitLabel="Save playbook"
          />
        ) : null}
      </Stack>
    </Paper>
  );
}
