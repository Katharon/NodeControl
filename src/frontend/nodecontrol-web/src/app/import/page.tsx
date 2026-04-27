import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function ImportPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Import wird später bestehende Hosts, Inventare und Automationsdefinitionen kontrolliert übernehmen."
        title="Import"
      />
    </AppPage>
  );
}
