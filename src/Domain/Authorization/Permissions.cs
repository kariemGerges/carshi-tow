namespace CarshiTow.Domain.Authorization;

/// <summary>
/// Enterprise level 2.1 permission strings (resource.action). Policies use these same values as policy names where applicable.
/// </summary>
public static class Permissions
{
    // Tow yard (staff & admin)
    public const string UploadsCreate = "uploads.create";
    public const string PacksCreate = "packs.create";
    public const string PacksViewAssigned = "packs.view_assigned";
    public const string PacksViewYard = "packs.view_yard";

    /// <summary>Set pack / tow-yard price.</summary>
    public const string PacksSetPrice = "packs.set_price";

    public const string PayoutsManage = "payouts.manage";
    public const string DashboardView = "dashboard.view";

    // Insurer (claims / assessors)
    public const string PreviewsWatermarked = "previews.watermarked";
    public const string PacksPay = "packs.pay";
    public const string PacksDownloadUnlocked = "packs.download_unlocked";

    // Crashify platform
    public const string PlatformUsersManage = "platform.users.manage";
    public const string PlatformUsersSearch = "platform.users.search";
    public const string PlatformTransactionsView = "platform.transactions.view";
    public const string PlatformTransactionsManage = "platform.transactions.manage";
    public const string RefundsProcess = "refunds.process";
    public const string PlatformAuditRead = "platform.audit.read";

    /// <summary>Every permission string; used to register authorization policies and for Crashify Admin tokens.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        UploadsCreate,
        PacksCreate,
        PacksViewAssigned,
        PacksViewYard,
        PacksSetPrice,
        PayoutsManage,
        DashboardView,
        PreviewsWatermarked,
        PacksPay,
        PacksDownloadUnlocked,
        PlatformUsersManage,
        PlatformUsersSearch,
        PlatformTransactionsView,
        PlatformTransactionsManage,
        RefundsProcess,
        PlatformAuditRead
    ];
}
