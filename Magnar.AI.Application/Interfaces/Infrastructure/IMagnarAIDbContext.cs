using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace Magnar.AI.Application.Interfaces.Infrastructure;

public interface IMagnarAIDbContext
{
    DbSet<PersistedGrant> UserGrants { get; }
}