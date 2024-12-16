using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Carter;
using Marten;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SavingsOnDapr.Api;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.DependencyInjection;
using SavingsPlatform.Accounts.Handlers;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Contracts.Accounts;
using SavingsPlatform.Contracts.Accounts.Commands;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ??
                    throw new ApplicationException("DAPR_HTTP_PORT is not set as Env Var");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });

builder.Services.AddSavingsAccounts(builder.Configuration);

var svcConfig = builder.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();
builder.Services.AddMarten(options =>
    {
        options.Connection(builder.Configuration.GetConnectionString("DocumentStore")
            ?? throw new NullReferenceException("DocumentStore ConnectionString"));

        options.UseSystemTextJsonForSerialization(EnumStorage.AsString, Casing.CamelCase);
        options.AutoCreateSchemaObjects = AutoCreate.All;

        options.Schema.For<MessageProcessedEntry>().UseIdentityKey();
        options.Schema.For<AggregateState<InstantAccessSavingsAccountState>>().UseOptimisticConcurrency(true);
        options.Schema.For<AggregateState<CurrentAccountState>>().UseOptimisticConcurrency(true);
    })
 .BuildSessionsWith<CustomSessionFactory>();

 builder.Logging.AddOpenTelemetry(x =>
 {
     x.IncludeScopes = true;
     x.IncludeFormattedMessage = true;
 });

(svcConfig?.UseAzureMonitor ?? false ?
    builder.Services.AddOpenTelemetry().UseAzureMonitor() : 
    builder.Services.AddOpenTelemetry())
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .ConfigureResource(r => r.AddService("savings-accounts"))
                    .AddZipkinExporter()
                    .AddConsoleExporter();
        });

builder.Services.AddLogging(cfg =>
{
    cfg.AddConsole();
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(new Assembly[]
    {
        Assembly.GetExecutingAssembly(),
        Assembly.GetAssembly(typeof(PublishEventsCommand))!,
        Assembly.GetAssembly(typeof(CreateInstantAccessSavingsAccountCmdHandler))!,
    });
});

var app = builder.Build();

app.UseCloudEvents();
app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();

app.MapSubscribeHandler();
app.UseRouting();
app.MapActorsHandlers();

app.Run();