import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobDetailsCard } from "@/components/jobs/JobDetailsCard";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type JobDetailsPageProps = {
  params: Promise<{
    customerId: string;
    jobId: string;
  }>;
};

export default async function JobDetailsPage({ params }: JobDetailsPageProps) {
  const { customerId, jobId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/actions`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Actions
        </Button>
        <JobDetailsCard customerId={customerId} jobId={jobId} />
      </Stack>
    </AppPage>
  );
}
