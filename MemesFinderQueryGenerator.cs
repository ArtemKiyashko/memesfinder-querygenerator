using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;

namespace MemesFinderQueryGenerator;

public sealed class MemesFinderQueryGenerator
{
    private readonly OpenAIQueryClient _openAIQueryClient;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<MemesFinderQueryGenerator> _logger;

    public MemesFinderQueryGenerator(
        OpenAIQueryClient openAIQueryClient,
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusOptions> options,
        ILogger<MemesFinderQueryGenerator> logger)
    {
        _openAIQueryClient = openAIQueryClient;
        _serviceBusClient = serviceBusClient;
        _options = options.Value;
        _logger = logger;
    }

    [Function(nameof(MemesFinderQueryGenerator))]
    public async Task Run(
        [ServiceBusTrigger("textmessages", "querygenerator", Connection = "ServiceBusOptions")] Update update,
        CancellationToken cancellationToken)
    {
        var message = update.Type switch
        {
            UpdateType.Message => update.Message,
            UpdateType.EditedMessage => update.EditedMessage,
            _ => null
        };

        if (message is null || string.IsNullOrWhiteSpace(message.Text))
        {
            _logger.LogInformation("Ignoring update without text message.");
            return;
        }

        var query = await _openAIQueryClient.GenerateQueryAsync(message.Text, cancellationToken);
        var model = new TgMessageModel { Message = message, Keyword = query };

        await using var sender = _serviceBusClient.CreateSender(_options.KeywordMessagesTopic);
        var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(model))
        {
            MessageId = $"{message.Chat.Id}:{message.MessageId}"
        };
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
