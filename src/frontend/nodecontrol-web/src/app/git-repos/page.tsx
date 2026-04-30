import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export default function GitReposPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="git-repos"
        description="Manage customer-scoped Git repository sources for one-time artifact imports."
        title="Git-Repos"
      />
    </AppPage>
  );
}
