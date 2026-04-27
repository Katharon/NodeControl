import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobScheduleList } from "@/components/schedules/JobScheduleList";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type SchedulesPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function SchedulesPage({ params }: SchedulesPageProps) {
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
        <JobScheduleList customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
