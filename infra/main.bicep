// P1/M2: Azure infrastructure for JobApplicationTracker API.
// Deploys to UK South on free-tier SKUs (£0).
// Usage:
//   az group create --name rg-jobtracker --location uksouth
//   az deployment group create --resource-group rg-jobtracker \
//     --template-file infra/main.bicep \
//     --parameters sqlAdminPassword='<STRONG_PASSWORD>'

@description('Azure region for all resources.')
param location string = 'westus3'

@description('Base name for resources. Must be globally unique for some resources.')
param baseName string = 'jobtracker-noman-dev'

@description('SQL Server admin login.')
param sqlAdminLogin string = 'sqladmin'

@description('SQL Server admin password.')
@secure()
param sqlAdminPassword string

@description('GHCR username (for App Service to pull the container image).')
param ghcrUsername string = 'MuhammadNomanDev'

@description('GHCR personal access token with read:packages scope.')
@secure()
param ghcrPassword string

// ── App Service Plan (Free F1, Linux) ──────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${baseName}-plan'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// ── Web App for Containers (GHCR image) ────────────────────────────────────

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${baseName}-api'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|ghcr.io/muhammadnomandev/job-application-tracker-api:latest'
      alwaysOn: false // not available on Free tier; container starts on first request
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://ghcr.io'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: ghcrUsername
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: ghcrPassword
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        // Connection strings and JWT settings are configured post-deploy
        // via `az webapp config appsettings set` (see docs/learning/p1m2.md).
      ]
    }
  }
}

// ── Azure SQL Server + Database (free tier) ────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${baseName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled' // Free tier; restrict via firewall rules below
  }

  // Allow Azure services (App Service) to reach SQL.
  resource allowAzureServices 'firewallRules@2023-08-01-preview' = {
    name: 'AllowAllWindowsAzureIps'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'jobtrackerdb'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // Free tier: 100,000 vCore seconds per month, 32 GB storage.
    // Auto-pauses after 1 hour of inactivity; resumes on first connection.
  }
}

// ── Storage Account (Standard LRS — free 5 GB) ────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${replace(baseName, '-', '')}stor'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// ── Outputs (wire into App Service app settings) ───────────────────────────
// Note: these contain secrets. Retrieve them via `az deployment group show`
// rather than storing them anywhere.

@secure()
output sqlConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=jobtrackerdb;Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

@secure()
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
