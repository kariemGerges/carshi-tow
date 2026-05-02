namespace CarshiTow.Domain.Enums;

/// <summary>Enterprise level 2.1 user roles (stored as PostgreSQL <c>user_role</c>).</summary>
public enum UserRole
{
    /// <summary>Tow yard owner or manager — full MFA; upload, packs, pricing, payouts, dashboard.</summary>
    TowYardAdmin = 1,

    /// <summary>Tow yard employee — MFA; upload photos, create packs, view assigned packs.</summary>
    TowYardStaff = 2,

    /// <summary>Insurer claims officer or assessor — link/access preview; optional account; watermark preview, pay, download unlocked.</summary>
    Insurer = 3,

    /// <summary>Crashify operator — MFA + authenticator app; full platform access, users, transactions, refunds.</summary>
    CrashifyAdmin = 4,

    /// <summary>Crashify support — MFA; view transactions and search users; no refunds or payouts.</summary>
    CrashifySupport = 5
}

public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    PendingVerification = 3
}

public enum TowYardStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,

    /// <summary>Registration rejected by Crashify Admin (AD-002); reason stored on the tow yard record.</summary>
    Rejected = 4
}

public enum PhotoPackStatus
{
    Draft = 1,
    PendingProcessing = 2,
    Active = 3,
    Paid = 4,
    Expired = 5,
    Flagged = 6
}

public enum DamageSeverity
{
    Low = 1,
    Moderate = 2,
    Severe = 3,
    ProbableTotalLoss = 4
}

public enum PhotoCategory
{
    Front = 1,
    Rear = 2,
    Left = 3,
    Right = 4,
    Interior = 5,
    Engine = 6,
    Undercarriage = 7,
    Odometer = 8,
    Other = 9
}

public enum TransactionStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

public enum PayoutStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public enum AustralianState
{
    NSW = 1,
    VIC = 2,
    QLD = 3,
    WA = 4,
    SA = 5,
    TAS = 6,
    NT = 7,
    ACT = 8
}
