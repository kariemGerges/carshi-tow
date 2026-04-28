namespace CarshiTow.Domain.Events;

public sealed record UserLoggedInEvent(Guid UserId, DateTime OccurredAtUtc);
