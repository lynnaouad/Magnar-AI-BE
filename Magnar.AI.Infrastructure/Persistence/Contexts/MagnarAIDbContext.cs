using Duende.IdentityServer.Models;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection;

namespace Magnar.AI.Infrastructure.Persistence.Contexts;

public class MagnarAIDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int,
      IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
      IdentityRoleClaim<int>, IdentityUserToken<int>>, IMagnarAIDbContext
{
    private readonly AuditableEntityInterceptor auditableEntityInterceptor;

    public MagnarAIDbContext(DbContextOptions<MagnarAIDbContext> options, AuditableEntityInterceptor auditableEntityInterceptor)
        : base(options)
    {
        this.auditableEntityInterceptor = auditableEntityInterceptor;
    }

    public DbSet<PersistedGrant> UserGrants => Set<PersistedGrant>();

    public DbSet<Connection> Connection => Set<Connection>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditableEntityInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
