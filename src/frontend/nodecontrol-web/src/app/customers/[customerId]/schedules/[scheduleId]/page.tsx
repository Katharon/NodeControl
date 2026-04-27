import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobScheduleDetailsCard } from "@/components/schedules/JobScheduleDetailsCard";
import { AppPage } from "@/components/layout/AppPage";

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
    <AppPage>
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
    </AppPage>
  );
}
