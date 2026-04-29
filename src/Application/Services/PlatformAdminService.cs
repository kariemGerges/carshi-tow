using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Services;

public sealed class PlatformAdminService(IUserRepository userRepository) : IPlatformAdminService
{
    public async Task AssignUserRoleAsync(Guid targetUserId, UserRole newRole, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(targetUserId, cancellationToken) ??
                   throw new KeyNotFoundException("User not found.");
        if (user.DeletedAtUtc is not null)
        {
            throw new InvalidOperationException("Cannot change role for a deleted user.");
        }

        user.Role = newRole;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
