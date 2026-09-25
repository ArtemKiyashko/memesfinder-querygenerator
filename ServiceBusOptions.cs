namespace MemesFinderQueryGenerator;

public sealed class ServiceBusOptions
{
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    public string TextMessagesTopic { get; set; } = "textmessages";
    public string KeywordMessagesTopic { get; set; } = "keywordmessages";
}
