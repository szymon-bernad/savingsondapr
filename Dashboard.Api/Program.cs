using Carter;
using Dashboard.Api.ApiClients;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<AccountsApiConfig>(builder.Configuration.GetSection("AccountsApiConfig"));

builder.Services.AddScoped<IAccountsApiClient, AccountsApiClient>()
                .AddScoped<IEventPublishingService, DaprEventPublishingService>();
var svcConfig = builder.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ??
                    throw new ApplicationException("DAPR_HTTP_PORT is not set as Env Var");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();
app.MapSubscribeHandler();
app.UseHttpsRedirection();
app.UseCloudEvents();
app.Run();
