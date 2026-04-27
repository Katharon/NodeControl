import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { AppPage } from "@/components/layout/AppPage";
import { CustomerSecretDetailsSection } from "@/components/secrets/CustomerSecretDetailsSection";

export const dynamic = "force-dynamic";

type SecretDetailsPageProps = {
  params: Promise<{
    customerId: string;
    secretId: string;
  }>;
};

export default async function SecretDetailsPage({ params }: SecretDetailsPageProps) {
  const { customerId, secretId } = await params;

  return (
    <AppPage>
      <Stack sx={{ gap: 2 }}>
        <Button
          href={`/customers/${customerId}/secrets`}
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start" }}
          variant="text"
        >
          Secrets
        </Button>
        <CustomerSecretDetailsSection customerId={customerId} secretId={secretId} />
      </Stack>
    </AppPage>
  );
}
