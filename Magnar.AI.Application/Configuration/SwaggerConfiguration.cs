// <copyright file="SwaggerConfiguration.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

namespace Magnar.AI.Application.Configuration;

public sealed record SwaggerConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.Swagger;

    public string? Route { get; init; }

    public string? Title { get; init; }

    public string? Version { get; init; }

    public string? Description { get; init; }

    public Contact Contact { get; init; } = new();
}
