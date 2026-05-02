using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.DTOs;

public sealed record TowYardDashboardSummaryDto(
    Guid TowYardId,
    string BusinessName,
    TowYardStatus YardStatus,
    int PacksDraft,
    int PacksActive,
    int PacksPaid,
    int PacksExpired,
    int PacksFlagged,
    int TransactionsSucceeded30d,
    int GrossRevenueCents30d,
    int GrossRevenueCentsAllTime);

public sealed record TowYardPayoutListItemDto(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    short TransactionCount,
    int GrossAmountCents,
    int NetAmountCents,
    PayoutStatus Status,
    DateTime? CompletedAtUtc);

public sealed record TowYardPayoutBalanceDto(
    int AccruedNetCents,
    int CompletedPayoutsNetCents,
    int AvailableBalanceCents);
