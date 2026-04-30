import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerGitArtifactImportSection } from "@/components/imports/CustomerGitArtifactImportSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type ImportPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function ImportPage({ params }: ImportPageProps) {
  const { customerId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunde
        </Button>
        <CustomerGitArtifactImportSection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
