import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function GitReposPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Git-Repos gehören zu späteren playbook- und artifact-basierten Workflows. Der MVP verwendet bewusst Inline-Playbooks."
        title="Git-Repos"
      />
    </AppPage>
  );
}
