using CarshiTow.Domain.Enums;

namespace CarshiTow.Domain.Authorization;

/// <summary>Maps <see cref="UserRole"/> to effective Enterprise 2.1 permissions.</summary>
public static class RolePermissions
{
    public static IReadOnlySet<string> ForRole(UserRole role) => role switch
    {
        UserRole.TowYardAdmin => TowYardAdmin,
        UserRole.TowYardStaff => TowYardStaff,
        UserRole.Insurer => Insurer,
        UserRole.CrashifyAdmin => CrashifyAdmin,
        UserRole.CrashifySupport => CrashifySupport,
        _ => Empty
    };

    private static readonly HashSet<string> Empty = [];

    private static readonly HashSet<string> TowYardAdmin =
    [
        Permissions.UploadsCreate,
        Permissions.PacksCreate,
        Permissions.PacksViewYard,
        Permissions.PacksSetPrice,
        Permissions.PayoutsManage,
        Permissions.DashboardView
    ];

    private static readonly HashSet<string> TowYardStaff =
    [
        Permissions.UploadsCreate,
        Permissions.PacksCreate,
        Permissions.PacksViewAssigned
    ];

    private static readonly HashSet<string> Insurer =
    [
        Permissions.PreviewsWatermarked,
        Permissions.PacksPay,
        Permissions.PacksDownloadUnlocked
    ];

    private static readonly HashSet<string> CrashifyAdmin = [.. Permissions.All];

    private static readonly HashSet<string> CrashifySupport =
    [
        Permissions.PlatformTransactionsView,
        Permissions.PlatformUsersSearch
    ];
}
