using Telegram.Bot.Types;

namespace MemesFinderQueryGenerator;

public sealed class TgMessageModel
{
    public Message Message { get; set; } = new();
    public string Keyword { get; set; } = string.Empty;
}
