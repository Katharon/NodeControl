"use client";

import { Alert, List, ListItem, ListItemText } from "@mui/material";
import type { VariableSetValidationResult as Result } from "@/lib/api/variableSets";

type VariableSetValidationResultProps = {
  result: Result | null;
};

export function VariableSetValidationResult({ result }: VariableSetValidationResultProps) {
  if (!result) {
    return null;
  }

  return (
    <Alert severity={result.isValid ? "success" : "error"}>
      {result.message}
      {result.errors.length > 0 ? (
        <List dense>
          {result.errors.map((error) => (
            <ListItem key={error} sx={{ py: 0 }}>
              <ListItemText primary={error} />
            </ListItem>
          ))}
        </List>
      ) : null}
    </Alert>
  );
}
