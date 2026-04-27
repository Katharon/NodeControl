import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type TemplateType = "GenericText" | "Jinja2" | "AnsibleVars" | "ShellScript" | "ConfigFile";
export type TemplateStatus = "Active" | "Archived";

export type Template = {
  id: string;
  customerId: string;
  name: string;
  slug: string;
  description: string | null;
  templateType: TemplateType;
  content: string;
  language: string | null;
  status: TemplateStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type TemplateInput = {
  name: string;
  slug: string;
  description?: string | null;
  templateType: TemplateType;
  content: string;
  language?: string | null;
};

export type TemplateValidationResult = {
  isValid: boolean;
  errors: string[];
  warnings: string[];
};

export function getTemplates(customerId: string) {
  return apiGet<Template[]>(`/api/v1/customers/${customerId}/templates`);
}

export function getTemplate(customerId: string, templateId: string) {
  return apiGet<Template>(`/api/v1/customers/${customerId}/templates/${templateId}`);
}

export function createTemplate(customerId: string, input: TemplateInput) {
  return apiPost<TemplateInput, Template>(`/api/v1/customers/${customerId}/templates`, input);
}

export function updateTemplate(customerId: string, templateId: string, input: TemplateInput) {
  return apiPut<TemplateInput, Template>(`/api/v1/customers/${customerId}/templates/${templateId}`, input);
}

export function archiveTemplate(customerId: string, templateId: string) {
  return apiDelete<Template>(`/api/v1/customers/${customerId}/templates/${templateId}`);
}

export function validateTemplate(customerId: string, input: Pick<TemplateInput, "templateType" | "content" | "language">) {
  return apiPost<Pick<TemplateInput, "templateType" | "content" | "language">, TemplateValidationResult>(
    `/api/v1/customers/${customerId}/templates/validate`,
    input,
  );
}

export function validateStoredTemplate(customerId: string, templateId: string) {
  return apiPost<Record<string, never>, TemplateValidationResult>(
    `/api/v1/customers/${customerId}/templates/${templateId}/validate`,
    {},
  );
}
