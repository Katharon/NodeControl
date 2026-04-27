import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobRunDetailsCard } from "@/components/jobRuns/JobRunDetailsCard";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type JobRunDetailsPageProps = {
  params: Promise<{
    customerId: string;
    jobRunId: string;
  }>;
};

export default async function JobRunDetailsPage({ params }: JobRunDetailsPageProps) {
  const { customerId, jobRunId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/runs`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Runs
        </Button>
        <JobRunDetailsCard customerId={customerId} jobRunId={jobRunId} />
      </Stack>
    </AppPage>
  );
}
