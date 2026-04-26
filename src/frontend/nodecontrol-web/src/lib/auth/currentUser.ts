import { apiGet } from "@/lib/api/apiClient";

export type CurrentUser = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  isPlatformAdmin: boolean;
  authProvider: string;
  externalSubject: string;
};

export function getCurrentUser() {
  return apiGet<CurrentUser>("/api/v1/me");
}
