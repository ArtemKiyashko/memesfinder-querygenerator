using Azure.Identity;
using Azure.Messaging.ServiceBus;
using MemesFinderQueryGenerator;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAIOptions"));
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection("ServiceBusOptions"));
builder.Services.AddHttpClient<OpenAIQueryClient>();
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.FullyQualifiedNamespace, new DefaultAzureCredential(), new ServiceBusClientOptions());
});

builder.Build().Run();