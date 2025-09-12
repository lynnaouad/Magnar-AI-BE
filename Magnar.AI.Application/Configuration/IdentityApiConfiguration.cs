using Duende.IdentityServer.Models;

namespace Magnar.AI.Application.Configuration;

public sealed record IdentityApiConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.IdentityApi;

    public IEnumerable<Client> Clients { get; init; } = [];
}