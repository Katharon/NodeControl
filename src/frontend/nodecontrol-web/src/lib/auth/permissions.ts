export type Permission =
  | "ViewCustomer"
  | "ManageCustomer"
  | "ManageMemberships"
  | "ViewNodes"
  | "ManageNodes"
  | "ViewPlaybooks"
  | "ManagePlaybooks"
  | "RunJobs"
  | "ViewJobRuns"
  | "CancelJobRuns"
  | "RetryJobRuns"
  | "ViewSchedules"
  | "ManageSchedules"
  | "ViewAuditLogs"
  | "ViewTemplates"
  | "ManageTemplates"
  | "ViewSecrets"
  | "ManageSecrets";

export function hasPermission(
  permissions: readonly Permission[] | undefined,
  permission: Permission,
) {
  return permissions?.includes(permission) ?? false;
}
