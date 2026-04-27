import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function UsersPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Benutzerverwaltung und Einladungen folgen später. Aktuell entstehen Benutzer über die OIDC-Anmeldung."
        title="Benutzer"
      />
    </AppPage>
  );
}
