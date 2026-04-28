"use client";

import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { type ReactNode } from "react";
import { QueryProvider } from "@/lib/query/QueryProvider";
import { muiTheme } from "@/theme/mui-theme";

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <AppRouterCacheProvider>
      <ThemeProvider theme={muiTheme}>
        <CssBaseline />
        <QueryProvider>{children}</QueryProvider>
      </ThemeProvider>
    </AppRouterCacheProvider>
  );
}
