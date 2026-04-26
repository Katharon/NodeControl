import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";
import type { Permission } from "@/lib/auth/permissions";

export type CustomerStatus = "Active" | "Archived";

export type Customer = {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  status: CustomerStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
  permissions: Permission[];
};

export type CustomerInput = {
  name: string;
  slug: string;
  description?: string | null;
};

export function getCustomers() {
  return apiGet<Customer[]>("/api/v1/customers");
}

export function getMyCustomers() {
  return apiGet<Customer[]>("/api/v1/me/customers");
}

export function getCustomer(customerId: string) {
  return apiGet<Customer>(`/api/v1/customers/${customerId}`);
}

export function createCustomer(input: CustomerInput) {
  return apiPost<CustomerInput, Customer>("/api/v1/customers", input);
}

export function updateCustomer(customerId: string, input: CustomerInput) {
  return apiPut<CustomerInput, Customer>(`/api/v1/customers/${customerId}`, input);
}

export function archiveCustomer(customerId: string) {
  return apiDelete<Customer>(`/api/v1/customers/${customerId}`);
}
