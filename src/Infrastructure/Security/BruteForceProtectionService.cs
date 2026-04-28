using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CarshiTow.Infrastructure.Security;

public sealed class BruteForceProtectionService(IDistributedCache cache) : IBruteForceProtectionService
{
    private const int LoginFailureThreshold = 5;
    private const int OtpFailureThreshold = 6;
    private static readonly TimeSpan SlidingWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LoginLockout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan OtpLockout = TimeSpan.FromMinutes(10);

    public Task EnsureLoginAllowedAsync(string email, string ipAddress, CancellationToken cancellationToken)
    {
        var state = GetState(LoginKey(email, ipAddress));
        if (state?.LockedUntilUtc is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return Task.CompletedTask;
    }

    public Task RegisterLoginFailureAsync(string email, string ipAddress, CancellationToken cancellationToken)
    {
        var key = LoginKey(email, ipAddress);
        var state = GetState(key) ?? new AttemptState();
        state.Failures++;
        if (state.Failures >= LoginFailureThreshold)
        {
            state.LockedUntilUtc = DateTime.UtcNow.Add(LoginLockout);
            state.Failures = 0;
        }

        SetState(key, state, SlidingWindow);
        return Task.CompletedTask;
    }

    public Task ResetLoginFailuresAsync(string email, string ipAddress, CancellationToken cancellationToken)
    {
        cache.Remove(LoginKey(email, ipAddress));
        return Task.CompletedTask;
    }

    public Task EnsureOtpAllowedAsync(Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        var state = GetState(OtpKey(userId, ipAddress));
        if (state?.LockedUntilUtc is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Too many invalid OTP attempts.");
        }

        return Task.CompletedTask;
    }

    public Task RegisterOtpFailureAsync(Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        var key = OtpKey(userId, ipAddress);
        var state = GetState(key) ?? new AttemptState();
        state.Failures++;
        if (state.Failures >= OtpFailureThreshold)
        {
            state.LockedUntilUtc = DateTime.UtcNow.Add(OtpLockout);
            state.Failures = 0;
        }

        SetState(key, state, SlidingWindow);
        return Task.CompletedTask;
    }

    public Task ResetOtpFailuresAsync(Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        cache.Remove(OtpKey(userId, ipAddress));
        return Task.CompletedTask;
    }

    private static string LoginKey(string email, string ipAddress) => $"bf:login:{email}:{ipAddress}";
    private static string OtpKey(Guid userId, string ipAddress) => $"bf:otp:{userId}:{ipAddress}";

    private AttemptState? GetState(string key)
    {
        var raw = cache.GetString(key);
        return string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<AttemptState>(raw);
    }

    private void SetState(string key, AttemptState state, TimeSpan sliding)
    {
        var raw = JsonSerializer.Serialize(state);
        cache.SetString(key, raw, new DistributedCacheEntryOptions
        {
            SlidingExpiration = sliding
        });
    }

    private sealed class AttemptState
    {
        public int Failures { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
    }
}
