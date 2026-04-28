import { DashboardOverview } from "@/components/dashboard/DashboardOverview";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

export default function DashboardPage() {
  return (
    <AppPage maxWidth="md">
      <DashboardOverview />
    </AppPage>
  );
}
