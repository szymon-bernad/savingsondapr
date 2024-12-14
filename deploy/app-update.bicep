param appName string = 'sod--api'
param envName string = 'sod-acaenv'

param location string = resourceGroup().location

param containerRegistryName string = 'mainazsub1'

param imgVer string
param backendApiPort int = 8080
param coreResourceGroupName string = 'main-resg'
param imageName string
param containerAppName string
param identityName string = 'savingsondapr-idntty'

param secretsList array
param envVars array 

var revisionName = 'v-${replace(imgVer, '.', '-')}'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
  scope: resourceGroup(coreResourceGroupName) 
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
  scope: resourceGroup(coreResourceGroupName)
}

module spApiApp 'container-app.bicep' = {
  name: appName
  dependsOn: []
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentName: envName
    containerAppName: containerAppName
    revisionName: revisionName
    containerImage: '${containerRegistry.properties.loginServer}/${imageName}:${imgVer}'
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
