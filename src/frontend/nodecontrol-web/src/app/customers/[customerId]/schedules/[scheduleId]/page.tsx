import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Box, Button, Container, Stack } from "@mui/material";
import { JobScheduleDetailsCard } from "@/components/schedules/JobScheduleDetailsCard";
import { AppProviders } from "@/lib/app/AppProviders";

export const dynamic = "force-dynamic";

type ScheduleDetailsPageProps = {
  params: Promise<{
    customerId: string;
    scheduleId: string;
  }>;
};

export default async function ScheduleDetailsPage({ params }: ScheduleDetailsPageProps) {
  const { customerId, scheduleId } = await params;

  return (
    <AppProviders>
      <Box component="main" sx={{ minHeight: "100vh", py: 6 }}>
        <Container maxWidth="lg">
          <Stack sx={{ gap: 2 }}>
            <Button
              href={`/customers/${customerId}/schedules`}
              startIcon={<ArrowBackIcon />}
              sx={{ alignSelf: "flex-start" }}
              variant="text"
            >
              Schedules
            </Button>
            <JobScheduleDetailsCard customerId={customerId} scheduleId={scheduleId} />
          </Stack>
        </Container>
      </Box>
    </AppProviders>
  );
}
