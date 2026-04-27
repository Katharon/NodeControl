import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { MembershipList } from "@/components/memberships/MembershipList";

export const dynamic = "force-dynamic";

type MembershipsPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function MembershipsPage({ params }: MembershipsPageProps) {
  const { customerId } = await params;

  return (
    <AppPage maxWidth="md">
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Kunde
        </Button>
        <MembershipList customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
