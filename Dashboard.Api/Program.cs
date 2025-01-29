using Carter;
using Dashboard.Api.ApiClients;
using Microsoft.Identity.Web;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ValidateAccessTokenPolicy", validateAccessTokenPolicy =>
    {
        // Validate ClientId from token
        // only accept tokens issued ....
        validateAccessTokenPolicy.RequireClaim("aud", "api://6d0aad49-2334-4353-9481-216f6c931969");
    });
});

builder.Services.AddCors();

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

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();
app.MapSubscribeHandler();
app.UseHttpsRedirection();
app.Run();
