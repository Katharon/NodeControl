"use client";

import { CssBaseline, ThemeProvider } from "@mui/material";
import { type ReactNode } from "react";
import { QueryProvider } from "@/lib/query/QueryProvider";
import { muiTheme } from "@/theme/mui-theme";

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider theme={muiTheme}>
      <CssBaseline />
      <QueryProvider>{children}</QueryProvider>
    </ThemeProvider>
  );
}
