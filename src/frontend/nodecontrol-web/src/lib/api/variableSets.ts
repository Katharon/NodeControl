import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type VariableSetFormat = "Yaml" | "Json";
export type VariableSetStatus = "Active" | "Archived";

export type VariableSet = {
  id: string;
  customerId: string;
  name: string;
  slug: string;
  description: string | null;
  format: VariableSetFormat;
  content: string;
  containsSensitiveValues: boolean;
  status: VariableSetStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type VariableSetInput = {
  name: string;
  slug: string;
  description?: string | null;
  format: VariableSetFormat;
  content: string;
  containsSensitiveValues: boolean;
};

export type VariableSetValidationResult = {
  isValid: boolean;
  message: string;
  errors: string[];
};

export function getVariableSets(customerId: string) {
  return apiGet<VariableSet[]>(`/api/v1/customers/${customerId}/variable-sets`);
}

export function getVariableSet(customerId: string, variableSetId: string) {
  return apiGet<VariableSet>(`/api/v1/customers/${customerId}/variable-sets/${variableSetId}`);
}

export function createVariableSet(customerId: string, input: VariableSetInput) {
  return apiPost<VariableSetInput, VariableSet>(
    `/api/v1/customers/${customerId}/variable-sets`,
    input,
  );
}

export function updateVariableSet(customerId: string, variableSetId: string, input: VariableSetInput) {
  return apiPut<VariableSetInput, VariableSet>(
    `/api/v1/customers/${customerId}/variable-sets/${variableSetId}`,
    input,
  );
}

export function archiveVariableSet(customerId: string, variableSetId: string) {
  return apiDelete<VariableSet>(`/api/v1/customers/${customerId}/variable-sets/${variableSetId}`);
}

export function validateVariableSet(customerId: string, variableSetId: string) {
  return apiPost<Record<string, never>, VariableSetValidationResult>(
    `/api/v1/customers/${customerId}/variable-sets/${variableSetId}/validate`,
    {},
  );
}
