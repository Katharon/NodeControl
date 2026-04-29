import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function NotificationsPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Benachrichtigungen bleiben Post-MVP. Aktuell sind Run Center, Logs und Audit die primären Nachvollziehbarkeitsflächen."
        title="Benachrichtigungen"
      />
    </AppPage>
  );
}
