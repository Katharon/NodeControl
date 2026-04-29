import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function SystemPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="System zeigt später technische Zustände, Versionen und Betriebsinformationen. Der aktuelle Demo-Betrieb bleibt über Scripts, Logs und Dokumentation nachvollziehbar."
        title="System"
      />
    </AppPage>
  );
}
