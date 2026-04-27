import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type SecretKind = "Generic" | "Password" | "ApiToken" | "SshPrivateKey" | "Certificate" | "ConnectionString";
export type SecretStatus = "Active" | "Archived";

export type Secret = {
  id: string;
  customerId: string;
  name: string;
  slug: string;
  description: string | null;
  kind: SecretKind;
  status: SecretStatus;
  hasValue: boolean;
  lastRotatedAtUtc: string | null;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type CreateSecretInput = {
  name: string;
  slug: string;
  description?: string | null;
  kind: SecretKind;
  value: string;
};

export type UpdateSecretInput = {
  name: string;
  slug: string;
  description?: string | null;
  kind: SecretKind;
};

export type RotateSecretInput = {
  value: string;
};

export type SecretReference = {
  slug: string;
  found: boolean;
  secretId: string | null;
  status: SecretStatus | null;
};

export type SecretReferenceValidationResult = {
  isValid: boolean;
  references: SecretReference[];
  errors: string[];
  warnings: string[];
};

export function getSecrets(customerId: string) {
  return apiGet<Secret[]>(`/api/v1/customers/${customerId}/secrets`);
}

export function getSecret(customerId: string, secretId: string) {
  return apiGet<Secret>(`/api/v1/customers/${customerId}/secrets/${secretId}`);
}

export function createSecret(customerId: string, input: CreateSecretInput) {
  return apiPost<CreateSecretInput, Secret>(`/api/v1/customers/${customerId}/secrets`, input);
}

export function updateSecret(customerId: string, secretId: string, input: UpdateSecretInput) {
  return apiPut<UpdateSecretInput, Secret>(`/api/v1/customers/${customerId}/secrets/${secretId}`, input);
}

export function rotateSecret(customerId: string, secretId: string, input: RotateSecretInput) {
  return apiPost<RotateSecretInput, Secret>(`/api/v1/customers/${customerId}/secrets/${secretId}/rotate`, input);
}

export function archiveSecret(customerId: string, secretId: string) {
  return apiDelete<Secret>(`/api/v1/customers/${customerId}/secrets/${secretId}`);
}

export function validateSecretReferences(customerId: string, content: string) {
  return apiPost<{ content: string }, SecretReferenceValidationResult>(
    `/api/v1/customers/${customerId}/secret-references/validate`,
    { content },
  );
}
