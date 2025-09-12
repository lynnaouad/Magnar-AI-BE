// <copyright file="VectorConfiguration.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed record VectorConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.VectorConfiguration;

    [Required]
    public bool EnableVectors { get; init; }

    [Required]
    public bool SeedDatabaseSchema { get; init; }
}