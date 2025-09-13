using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Magnar.AI.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly Contexts.MagnarAIDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUnitOfWork unitOfWork;

    public DatabaseInitializer(
        Contexts.MagnarAIDbContext context,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
    {
        this.context = context;
        this.userManager = userManager;
        this.unitOfWork = unitOfWork;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, Constants.Errors.DatabaseInitialization);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, Constants.Errors.DatabaseInitialization);
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        await Seeds.UsersSeed.SeedAsync(userManager, unitOfWork);
    }
}