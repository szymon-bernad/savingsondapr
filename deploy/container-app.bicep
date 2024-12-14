param containerAppName string
param location string
param environmentName string
param containerImage string
param targetPort int
param isExternalIngress bool
param enableIngress bool 
param minReplicas int = 0
param maxReplicas int = 1
param envVarsList array = []
param secretsRefList array = []
param revisionMode string = 'Single'
param revisionName string = 'std'
param useProbes bool = true
param transport string = 'auto'
param identityName string
param containerRegistryServer string
param coreResGroupName string = 'main-resg'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
  scope: resourceGroup(coreResGroupName) 
}

resource env 'Microsoft.App/managedEnvironments@2023-11-02-preview' existing = {
  name: environmentName
}

resource containerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}' : {}
    }
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: revisionMode
      registries: [
        {
          server: containerRegistryServer
          identity: identity.id
        }
      ]
      secrets: secretsRefList
      ingress: enableIngress ? {
        external: isExternalIngress
        targetPort: targetPort
        transport: transport
      } : null
      dapr: {
        enabled: true
        appPort: targetPort
        appId: containerAppName
        appProtocol: 'http'
        
      }
    }
    template: {
      revisionSuffix: revisionName
      containers: [
        {
          image: containerImage
          name: containerAppName
          env: envVarsList
          probes: useProbes? [
            {
              type: 'Readiness'
               httpGet: {
                 port: 8080
                 path: '/healthz'
                 scheme: 'HTTP'
               }
              periodSeconds: 10
              timeoutSeconds: 10
              initialDelaySeconds: 15
              successThreshold: 1
              failureThreshold: 15
            }
          ] : null
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output fqdn string = enableIngress ? containerApp.properties.configuration.ingress.fqdn 
                      : 'Ingress not enabled'
