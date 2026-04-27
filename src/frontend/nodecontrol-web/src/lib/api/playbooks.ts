import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type PlaybookSourceType = "InlineYaml" | "ArtifactDirectory" | "GitRepository";
export type PlaybookStatus = "Active" | "Archived";

export type Playbook = {
  id: string;
  customerId: string;
  name: string;
  slug: string;
  description: string | null;
  sourceType: PlaybookSourceType;
  status: PlaybookStatus;
  inlineContent: string | null;
  entryFilePath: string | null;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type PlaybookInput = {
  name: string;
  slug: string;
  description?: string | null;
  sourceType: PlaybookSourceType;
  inlineContent?: string | null;
  entryFilePath?: string | null;
};

export type PlaybookValidationResult = {
  isValid: boolean;
  message: string;
  errors: string[];
};

export function getPlaybooks(customerId: string) {
  return apiGet<Playbook[]>(`/api/v1/customers/${customerId}/playbooks`);
}

export function getPlaybook(customerId: string, playbookId: string) {
  return apiGet<Playbook>(`/api/v1/customers/${customerId}/playbooks/${playbookId}`);
}

export function createPlaybook(customerId: string, input: PlaybookInput) {
  return apiPost<PlaybookInput, Playbook>(`/api/v1/customers/${customerId}/playbooks`, input);
}

export function updatePlaybook(customerId: string, playbookId: string, input: PlaybookInput) {
  return apiPut<PlaybookInput, Playbook>(
    `/api/v1/customers/${customerId}/playbooks/${playbookId}`,
    input,
  );
}

export function archivePlaybook(customerId: string, playbookId: string) {
  return apiDelete<Playbook>(`/api/v1/customers/${customerId}/playbooks/${playbookId}`);
}

export function validatePlaybook(customerId: string, playbookId: string) {
  return apiPost<Record<string, never>, PlaybookValidationResult>(
    `/api/v1/customers/${customerId}/playbooks/${playbookId}/validate`,
    {},
  );
}
