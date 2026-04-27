import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobDetailsCard } from "@/components/jobs/JobDetailsCard";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type ActionDetailsPageProps = {
  params: Promise<{
    customerId: string;
    actionId: string;
  }>;
};

export default async function ActionDetailsPage({ params }: ActionDetailsPageProps) {
  const { customerId, actionId } = await params;

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
        <JobDetailsCard customerId={customerId} jobId={actionId} />
      </Stack>
    </AppPage>
  );
}
