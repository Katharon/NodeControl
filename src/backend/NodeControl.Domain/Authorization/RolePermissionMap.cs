using NodeControl.Domain.Customers;

namespace NodeControl.Domain.Authorization;

public static class RolePermissionMap
{
    private static readonly IReadOnlySet<Permission> OwnerPermissions =
        Enum.GetValues<Permission>().ToHashSet();

    private static readonly IReadOnlySet<Permission> AdminPermissions =
        OwnerPermissions
            .Where(permission => permission != Permission.ManageMemberships)
            .ToHashSet();

    private static readonly IReadOnlySet<Permission> OperatorPermissions = new HashSet<Permission>
    {
        Permission.ViewCustomer,
        Permission.ViewNodes,
        Permission.ViewPlaybooks,
        Permission.RunJobs,
        Permission.ViewJobRuns,
        Permission.ViewSchedules
    };

    private static readonly IReadOnlySet<Permission> ViewerPermissions = new HashSet<Permission>
    {
        Permission.ViewCustomer,
        Permission.ViewNodes,
        Permission.ViewPlaybooks,
        Permission.ViewJobRuns,
        Permission.ViewSchedules
    };

    private static readonly IReadOnlySet<Permission> AuditorPermissions = new HashSet<Permission>
    {
        Permission.ViewCustomer,
        Permission.ViewJobRuns,
        Permission.ViewAuditLogs
    };

    public static bool HasPermission(CustomerRole role, Permission permission)
    {
        return GetPermissions(role).Contains(permission);
    }

    public static IReadOnlyCollection<Permission> GetPermissions(CustomerRole role)
    {
        return role switch
        {
            CustomerRole.Owner => OwnerPermissions.ToArray(),
            CustomerRole.Admin => AdminPermissions.ToArray(),
            CustomerRole.Operator => OperatorPermissions.ToArray(),
            CustomerRole.Viewer => ViewerPermissions.ToArray(),
            CustomerRole.Auditor => AuditorPermissions.ToArray(),
            _ => []
        };
    }
}
