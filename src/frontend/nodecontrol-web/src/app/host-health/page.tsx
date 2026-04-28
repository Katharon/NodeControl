import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function HostHealthPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="host-health"
        description="Wähle einen Kunden, um den Hostzustand und Verbindungstests zu öffnen."
        title="Hostzustand"
      />
    </AppPage>
  );
}
