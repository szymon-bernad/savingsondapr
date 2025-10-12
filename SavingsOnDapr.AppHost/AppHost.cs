using CommunityToolkit.Aspire.Hosting.Dapr;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;

var builder = DistributedApplication.CreateBuilder(args);
var pgConnStr = builder.AddConnectionString("pgsql").Resource;

var sodApi = builder.AddProject<Projects.SavingsOnDapr_Api>("savingsondapr-api")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "dapr-savings-api",
        AppPort = 5269,
        DaprHttpPort = 3605,
        PlacementHostAddress = "localhost:6050",
        AppProtocol = "http",
        
    })
    .WithEnvironment("ConnectionStrings__DocumentStore", pgConnStr.ConnectionStringExpression);

var currex = builder.AddProject<Projects.CurrencyExchange_Api>("currencyexchange-api")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "dapr-exchange-api",
        AppPort = 5120,
        DaprHttpPort = 3607,
        PlacementHostAddress = "localhost:6050",
        AppProtocol = "http",
    });

builder.AddProject<Projects.Dashboard_Api>("dashboard-api")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "dapr-dashboard-api",
        AppPort = 5127,
        DaprHttpPort = 3609,
        PlacementHostAddress = "localhost:6050",
        AppProtocol = "http",
    })
    .WithReference(sodApi)
    .WithReference(currex)
    .WithEnvironment("AccountsApiConfig__AccountsApiServiceName", "dapr-savings-api")
    .WithEnvironment("ExchangeApiConfig__ApiServiceName", "dapr-exchange-api")
    .WithEnvironment("EventStoreApiConfig__EventStoreApiServiceName", "dapr-savings-evt");

builder.Build().Run();
