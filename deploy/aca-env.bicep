param acaEnvironmentName string
param location string
param logAnalyticsName string = 'log-analytics-wspc1'
param appInsightsName string = 'main-res-appins1'
param coreResourceGroupName string
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
  scope: resourceGroup(coreResourceGroupName)
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
  scope: resourceGroup(coreResourceGroupName)
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: acaEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    appInsightsConfiguration: {
      connectionString: appInsights.properties.ConnectionString
    }
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: [
          'appInsights'
        ]
      }
      logsConfiguration: {
        destinations: [
          'appInsights'
        ]
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output acaEnvironmentName string = containerAppEnvironment.name
output acaEnvironmentId string = containerAppEnvironment.id
