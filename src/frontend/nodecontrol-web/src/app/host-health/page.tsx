import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function HostHealthPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Hier entsteht eine Übersicht für Erreichbarkeit, letzte Ausführungssignale und Betriebszustand von Hosts."
        title="Hostzustand"
      />
    </AppPage>
  );
}
