namespace Magnar.AI.Application.Models;

public sealed record UserCredentials(
    string UserName,
    string Password,
    string Token);
