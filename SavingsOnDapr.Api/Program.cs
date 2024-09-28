using System.Text.Json.Serialization;
using System.Text.Json;
using Marten;
using Weasel.Core;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Contracts.Accounts;
using SavingsPlatform.Accounts.DependencyInjection;
using SavingsOnDapr.Api;
using Carter;
using SavingsPlatform.Contracts.Accounts.Commands;
using System.Reflection;
using SavingsPlatform.Accounts.Handlers;
using SavingsPlatform.Accounts.Current.Models;

var builder = WebApplication.CreateBuilder(args);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ??
                    throw new ApplicationException("DAPR_HTTP_PORT is not set as Env Var");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });
builder.Services.AddSavingsAccounts();

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