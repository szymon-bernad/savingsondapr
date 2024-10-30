using Carter;
using CurrencyExchange.Api.Internal;
using System.Text.Json.Serialization;
using System.Text.Json;
using Dapr.Workflow;

var builder = WebApplication.CreateBuilder(args);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ??
                    throw new ApplicationException("DAPR_HTTP_PORT is not set as Env Var");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IExchangeRatesService, ExchangeRatesService>();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();
app.UseHttpsRedirection();

app.Run();

var wfc = app.Services.GetRequiredService<DaprWorkflowClient>();