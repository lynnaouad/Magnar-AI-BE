// <copyright file="ChatCompletionResponse.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

using Magnar.AI.Application.Models;
using Microsoft.AspNetCore.Http;

namespace Magnar.AI.Application.Models.Responses.AI;

public class ChatCompletionResponse
{
    public bool Success { get; set; }

    public string Content { get; set; } = string.Empty;

    public Error Error { get; set; } = new Error(Constants.Errors.ErrorOccured);
}
