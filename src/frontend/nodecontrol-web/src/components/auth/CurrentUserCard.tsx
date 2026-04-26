"use client";

import LogoutIcon from "@mui/icons-material/Logout";
import PersonIcon from "@mui/icons-material/Person";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/apiClient";
import { getCurrentUser } from "@/lib/auth/currentUser";

export function CurrentUserCard() {
  const currentUserQuery = useQuery({
    queryKey: ["current-user"],
    queryFn: getCurrentUser,
  });

  if (currentUserQuery.isPending) {
    return (
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 2 }}>
          <CircularProgress size={22} />
          <Typography>Loading current user</Typography>
        </Stack>
      </Paper>
    );
  }

  if (currentUserQuery.isError) {
    const isUnauthorized =
      currentUserQuery.error instanceof ApiError &&
      currentUserQuery.error.status === 401;

    return (
      <Alert
        action={
          isUnauthorized ? (
            <Button color="inherit" href="/auth/login" size="small">
              Sign in
            </Button>
          ) : undefined
        }
        severity={isUnauthorized ? "info" : "error"}
      >
        {isUnauthorized
          ? "You are not signed in."
          : "The current user could not be loaded."}
      </Alert>
    );
  }

  const currentUser = currentUserQuery.data;

  return (
    <Paper sx={{ p: 3 }}>
      <Stack sx={{ gap: 2 }}>
        <Stack direction="row" sx={{ alignItems: "center", gap: 1.5 }}>
          <PersonIcon color="primary" />
          <Box>
            <Typography component="h1" variant="h5">
              {currentUser.displayName}
            </Typography>
            <Typography color="text.secondary">{currentUser.email}</Typography>
          </Box>
        </Stack>

        <Divider />

        <Stack sx={{ gap: 1 }}>
          <Typography>
            Provider: <strong>{currentUser.authProvider}</strong>
          </Typography>
          <Typography>
            Subject: <strong>{currentUser.externalSubject}</strong>
          </Typography>
          <Typography>
            Platform admin:{" "}
            <strong>{currentUser.isPlatformAdmin ? "Yes" : "No"}</strong>
          </Typography>
        </Stack>

        <Box component="form" action="/auth/logout" method="post">
          <Button startIcon={<LogoutIcon />} type="submit" variant="outlined">
            Sign out
          </Button>
        </Box>
      </Stack>
    </Paper>
  );
}
