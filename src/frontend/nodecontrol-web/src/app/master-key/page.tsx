import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function MasterKeyPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Master-Key-Verwaltung ist eine spätere Betriebs- und Wiederherstellungsfläche. Im MVP bleiben Secrets auf geschützte Werte, Rotation und sichere Referenzen begrenzt."
        title="Master-Key"
      />
    </AppPage>
  );
}
