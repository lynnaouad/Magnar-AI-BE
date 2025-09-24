namespace Magnar.AI.Application.Interfaces.Services;

public interface IAuthorizationService
{
    Task<bool> CanAccessWorkspace(int workspaceId, string username, CancellationToken cancellationToken);

    Task<bool> CanAccessWorkspace(int workspaceId, CancellationToken cancellationToken);
}
