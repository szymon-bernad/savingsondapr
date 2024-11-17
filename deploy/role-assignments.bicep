param location string = resourceGroup().location
param containerRegistryName string = 'mainazsub1'
param keyVaultName string = 'main-kv-101'
param containerAppName string = 'savingsondapr'

var acrPullRoleId = subscriptionResourceId( 'Microsoft.Authorization/roleDefinitions', 
                                            '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var keyVaultSecretUserRoleId = subscriptionResourceId( 'Microsoft.Authorization/roleDefinitions', 
                                                       '4633458b-17de-408a-b874-0445c86b69e6')

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${containerAppName}-idntty'
  location: location
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(identity.id, containerRegistry.id, acrPullRoleId)
  scope: containerRegistry
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: acrPullRoleId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(identity.id, keyVault.id, keyVaultSecretUserRoleId)
  scope: keyVault
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: keyVaultSecretUserRoleId
    principalType: 'ServicePrincipal'
  }
}
