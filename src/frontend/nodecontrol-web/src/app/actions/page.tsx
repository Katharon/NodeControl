import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function ActionsPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="actions"
        description="Wähle einen Kunden, um dessen wiederverwendbare Actions zu öffnen."
        title="Actions"
      />
    </AppPage>
  );
}
