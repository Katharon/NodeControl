import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { JobDetailsCard } from "@/components/jobs/JobDetailsCard";
import { AppProviders } from "@/lib/app/AppProviders";

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
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}/jobs`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Jobs
            </Button>
            <JobDetailsCard customerId={customerId} jobId={jobId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
