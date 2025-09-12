using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed class ODataConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.OData;

    [Required]
    public int SelectTop { get; init; }
}
