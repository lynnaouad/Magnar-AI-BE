// <copyright file="UrlsConfiguration.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed record UrlsConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.Urls;

    [Required]
    public string WebUrl { get; init; } = string.Empty;

    [Required]
    public string Authority { get; init; } = string.Empty;
}