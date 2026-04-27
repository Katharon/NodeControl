import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function VariablesPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="variables"
        description="Wähle einen Kunden, um dessen Variablen zu öffnen."
        title="Variablen"
      />
    </AppPage>
  );
}
