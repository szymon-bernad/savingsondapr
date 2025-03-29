using Azure.Monitor.OpenTelemetry.AspNetCore;
using Carter;
using Marten;
using Marten.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Common.Config;
using System.Text.Json;
using System.Text.Json.Serialization;
using Weasel.Core;
using SavingsOnDapr.EventStore.Handlers;
using SavingsPlatform.Common.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMarten(
    opts =>
    {
        opts.Connection(builder.Configuration.GetConnectionString("MartenStore") ??
            throw new ArgumentNullException("MartenStore"));

        opts.Schema.For<EventStatusEntry>().UseIdentityKey();
        opts.AutoCreateSchemaObjects = AutoCreate.All;
        opts.Events.AppendMode = EventAppendMode.Quick;
        opts.Events.StreamIdentity = StreamIdentity.AsString;
    });

builder.Services.AddScoped<AccountHierarchyEventStore>();
builder.Services.AddScoped<CurrencyExchangeEventStore>();
builder.Services.AddScoped<ICurrencyExchangeSummaryRequestHandler, CurrencyExchangeSummaryRequestHandler>();
builder.Services.AddScoped<IEventPublishingService, DaprEventPublishingService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? throw new ApplicationException("DAPR_HTTP_PORT is not set as EnvVar");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });
builder.Services.AddCarter();
var svcConfig = builder.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();

builder.Logging.AddOpenTelemetry(x =>
{
    x.IncludeScopes = true;
    x.IncludeFormattedMessage = true;
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
});

(svcConfig?.UseAzureMonitor ?? false ?
    builder.Services.AddOpenTelemetry().UseAzureMonitor() :
    builder.Services.AddOpenTelemetry())
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .ConfigureResource(r => r.AddService("savings-eventstore"))
               .AddConsoleExporter();
    });

var app = builder.Build();

app.UseCloudEvents();
app.MapCarter();

app.MapGet("/", () => Results.LocalRedirect("~/swagger"));
app.MapSubscribeHandler();
app.UseRouting();
app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
