import { AppPage } from "@/components/layout/AppPage";
import { RunWizard } from "@/components/runWizard/RunWizard";

export const dynamic = "force-dynamic";

export default function RunWizardPage() {
  return (
    <AppPage maxWidth="xl">
      <RunWizard />
    </AppPage>
  );
}
