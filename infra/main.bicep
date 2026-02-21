@description('Azure location for deployment resources.')
param location string = resourceGroup().location

@description('Container Apps environment name.')
param containerAppsEnvironmentName string = 'replay-env'

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {}
}

output CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.name
output CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.id
