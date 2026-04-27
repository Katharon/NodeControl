import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export const dynamic = "force-dynamic";

export default function SchedulesPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="schedules"
        description="Wähle einen Kunden, um dessen Schedules zu öffnen."
        title="Schedules"
      />
    </AppPage>
  );
}
