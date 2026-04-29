using CarshiTow.Domain.Enums;

namespace CarshiTow.Application.Interfaces;

public interface IPlatformAdminService
{
    Task AssignUserRoleAsync(Guid targetUserId, UserRole newRole, CancellationToken cancellationToken);
}
