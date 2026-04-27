import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function PlaybooksPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="playbooks"
        description="Wähle einen Kunden, um dessen Playbooks zu öffnen."
        title="Playbooks"
      />
    </AppPage>
  );
}
