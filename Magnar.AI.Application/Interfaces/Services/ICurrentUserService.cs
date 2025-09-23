namespace Magnar.AI.Application.Interfaces.Services;

public interface ICurrentUserService
{
    int GetId();

    string GetUsername();

    string GetEmail();

    bool IsCurrentUser(int userId);
}
