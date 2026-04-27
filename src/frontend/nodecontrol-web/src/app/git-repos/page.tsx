import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function GitReposPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Git-Repos sind für spätere playbook- und artifact-basierte Workflows vorgesehen."
        title="Git-Repos"
      />
    </AppPage>
  );
}
