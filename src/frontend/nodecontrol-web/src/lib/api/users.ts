import { apiGet } from "@/lib/api/apiClient";

export type ExternalIdentitySummary = {
  provider: string;
  subject: string;
};

export type User = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  isPlatformAdmin: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  externalIdentities: ExternalIdentitySummary[];
};

type GetUsersOptions = {
  query?: string;
  includeInactive?: boolean;
  limit?: number;
};

export function getUsers(options: GetUsersOptions = {}) {
  const searchParams = new URLSearchParams();
  if (options.query?.trim()) {
    searchParams.set("q", options.query.trim());
  }

  if (options.includeInactive) {
    searchParams.set("includeInactive", "true");
  }

  if (options.limit) {
    searchParams.set("limit", String(options.limit));
  }

  const queryString = searchParams.toString();
  return apiGet<User[]>(`/api/v1/users${queryString ? `?${queryString}` : ""}`);
}
