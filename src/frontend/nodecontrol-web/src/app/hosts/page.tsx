import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function HostsPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="hosts"
        description="Wähle einen Kunden, um dessen Hosts und Control Hosts zu öffnen."
        title="Hosts"
      />
    </AppPage>
  );
}
