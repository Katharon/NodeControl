"use client";

import SearchIcon from "@mui/icons-material/Search";
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  InputAdornment,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError } from "@/lib/api/apiClient";
import { getUsers } from "@/lib/api/users";

export function UserList() {
  const [query, setQuery] = useState("");
  const usersQuery = useQuery({
    queryKey: ["users", query],
    queryFn: () => getUsers({ query, limit: 50 }),
  });

  if (usersQuery.isError) {
    const forbidden = usersQuery.error instanceof ApiError && usersQuery.error.status === 403;
    return (
      <Alert severity={forbidden ? "warning" : "error"}>
        {forbidden
          ? "Benutzerverwaltung ist nur für Platform Admins verfügbar."
          : "Benutzer konnten nicht geladen werden."}
      </Alert>
    );
  }

  return (
    <Stack sx={{ gap: 2 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{ alignItems: { sm: "center" }, justifyContent: "space-between", gap: 2 }}
      >
        <Box>
          <Typography component="h1" variant="h4">
            Benutzer
          </Typography>
          <Typography color="text.secondary">
            Bekannte Plattformbenutzer aus Fake Auth oder OIDC-Anmeldungen.
          </Typography>
        </Box>
        <TextField
          label="Suche"
          onChange={(event) => setQuery(event.target.value)}
          size="small"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
          sx={{ minWidth: { sm: 280 } }}
          value={query}
        />
      </Stack>

      {usersQuery.isPending ? (
        <Paper sx={{ p: 3 }}>
          <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
            <CircularProgress size={22} />
            <Typography>Benutzer werden geladen</Typography>
          </Stack>
        </Paper>
      ) : usersQuery.data.length === 0 ? (
        <Alert severity="info">Keine Benutzer gefunden.</Alert>
      ) : (
        <Paper sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Aktiv</TableCell>
                <TableCell>Platform Admin</TableCell>
                <TableCell>Letzter Login</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {usersQuery.data.map((user) => (
                <TableRow key={user.id}>
                  <TableCell sx={{ fontWeight: 700 }}>{user.displayName}</TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <Chip
                      color={user.isActive ? "success" : "default"}
                      label={user.isActive ? "Ja" : "Nein"}
                      size="small"
                      variant={user.isActive ? "filled" : "outlined"}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      color={user.isPlatformAdmin ? "primary" : "default"}
                      label={user.isPlatformAdmin ? "Ja" : "Nein"}
                      size="small"
                      variant={user.isPlatformAdmin ? "filled" : "outlined"}
                    />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : "n/a"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
    </Stack>
  );
}
