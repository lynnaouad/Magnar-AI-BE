// <copyright file="ChatCompletionResponse.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

using Microsoft.SemanticKernel.ChatCompletion;

namespace Magnar.AI.Application.Models.Responses.AI;

public class ChatCompletionResponse
{
    public bool Success { get; set; }

    public string Content { get; set; } = string.Empty;

    public Error Error { get; set; } = new Error(Constants.Errors.ErrorOccured);

    public ChatMessageContentItemCollection Items { get; set; } = [];
}
