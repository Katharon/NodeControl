import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export const customerRoles = [
  "Owner",
  "Admin",
  "Operator",
  "Viewer",
  "Auditor",
] as const;

export type CustomerRole = (typeof customerRoles)[number];

export type CustomerMembership = {
  id: string;
  customerId: string;
  userId: string;
  userDisplayName: string;
  userEmail: string;
  role: CustomerRole;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  deactivatedAt: string | null;
};

export type CreateCustomerMembershipInput = {
  userId: string;
  role: CustomerRole;
};

export type UpdateCustomerMembershipInput = {
  role: CustomerRole;
  isActive: boolean;
};

export function getCustomerMemberships(customerId: string) {
  return apiGet<CustomerMembership[]>(`/api/v1/customers/${customerId}/memberships`);
}

export function createCustomerMembership(
  customerId: string,
  input: CreateCustomerMembershipInput,
) {
  return apiPost<CreateCustomerMembershipInput, CustomerMembership>(
    `/api/v1/customers/${customerId}/memberships`,
    input,
  );
}

export function updateCustomerMembership(
  customerId: string,
  membershipId: string,
  input: UpdateCustomerMembershipInput,
) {
  return apiPut<UpdateCustomerMembershipInput, CustomerMembership>(
    `/api/v1/customers/${customerId}/memberships/${membershipId}`,
    input,
  );
}

export function deactivateCustomerMembership(customerId: string, membershipId: string) {
  return apiDelete<CustomerMembership>(
    `/api/v1/customers/${customerId}/memberships/${membershipId}`,
  );
}
