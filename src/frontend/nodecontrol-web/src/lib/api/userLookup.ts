import { apiGet } from "@/lib/api/apiClient";

export type UserLookupResult = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  isPlatformAdmin: boolean;
};

export function searchUsersForCustomerMembership(customerId: string, query: string) {
  const searchParams = new URLSearchParams();
  if (query.trim().length > 0) {
    searchParams.set("query", query.trim());
  }

  searchParams.set("limit", "20");

  const queryString = searchParams.toString();
  return apiGet<UserLookupResult[]>(
    `/api/v1/customers/${customerId}/membership-candidates${queryString ? `?${queryString}` : ""}`,
  );
}
