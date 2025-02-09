using Carter;
using Dashboard.Api;
using Dashboard.Api.ApiClients;
using Microsoft.Identity.Web;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var authConfig = builder.Configuration.GetSection("AzureAd").Get<AzureAdConfig>();
Console.WriteLine($"AzureAdConfig: {authConfig?.Instance} {authConfig?.TenantId} {authConfig?.ClientId}");

builder.Services.AddCors();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

builder.Services.Configure<AccountsApiConfig>(builder.Configuration.GetSection("AccountsApiConfig"));
builder.Services.Configure<ExchangeApiConfig>(builder.Configuration.GetSection("ExchangeApiConfig"));

builder.Services.AddScoped<IAccountsApiClient, AccountsApiClient>()
                .AddScoped<IExchangeApiClient, ExchangeApiClient>()
                .AddScoped<IEventPublishingService, DaprEventPublishingService>();
var svcConfig = builder.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ??
                    throw new ApplicationException("DAPR_HTTP_PORT is not set as Env Var");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

builder.Logging.AddOpenTelemetry(x =>
{
    x.IncludeScopes = true;
    x.IncludeFormattedMessage = true;
});

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .ConfigureResource(r => r.AddService("dashboard-api")));
var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});
app.UseAuthentication();
app.UseAuthorization();

app.UseCloudEvents();
app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();
app.MapSubscribeHandler();
app.Run();
