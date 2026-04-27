import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function SecretsPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="secrets"
        description="Wähle einen Kunden, um dessen Secrets zu öffnen."
        title="Secrets"
      />
    </AppPage>
  );
}
