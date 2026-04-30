"use client";

import CheckIcon from "@mui/icons-material/Check";
import { Alert, Box, Button, Chip, CircularProgress, Divider, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { PlaybookForm } from "@/components/playbooks/PlaybookForm";
import { PlaybookValidationResult } from "@/components/playbooks/PlaybookValidationResult";
import {
  getPlaybook,
  type PlaybookArtifactFile,
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
        {playbookQuery.data.sourceType === "ArtifactDirectory" ? (
          <ArtifactFileList
            entryFilePath={playbookQuery.data.entryFilePath ?? "site.yml"}
            files={playbookQuery.data.artifactFiles}
          />
        ) : null}
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

function ArtifactFileList({ entryFilePath, files }: { entryFilePath: string; files: PlaybookArtifactFile[] }) {
  return (
    <Box sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
      <Stack divider={<Divider />}>
        <Box sx={{ p: 2 }}>
          <Typography sx={{ fontWeight: 700 }}>Artifact files</Typography>
          <Typography color="text.secondary" variant="body2">Entry file: {entryFilePath}</Typography>
        </Box>
        {files.length === 0 ? (
          <Box sx={{ p: 2 }}>
            <Typography color="text.secondary">No artifact files are stored.</Typography>
          </Box>
        ) : (
          files.map((file) => (
            <Stack
              direction={{ xs: "column", sm: "row" }}
              key={file.path}
              sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 1, p: 2 }}
            >
              <Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
                <Typography sx={{ fontFamily: "monospace", fontSize: 14 }}>{file.path}</Typography>
                {file.path === entryFilePath ? <Chip label="Entry" size="small" color="primary" variant="outlined" /> : null}
              </Stack>
              <Typography color="text.secondary" variant="body2">{file.content.length.toLocaleString()} chars</Typography>
            </Stack>
          ))
        )}
      </Stack>
    </Box>
  );
}
