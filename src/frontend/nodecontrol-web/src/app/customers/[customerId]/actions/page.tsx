import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { JobList } from "@/components/jobs/JobList";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type ActionsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function ActionsPage({ params }: ActionsPageProps) {
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
        <JobList customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
