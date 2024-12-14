param appname string = 'sod'
param location string = resourceGroup().location

param containerRegistryName string = 'mainazsub1'
param keyVaultName string = 'main-kv-101'
param apiImgVer string = ''
param eventImgVer string = ''
param exchImgVer string = ''

param backendApiPort int = 8080
param coreResourceGroupName string = 'main-resg'

param spApiImage string = 'savingsondapr.api'
param eventStoreImage string = 'savingsondapr.eventstore'
param exchApiImage string = 'savingsondapr.currencyexchange'

param apiAppName string = 'sod-api'
param eventStoreAppName string = 'sod-eventstore'
param exchAppName string = 'sod-exchange'

param identityName string = 'savingsondapr-idntty'


@secure()
param sbconnstr string

@secure()
param storeconnstr string

@secure()
param cfgconnstr string

var environmentName = '${appname}-acaenv'

// Container Apps Environment 
module environment 'aca-env.bicep' = {
  dependsOn: [ ]
  name: environmentName
  params: {
    acaEnvironmentName: environmentName
    location: location
    coreResourceGroupName: coreResourceGroupName
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
  scope: resourceGroup(coreResourceGroupName) 
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
  scope: resourceGroup(coreResourceGroupName)
}

// SavingAccounts API App
module spApiApp 'container-app.bicep' = {
  name: '${appname}--api'
  dependsOn: []
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentName: environment.outputs.acaEnvironmentName
    containerAppName: apiAppName
    containerImage: '${containerRegistry.properties.loginServer}/${spApiImage}:${apiImgVer}'
    targetPort: backendApiPort
    minReplicas: 1
    maxReplicas: 1
    containerRegistryServer: containerRegistry.properties.loginServer
    identityName: identityName
    revisionMode: 'Multiple'
    revisionName: 'v-${ replace(apiImgVer, '.', '-')}'
    secretsRefList: [
      {
        name: 'docstoreconnstr'
        keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/docstoreconnstr'
        identity: identity.id
      }
      {
        name: 'appinsconnstr'
        keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/appinsconnstr'
        identity: identity.id
      }
    ]
    envVarsList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
        name: 'NAMESPACE'
        value: 'savings'
      }
	    {
        name: 'ConnectionStrings__DocumentStore'
        secretRef: 'docstoreconnstr'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        secretRef: 'appinsconnstr'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
      {
        name: 'EventStoreApiConfig__EventStoreApiServiceName'
        value: eventStoreAppName
      }
      {
        name: 'ServiceConfig__Version'
        value: 'v-${apiImgVer}'
      }
    ]
  }
}

// EventStore App
module eventStoreApiApp 'container-app.bicep' = {
  name: '${appname}--eventstore'
  dependsOn: []
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentName: environment.outputs.acaEnvironmentName
    containerAppName: eventStoreAppName
    containerImage: '${containerRegistry.properties.loginServer}/${eventStoreImage}:${eventImgVer}'
    targetPort: backendApiPort
    minReplicas: 1
    maxReplicas: 1
    containerRegistryServer: containerRegistry.properties.loginServer
    identityName: identityName
    revisionMode: 'Single'
    secretsRefList: [
      {
        name: 'eventstoreconnstr'
        keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/eventstoreconnstr'
        identity: identity.id
      }
      {
        name: 'appinsconnstr'
        keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/appinsconnstr'
        identity: identity.id
      }
    ]
    envVarsList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
        name: 'NAMESPACE'
        value: 'savings'
      }
	    {
        name: 'ConnectionStrings__MartenStore'
        secretRef: 'eventstoreconnstr'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        secretRef: 'appinsconnstr'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ]
  }
}

// SavingAccounts API App
module exchApiApp 'container-app.bicep' = {
  name: '${appname}--exch'
  dependsOn: []
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentName: environment.outputs.acaEnvironmentName
    containerAppName: exchAppName
    containerImage: '${containerRegistry.properties.loginServer}/${exchApiImage}:${exchImgVer}'
    targetPort: backendApiPort
    minReplicas: 1
    maxReplicas: 1
    containerRegistryServer: containerRegistry.properties.loginServer
    identityName: identityName
    revisionMode: 'Single'
    secretsRefList: [
      {
        name: 'appinsconnstr'
        keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/appinsconnstr'
        identity: identity.id
      }
    ]
    envVarsList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
        name: 'NAMESPACE'
        value: 'savings'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        secretRef: 'appinsconnstr'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
      {
        name: 'AccountsApiConfig__AccountsApiServiceName'
        value: apiAppName
      }
    ]
  }
}

////Statestore Component
resource statestoreDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  name: '${environmentName}/statestore-postgres'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'state.postgresql'
    version: 'v1'
    secrets: [
      {
        name: 'storeconnectionstring'
        value: storeconnstr
      }
    ]
    initTimeout: '60s'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'storeconnectionstring'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      } ]
    scopes: [
      apiAppName
      exchAppName
    ]
  }
}

//pubsub Service Bus Component
resource pubsubServicebusDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  name: '${environmentName}/pubsub'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'pubsub.azure.servicebus.topics'
    version: 'v1'
    secrets: [
      {
        name: 'sbrootconnectionstring'
        value: sbconnstr
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'sbrootconnectionstring'
      }
      {
        name: 'maxDeliveryCount'
        value: '1'
      }
      {
        name: 'maxRetriableErrorsPerSec'
        value: '2'
      }
    ]
    scopes: [
      apiAppName
      eventStoreAppName
      exchAppName
    ]
  }
}

//App Configuration store component
resource appcfgDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  name: '${environmentName}/appcfg'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'configuration.azure.appconfig'
    version: 'v1'
    secrets: [
      {
        name: 'connstring'
        value: cfgconnstr
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'connstring'
      }
    ]
    scopes: [
      apiAppName
      eventStoreAppName
      exchAppName
    ]
  }
}

resource myPubSubPolicy 'Microsoft.App/managedEnvironments/daprComponents/resiliencyPolicies@2023-11-02-preview' = {
  name: '${environmentName}-pubsubplcy'
  parent: pubsubServicebusDaprComponent
  properties: {
    outboundPolicy: {
      httpRetryPolicy: {
          maxRetries: 1
          retryBackOff: {
            initialDelayInMilliseconds: 1000
            maxIntervalInMilliseconds: 1000
          }
        }
    } 
    inboundPolicy: {
      httpRetryPolicy: {
        maxRetries: 8
        retryBackOff: {
          initialDelayInMilliseconds: 200
          maxIntervalInMilliseconds: 5000
        }
      }
    }
  }
}

