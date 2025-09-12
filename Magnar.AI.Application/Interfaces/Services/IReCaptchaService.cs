namespace Magnar.AI.Application.Interfaces.Services;

public interface IReCaptchaService
{
    Task<bool> ValidateReCaptchaTokenAsync(string token);
}