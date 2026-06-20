// ═══════════════════════════════════════════════════════════════════════════════
// Desicon Engineering — Logistics & Fleet Management Platform
// Azure Infrastructure — Bicep Template
//
// Provisions:
//   • Log Analytics Workspace + Application Insights
//   • Azure SQL Server + Database (Standard S0)
//   • Azure Storage Account + Blob containers
//   • Azure Key Vault (RBAC-enabled)
//   • App Service Plan (Linux B1) + App Service (.NET 8 API)
//   • Azure Static Web App (React frontend — Free tier)
//
// Deploy:
//   az group create --name rg-desicon-logistics-prod --location southafricanorth
//   az deployment group create \
//     --resource-group rg-desicon-logistics-prod \
//     --template-file infra/main.bicep \
//     --parameters infra/main.bicepparam
// ═══════════════════════════════════════════════════════════════════════════════

@description('Azure region for all resources')
param location string = 'southafricanorth'

@description('Environment label (prod | staging | dev)')
@allowed(['prod', 'staging', 'dev'])
param environment string = 'prod'

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@description('SQL Server administrator password (stored in Key Vault after deployment)')
@secure()
param sqlAdminPassword string

@description('Entra ID Tenant ID')
param entraTenantId string

@description('Entra ID API Application (Client) ID')
param entraApiClientId string

@description('Static Web App URL for CORS — update after first deployment')
param corsAllowedOrigin string = 'https://placeholder.azurestaticapps.net'

// ── Naming ─────────────────────────────────────────────────────────────────────
var suffix   = take(uniqueString(resourceGroup().id), 6)
var appBase  = 'deslogistics'
var appSvcPlanName     = 'asp-${appBase}-${environment}'
var apiAppName         = 'app-${appBase}-api-${environment}-${suffix}'
var sqlServerName      = 'sql-${appBase}-${environment}-${suffix}'
var sqlDbName          = 'sqldb-logistics'
var storageName        = 'stdeslog${environment}${suffix}'        // max 18 chars (Azure limit: 24, no hyphens)
var keyVaultName       = 'kv-deslog-${environment}-${suffix}'     // max 22 chars (Azure limit: 24)
var appInsightsName    = 'appi-${appBase}-${environment}'
var logAnalyticsName   = 'log-${appBase}-${environment}'
var staticWebAppName   = 'stapp-${appBase}-${environment}'

// ═══════════════════════════════════════════════════════════════════════════════
// LOG ANALYTICS WORKSPACE
// ═══════════════════════════════════════════════════════════════════════════════
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// APPLICATION INSIGHTS
// ═══════════════════════════════════════════════════════════════════════════════
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 30
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AZURE SQL SERVER + DATABASE
// ═══════════════════════════════════════════════════════════════════════════════
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow all Azure services (App Service, GitHub Actions migration runner) to connect
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'S0'        // 10 DTUs ~$15/month — upgrade to S1 (20 DTUs) if needed
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AZURE STORAGE ACCOUNT + BLOB CONTAINERS
// ═══════════════════════════════════════════════════════════════════════════════
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource receiptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'receipts'
  properties: { publicAccess: 'None' }
}

resource vehiclePhotosContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'vehicle-photos'
  properties: { publicAccess: 'None' }
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'documents'
  properties: { publicAccess: 'None' }
}

// ═══════════════════════════════════════════════════════════════════════════════
// KEY VAULT (RBAC-enabled — no access policies)
// ═══════════════════════════════════════════════════════════════════════════════
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: entraTenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// Store SQL admin password in Key Vault immediately
resource sqlPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-admin-password'
  properties: { value: sqlAdminPassword }
}

// ═══════════════════════════════════════════════════════════════════════════════
// APP SERVICE PLAN (Linux B1)
// ═══════════════════════════════════════════════════════════════════════════════
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appSvcPlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'       // 1 vCore, 1.75 GB RAM, ~$13/month
    tier: 'Basic'
  }
  properties: {
    reserved: true   // required for Linux plans
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// APP SERVICE — .NET 8 API
// ═══════════════════════════════════════════════════════════════════════════════
resource apiApp 'Microsoft.Web/sites@2023-01-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'SystemAssigned'   // Managed identity — grants Key Vault access below
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true          // Prevents cold starts (not available on Free tier)
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        // Runtime environment
        { name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production' }

        // Application Insights telemetry
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString }
        { name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3' }

        // Entra ID (non-secret — safe to store here)
        { name: 'EntraId__TenantId'
          value: entraTenantId }
        { name: 'EntraId__ClientId'
          value: entraApiClientId }
        #disable-next-line no-hardcoded-env-urls
        { name: 'EntraId__Instance'
          value: 'https://login.microsoftonline.com/' }
        { name: 'EntraId__Audience'
          value: 'api://${entraApiClientId}' }

        // CORS — Static Web App URL (update after first web deployment)
        { name: 'Cors__AllowedOrigins__0'
          value: corsAllowedOrigin }

        // Disable demo seeding in production
        { name: 'Demo__SeedOnStartup'
          value: 'false' }

        // Feature flags
        { name: 'Features__MaintenanceReminders', value: 'true' }
        { name: 'Features__AutoAssignment',       value: 'true' }
        { name: 'Features__FuelTracking',         value: 'true' }
        { name: 'Features__AuditLogging',         value: 'true' }
      ]
    }
  }
}

// Grant App Service managed identity "Key Vault Secrets User" on Key Vault
// so it can read secrets at runtime using Key Vault references
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiApp.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AZURE STATIC WEB APP (React / Vite frontend — Free tier)
// ═══════════════════════════════════════════════════════════════════════════════
// Note: Static Web Apps are only available in a subset of regions.
// We use westeurope as it supports the Free tier globally.
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: 'westeurope'
  sku: { name: 'Free', tier: 'Free' }
  properties: {
    // We supply our own GitHub Actions workflow — skip auto-generation
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// OUTPUTS — captured by post-deploy-setup.sh and DEPLOYMENT.md steps
// ═══════════════════════════════════════════════════════════════════════════════
output apiAppName              string = apiApp.name
output apiUrl                  string = 'https://${apiApp.properties.defaultHostName}'
output sqlServerFqdn           string = sqlServer.properties.fullyQualifiedDomainName
output sqlDbName               string = sqlDbName
output storageAccountName      string = storageAccount.name
output keyVaultName            string = keyVault.name
output keyVaultUri             string = keyVault.properties.vaultUri
output appInsightsConnStr      string = appInsights.properties.ConnectionString
output staticWebAppName        string = staticWebApp.name
output staticWebAppUrl         string = 'https://${staticWebApp.properties.defaultHostname}'
// staticWebAppDeployToken: fetch after deployment with:
//   az staticwebapp secrets list --name <staticWebAppName> --query properties.apiKey -o tsv
