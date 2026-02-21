@description('Azure location for deployment resources.')
param location string = resourceGroup().location

@description('Container Apps environment name.')
param containerAppsEnvironmentName string = 'replay-env'

@description('Container App name.')
param containerAppName string = 'replay'

@description('Container image for the app.')
param containerImage string

@description('GHCR username used by ACA to pull the container image.')
param ghcrUsername string

@secure()
@description('GHCR personal access token used by ACA to pull the container image.')
param ghcrPat string

@secure()
@description('Spotify client ID.')
param spotifyClientId string

@secure()
@description('Spotify client secret.')
param spotifyClientSecret string

@secure()
@description('Spotify redirect URI.')
param spotifyRedirectUri string

@secure()
@description('Optional Last.fm API key.')
param lastfmApiKey string = ''

@secure()
@description('Optional Discogs consumer key.')
param discogsConsumerKey string = ''

@secure()
@description('Optional Discogs consumer secret.')
param discogsConsumerSecret string = ''

@secure()
@description('Optional Setlist.fm API key.')
param setlistfmApiKey string = ''

@description('Optional custom domain for ingress (for example: replay.rickroche.com).')
param customDomain string = ''

var useCustomDomain = !empty(customDomain)

var optionalSecrets = concat(
  empty(lastfmApiKey) ? [] : [{ name: 'lastfm-api-key', value: lastfmApiKey }],
  empty(discogsConsumerKey) ? [] : [{ name: 'discogs-consumer-key', value: discogsConsumerKey }],
  empty(discogsConsumerSecret) ? [] : [{ name: 'discogs-consumer-secret', value: discogsConsumerSecret }],
  empty(setlistfmApiKey) ? [] : [{ name: 'setlistfm-api-key', value: setlistfmApiKey }]
)

var optionalEnvVars = concat(
  empty(lastfmApiKey) ? [] : [{ name: 'Lastfm__ApiKey', secretRef: 'lastfm-api-key' }],
  empty(discogsConsumerKey) ? [] : [{ name: 'Discogs__ConsumerKey', secretRef: 'discogs-consumer-key' }],
  empty(discogsConsumerSecret) ? [] : [{ name: 'Discogs__ConsumerSecret', secretRef: 'discogs-consumer-secret' }],
  empty(setlistfmApiKey) ? [] : [{ name: 'SetlistFm__ApiKey', secretRef: 'setlistfm-api-key' }]
)

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2025-07-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {}
}

resource containerApp 'Microsoft.App/containerApps@2025-07-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        customDomains: useCustomDomain ? [
          {
            name: customDomain
            bindingType: 'Auto'
          }
        ] : []
      }
      registries: [
        {
          server: 'ghcr.io'
          username: ghcrUsername
          passwordSecretRef: 'ghcr-pat'
        }
      ]
      secrets: concat([
        {
          name: 'ghcr-pat'
          value: ghcrPat
        }
        {
          name: 'spotify-client-id'
          value: spotifyClientId
        }
        {
          name: 'spotify-client-secret'
          value: spotifyClientSecret
        }
        {
          name: 'spotify-redirect-uri'
          value: spotifyRedirectUri
        }
      ], optionalSecrets)
    }
    template: {
      containers: [
        {
          name: 'server'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat([
            {
              name: 'Spotify__ClientId'
              secretRef: 'spotify-client-id'
            }
            {
              name: 'Spotify__ClientSecret'
              secretRef: 'spotify-client-secret'
            }
            {
              name: 'Spotify__RedirectUri'
              secretRef: 'spotify-redirect-uri'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ], optionalEnvVars)
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.name
output CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.id
output CONTAINER_APP_NAME string = containerApp.name
