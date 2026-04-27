import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function RunsPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="runs"
        description="Wähle einen Kunden, um dessen Runs zu öffnen."
        title="Runs"
      />
    </AppPage>
  );
}
