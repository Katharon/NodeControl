import { Container, Stack } from "@mui/material";
import type { ContainerProps } from "@mui/material";
import type { ReactNode } from "react";
import { ProductShell } from "@/components/layout/ProductShell";
import { AppProviders } from "@/lib/app/AppProviders";

type AppPageProps = {
  children: ReactNode;
  maxWidth?: ContainerProps["maxWidth"];
};

export function AppPage({ children, maxWidth = "lg" }: AppPageProps) {
  return (
    <AppProviders>
      <ProductShell>
        <Container maxWidth={maxWidth} sx={{ py: 4 }}>
          <Stack sx={{ gap: 2 }}>{children}</Stack>
        </Container>
      </ProductShell>
    </AppProviders>
  );
}
