
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Magnar.AI.Application.Interfaces.Infrastructure;

public interface IMagnarAIDbContext
{
    DbSet<PersistedGrant> UserGrants { get; }

    DbSet<DataProtectionKey> DataProtectionKeys { get; }

    DbSet<Provider> Connection { get; }
}