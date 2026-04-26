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
  | "ManageSchedules"
  | "ViewAuditLogs";

export function hasPermission(
  permissions: readonly Permission[] | undefined,
  permission: Permission,
) {
  return permissions?.includes(permission) ?? false;
}
