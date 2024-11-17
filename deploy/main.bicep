param appname string = 'sod'
param location string = resourceGroup().location
param containerRegistryName string = 'mainazsub1'
param keyVaultName string = 'main-kv-101'
param apiImgVer string = '0.4'
param eventImgVer string = '0.1'
param backendApiPort int = 8080
param coreResourceGroupName string = 'main-resg'
param spApiImage string = 'savingsondapr.api'
param eventStoreImage string = 'savingsondapr.eventstore'
param apiAppName string = 'savingsondapr-api'
param eventStoreAppName string = 'savings-eventstore'
param identityName string = 'savingsondapr-idntty'

@secure()
param sbconnstr string

@secure()
param storeconnstr string

var environmentName = '${appname}-${uniqueString(deployment().name)}'


// Container Apps Environment 
module environment 'aca-env.bicep' = {
  dependsOn: [ ]
  name: '${deployment().name}--acaenv'
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
  name: '${deployment().name}--api'
  dependsOn: [
    environment
  ]
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
    revisionMode: 'Single'
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
    ]
  }
}

// SavingAccounts API App
module eventStoreApiApp 'container-app.bicep' = {
  name: '${deployment().name}--eventstore'
  dependsOn: [
    environment
  ]
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
    componentType: 'pubsub.azure.servicebus'
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
    ]
    scopes: [
      apiAppName
      eventStoreAppName
    ]
  }
}
