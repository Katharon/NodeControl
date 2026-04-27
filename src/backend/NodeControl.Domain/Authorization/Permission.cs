namespace NodeControl.Domain.Authorization;

public enum Permission
{
    ViewCustomer = 1,
    ManageCustomer = 2,
    ManageMemberships = 3,
    ViewNodes = 4,
    ManageNodes = 5,
    ViewPlaybooks = 6,
    ManagePlaybooks = 7,
    RunJobs = 8,
    ViewJobRuns = 9,
    CancelJobRuns = 10,
    RetryJobRuns = 11,
    ViewSchedules = 12,
    ManageSchedules = 13,
    ViewAuditLogs = 14,
    ViewTemplates = 15,
    ManageTemplates = 16
}
