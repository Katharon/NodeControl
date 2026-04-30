import { AppPage } from "@/components/layout/AppPage";
import { ProductRoutePicker } from "@/components/layout/ProductRoutePicker";

export default function ImportPage() {
  return (
    <AppPage>
      <ProductRoutePicker
        customerPath="import"
        description="Import public GitHub files once into managed Playbook and Template artifacts."
        title="Import"
      />
    </AppPage>
  );
}
