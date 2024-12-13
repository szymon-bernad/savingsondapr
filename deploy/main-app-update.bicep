param appname string = 'sod'
param location string = resourceGroup().location

param containerRegistryName string = 'mainazsub1'
param keyVaultName string
param apiImgVer string

param backendApiPort int = 8080
param coreResourceGroupName string = 'main-resg'

param spApiImage string

param apiAppName string
param eventStoreAppName string

param identityName string = 'savingsondapr-idntty'

param secretsList array
param envVars array 
var environmentName = '${appname}-acaenv'

resource environment 'Microsoft.App/managedEnvironments@2023-11-02-preview' existing = {
  name: environmentName
  scope: resourceGroup() 
}
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
  scope: resourceGroup(coreResourceGroupName) 
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
  scope: resourceGroup(coreResourceGroupName)
}

module spApiApp 'container-app.bicep' = {
  name: '${appname}--api'
  dependsOn: []
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentName: environment.name
    containerAppName: apiAppName
    revisionName: 'v-${replace(apiImgVer, '.', '-')}'
    containerImage: '${containerRegistry.properties.loginServer}/${spApiImage}:${apiImgVer}'
    targetPort: backendApiPort
    minReplicas: 1
    maxReplicas: 1
    containerRegistryServer: containerRegistry.properties.loginServer
    identityName: identityName
    revisionMode: 'Single'
    secretsRefList: [for secrt in secretsList: {
      name: secrt.name
      keyVaultUrl: secrt.value
      identity: identity.id
    }]
    envVarsList: envVars
  }
}
