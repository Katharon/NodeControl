import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function ImportPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Import bleibt Post-MVP. Der MVP hält die Datenanlage bewusst explizit, kundenbezogen und auditierbar."
        title="Import"
      />
    </AppPage>
  );
}
