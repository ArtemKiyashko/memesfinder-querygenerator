using System.Net.Http.Headers;
using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemesFinderQueryGenerator;

public sealed class OpenAIQueryClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAIQueryClient> _logger;

    public OpenAIQueryClient(HttpClient httpClient, IOptions<OpenAIOptions> options, ILogger<OpenAIQueryClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateQueryAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAIOptions:ApiKey is not configured.");

        var request = new
        {
            model = _options.Model,
            temperature = 0.2,
            max_tokens = 80,
            messages = new[]
            {
                new { role = "system", content = QueryPrompt.System },
                new { role = "user", content = QueryPrompt.User(message) }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(body);
        var result = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

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
