using UserService.Application.DTOs;

namespace UserService.Application.Services;

public interface IProfileSnapshotService
{
    Task<ProfileSnapshotResponse?> GetCurrentAsync(string userId, CancellationToken cancellationToken = default);
}