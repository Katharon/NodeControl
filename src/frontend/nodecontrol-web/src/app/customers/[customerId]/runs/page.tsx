import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobRunList } from "@/components/jobRuns/JobRunList";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type RunsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function RunsPage({ params }: RunsPageProps) {
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
        <JobRunList customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
