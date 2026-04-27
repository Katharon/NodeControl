import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function AuditPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="audit"
        description="Wähle einen Kunden, um dessen Audit Trail zu öffnen."
        title="Audit"
      />
    </AppPage>
  );
}
