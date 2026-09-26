using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Messaging.ServiceBus;
using MemesFinderQueryGenerator;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

var builder = FunctionsApplication.CreateBuilder(args);

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Azure.Messaging.ServiceBus.*"))
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAIOptions"));
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection("ServiceBusOptions"));
builder.Services.AddOptions<QueryGenerationOptions>()
    .Bind(builder.Configuration.GetSection(nameof(QueryGenerationOptions)))
    .Validate(options => options.FidelityLevel is >= 1 and <= 10,
        "QueryGenerationOptions:FidelityLevel must be between 1 and 10.")
    .ValidateOnStart();
builder.Services.AddSingleton<OpenAIQueryClient>();
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.FullyQualifiedNamespace, new DefaultAzureCredential(), new ServiceBusClientOptions());
});

builder.Build().Run();