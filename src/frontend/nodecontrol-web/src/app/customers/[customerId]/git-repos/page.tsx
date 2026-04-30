import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button, Stack } from "@mui/material";
import { CustomerGitRepositoryListSection } from "@/components/gitRepositories/CustomerGitRepositoryListSection";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

type GitRepositoriesPageProps = {
  params: Promise<{
    customerId: string;
  }>;
};

export default async function GitRepositoriesPage({ params }: GitRepositoriesPageProps) {
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
        <CustomerGitRepositoryListSection customerId={customerId} />
      </Stack>
    </AppPage>
  );
}
