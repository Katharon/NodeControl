import { CustomerList } from "@/components/customers/CustomerList";
import { AppPage } from "@/components/layout/AppPage";

export const dynamic = "force-dynamic";

export default function CustomersPage() {
  return (
    <AppPage maxWidth="md">
      <CustomerList />
    </AppPage>
  );
}
