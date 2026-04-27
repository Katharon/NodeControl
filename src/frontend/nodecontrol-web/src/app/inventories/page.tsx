import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function InventoriesPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="inventories"
        description="Wähle einen Kunden, um dessen Inventare und Vorschau zu öffnen."
        title="Inventare"
      />
    </AppPage>
  );
}
