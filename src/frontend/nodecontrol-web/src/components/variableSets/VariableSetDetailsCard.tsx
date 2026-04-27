"use client";

import CheckIcon from "@mui/icons-material/Check";
import { Alert, Button, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { VariableSetForm } from "@/components/variableSets/VariableSetForm";
import { VariableSetValidationResult } from "@/components/variableSets/VariableSetValidationResult";
import {
  getVariableSet,
  type VariableSetValidationResult as Result,
  updateVariableSet,
  validateVariableSet,
} from "@/lib/api/variableSets";

type VariableSetDetailsCardProps = {
  customerId: string;
  variableSetId: string;
  canManagePlaybooks: boolean;
};

export function VariableSetDetailsCard({ customerId, variableSetId, canManagePlaybooks }: VariableSetDetailsCardProps) {
  const queryClient = useQueryClient();
  const [validationResult, setValidationResult] = useState<Result | null>(null);
  const variableSetQuery = useQuery({ queryKey: ["variable-set", customerId, variableSetId], queryFn: () => getVariableSet(customerId, variableSetId) });
  const updateMutation = useMutation({
    mutationFn: (input: Parameters<typeof updateVariableSet>[2]) => updateVariableSet(customerId, variableSetId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["variable-set", customerId, variableSetId] });
      await queryClient.invalidateQueries({ queryKey: ["variable-sets", customerId] });
    },
  });
  const validateMutation = useMutation({
    mutationFn: () => validateVariableSet(customerId, variableSetId),
    onSuccess: setValidationResult,
  });

  if (variableSetQuery.isPending) {
    return <CircularProgress size={22} />;
  }

  if (variableSetQuery.isError) {
    return <Alert severity="error">This variable set could not be loaded.</Alert>;
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 3 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
          <Stack>
            <Typography component="h1" variant="h4">{variableSetQuery.data.name}</Typography>
            <Typography color="text.secondary">{variableSetQuery.data.slug}</Typography>
          </Stack>
          <Button disabled={validateMutation.isPending} onClick={() => validateMutation.mutate()} startIcon={<CheckIcon />} variant="outlined">
            Validate
          </Button>
        </Stack>
        <VariableSetValidationResult result={validationResult} />
        {canManagePlaybooks ? (
          <VariableSetForm
            variableSet={variableSetQuery.data}
            onSubmit={async (input) => { await updateMutation.mutateAsync(input); }}
            submitLabel="Save variable set"
          />
        ) : null}
      </Stack>
    </Paper>
  );
}
