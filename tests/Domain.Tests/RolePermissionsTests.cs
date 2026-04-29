using CarshiTow.Domain.Authorization;
using CarshiTow.Domain.Enums;

namespace Domain.Tests;

public sealed class RolePermissionsTests
{
    [Fact]
    public void TowYardStaff_has_staff_pack_and_upload_permissions()
    {
        var p = RolePermissions.ForRole(UserRole.TowYardStaff);
        Assert.Contains(Permissions.UploadsCreate, p);
        Assert.Contains(Permissions.PacksCreate, p);
        Assert.Contains(Permissions.PacksViewAssigned, p);
        Assert.DoesNotContain(Permissions.PayoutsManage, p);
    }

    [Fact]
    public void TowYardAdmin_has_pricing_payouts_and_yard_wide_pack_view()
    {
        var p = RolePermissions.ForRole(UserRole.TowYardAdmin);
        Assert.Contains(Permissions.PacksSetPrice, p);
        Assert.Contains(Permissions.PayoutsManage, p);
        Assert.Contains(Permissions.PacksViewYard, p);
    }

    [Fact]
    public void Insurer_has_insurer_flow_permissions_only()
    {
        var p = RolePermissions.ForRole(UserRole.Insurer);
        Assert.Contains(Permissions.PreviewsWatermarked, p);
        Assert.Contains(Permissions.PacksPay, p);
        Assert.Contains(Permissions.PacksDownloadUnlocked, p);
        Assert.Equal(3, p.Count);
    }

    [Fact]
    public void CrashifySupport_can_view_transactions_and_search_users_but_not_refunds()
    {
        var p = RolePermissions.ForRole(UserRole.CrashifySupport);
        Assert.Contains(Permissions.PlatformTransactionsView, p);
        Assert.Contains(Permissions.PlatformUsersSearch, p);
        Assert.DoesNotContain(Permissions.RefundsProcess, p);
    }

    [Fact]
    public void CrashifyAdmin_includes_every_declared_permission()
    {
        var p = RolePermissions.ForRole(UserRole.CrashifyAdmin);
        foreach (var perm in Permissions.All)
        {
            Assert.Contains(perm, p);
        }
    }
}
