import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/apiClient";

export type GitRepositoryStatus = "Active" | "Archived";

export type GitRepository = {
  id: string;
  customerId: string;
  name: string;
  repositoryUrl: string;
  branch: string | null;
  revision: string | null;
  subpath: string | null;
  status: GitRepositoryStatus;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
};

export type GitRepositoryInput = {
  name: string;
  repositoryUrl: string;
  branch?: string | null;
  revision?: string | null;
  subpath?: string | null;
};

export function getGitRepositories(customerId: string) {
  return apiGet<GitRepository[]>(`/api/v1/customers/${customerId}/git-repositories`);
}

export function createGitRepository(customerId: string, input: GitRepositoryInput) {
  return apiPost<GitRepositoryInput, GitRepository>(`/api/v1/customers/${customerId}/git-repositories`, input);
}

export function updateGitRepository(customerId: string, gitRepositoryId: string, input: GitRepositoryInput) {
  return apiPut<GitRepositoryInput, GitRepository>(
    `/api/v1/customers/${customerId}/git-repositories/${gitRepositoryId}`,
    input,
  );
}

export function archiveGitRepository(customerId: string, gitRepositoryId: string) {
  return apiDelete<GitRepository>(`/api/v1/customers/${customerId}/git-repositories/${gitRepositoryId}`);
}
