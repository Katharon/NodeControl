"use client";

import { Alert, List, ListItem, ListItemText } from "@mui/material";
import type { PlaybookValidationResult as Result } from "@/lib/api/playbooks";

type PlaybookValidationResultProps = {
  result: Result | null;
};

export function PlaybookValidationResult({ result }: PlaybookValidationResultProps) {
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
