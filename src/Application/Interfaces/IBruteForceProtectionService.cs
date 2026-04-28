namespace CarshiTow.Application.Interfaces;

public interface IBruteForceProtectionService
{
    Task EnsureLoginAllowedAsync(string email, string ipAddress, CancellationToken cancellationToken);
    Task RegisterLoginFailureAsync(string email, string ipAddress, CancellationToken cancellationToken);
    Task ResetLoginFailuresAsync(string email, string ipAddress, CancellationToken cancellationToken);

    Task EnsureOtpAllowedAsync(Guid userId, string ipAddress, CancellationToken cancellationToken);
    Task RegisterOtpFailureAsync(Guid userId, string ipAddress, CancellationToken cancellationToken);
    Task ResetOtpFailuresAsync(Guid userId, string ipAddress, CancellationToken cancellationToken);
}
