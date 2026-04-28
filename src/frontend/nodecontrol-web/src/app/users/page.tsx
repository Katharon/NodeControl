import { AppPage } from "@/components/layout/AppPage";
import { UserList } from "@/components/users/UserList";

export const dynamic = "force-dynamic";

export default function UsersPage() {
  return (
    <AppPage maxWidth="lg">
      <UserList />
    </AppPage>
  );
}
