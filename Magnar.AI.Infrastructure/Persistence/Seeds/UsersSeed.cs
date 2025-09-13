using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Magnar.AI.Infrastructure.Persistence.Seeds;

public static class UsersSeed
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        ApplicationUser defaultUser = new()
        {
            UserName = Constants.Seeds.DefaultUser.Username,
            Email = Constants.Seeds.DefaultUser.Email,
            Active = true,
            FirstName = Constants.Seeds.DefaultUser.FirstName,
            LastName = Constants.Seeds.DefaultUser.LastName,
            EmailConfirmed = true,
        };

        ApplicationUser user = await userManager.FindByNameAsync(defaultUser.UserName);
        if (user is not null)
        {
            return;
        }

        IdentityResult result = await userManager.CreateAsync(defaultUser, Constants.Seeds.DefaultUser.Password);
        if (result.Succeeded)
        {
            await userManager.SetLockoutEnabledAsync(defaultUser, false);
            await unitOfWork.SaveChangesAsync();
        }
    }
}