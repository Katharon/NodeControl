import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function TemplatesPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="templates"
        description="Wähle einen Kunden, um dessen Templates zu öffnen."
        title="Templates"
      />
    </AppPage>
  );
}
