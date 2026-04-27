import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function NotificationsPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Benachrichtigungen werden später über Run-Ergebnisse, Fehler und wichtige Systemereignisse informieren."
        title="Benachrichtigungen"
      />
    </AppPage>
  );
}
