import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function SystemPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="System zeigt später technische Zustände, Versionen und Betriebsinformationen der NodeControl-Installation."
        title="System"
      />
    </AppPage>
  );
}
