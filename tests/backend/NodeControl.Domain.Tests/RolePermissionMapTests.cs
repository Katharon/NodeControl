using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;

namespace NodeControl.Domain.Tests;

public sealed class RolePermissionMapTests
{
    [Fact]
    public void Owner_has_all_permissions()
    {
        foreach (var permission in Enum.GetValues<Permission>())
        {
            Assert.True(RolePermissionMap.HasPermission(CustomerRole.Owner, permission));
        }
    }

    [Fact]
    public void Admin_does_not_have_manage_memberships()
    {
        Assert.False(RolePermissionMap.HasPermission(CustomerRole.Admin, Permission.ManageMemberships));
    }

    [Fact]
    public void Operator_can_run_jobs_but_cannot_manage_memberships()
    {
        Assert.True(RolePermissionMap.HasPermission(CustomerRole.Operator, Permission.RunJobs));
        Assert.False(RolePermissionMap.HasPermission(CustomerRole.Operator, Permission.ManageMemberships));
    }

    [Fact]
    public void Viewer_cannot_manage_customer()
    {
        Assert.False(RolePermissionMap.HasPermission(CustomerRole.Viewer, Permission.ManageCustomer));
    }

    [Fact]
    public void Viewer_can_view_templates_but_cannot_manage_templates()
    {
        Assert.True(RolePermissionMap.HasPermission(CustomerRole.Viewer, Permission.ViewTemplates));
        Assert.False(RolePermissionMap.HasPermission(CustomerRole.Viewer, Permission.ManageTemplates));
    }

    [Fact]
    public void Auditor_can_view_audit_logs_but_cannot_manage_customer()
    {
        Assert.True(RolePermissionMap.HasPermission(CustomerRole.Auditor, Permission.ViewAuditLogs));
        Assert.False(RolePermissionMap.HasPermission(CustomerRole.Auditor, Permission.ManageCustomer));
    }
}
