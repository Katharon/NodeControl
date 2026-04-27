import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { JobRunDetailsCard } from "@/components/jobRuns/JobRunDetailsCard";
import { AppProviders } from "@/lib/app/AppProviders";

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
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}/job-runs`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Job Runs
            </Button>
            <JobRunDetailsCard customerId={customerId} jobRunId={jobRunId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
