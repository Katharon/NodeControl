import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function SecurityPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Sicherheit bündelt später weitergehende Härtung und Auth-Konfiguration. Der MVP setzt bereits auf OIDC-Fähigkeit, interne Berechtigungen und kundenbezogene Prüfungen."
        title="Sicherheit"
      />
    </AppPage>
  );
}
