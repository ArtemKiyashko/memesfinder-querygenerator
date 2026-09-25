using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemesFinderQueryGenerator;

public sealed class OpenAIQueryClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIQueryClient> _logger;

    public OpenAIQueryClient(IOptions<OpenAIOptions> options, ILogger<OpenAIQueryClient> logger)
    {
        var openAIOptions = options.Value;
        if (string.IsNullOrWhiteSpace(openAIOptions.ApiKey))
            throw new InvalidOperationException("OpenAIOptions:ApiKey is not configured.");

        _chatClient = new ChatClient(openAIOptions.Model, openAIOptions.ApiKey);
        _logger = logger;
    }

    public async Task<string> GenerateQueryAsync(string message, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(QueryPrompt.System),
            new UserChatMessage(QueryPrompt.User(message))
        };

        var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var result = completion.Value.Content[0].Text;

        var query = NormalizeQuery(result);
        if (query is null)
            throw new InvalidOperationException("OpenAI returned an invalid search query.");

        return query;
    }

    private string? NormalizeQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var query = value.Trim().Trim('"', '\'');
        if (query.Contains('\n') || query.Contains('\r') || query.Length is < 3 or > 150)
            return null;

        if (query.StartsWith("{") || query.Contains("```", StringComparison.Ordinal))
            return null;

        _logger.LogInformation("Generated meme search query: {Query}", query);
        return query;
    }
}
