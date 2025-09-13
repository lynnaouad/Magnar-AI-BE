// <copyright file="AISearchParametersDto.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

namespace Magnar.AI.Application.Dto.AI;

public sealed record AISearchParametersDto
{
    public int Top { get; init; }

    public string Prompt { get; init; } = string.Empty;
}
