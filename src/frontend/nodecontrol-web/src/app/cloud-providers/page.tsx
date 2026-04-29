import { AppPage } from "@/components/layout/AppPage";
import { PlaceholderProductPage } from "@/components/layout/PlaceholderProductPage";

export default function CloudProvidersPage() {
  return (
    <AppPage>
      <PlaceholderProductPage
        description="Cloud-Provider sind als spätere Inventarquellen denkbar. Im MVP werden Hosts und Inventare bewusst direkt in NodeControl gepflegt."
        title="Cloud-Provider"
      />
    </AppPage>
  );
}
