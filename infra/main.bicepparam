// ═══════════════════════════════════════════════════════════════════════════════
// Desicon Engineering — Logistics Platform — Production Parameters
//
// INSTRUCTIONS:
//   1. Fill in your values below (replace all REPLACE_WITH_* placeholders)
//   2. NEVER commit this file to git with real passwords — add to .gitignore
//      or use `az deployment group create --parameters` flags instead
//
// Deploy:
//   az deployment group create \
//     --resource-group rg-desicon-logistics-prod \
//     --template-file infra/main.bicep \
//     --parameters infra/main.bicepparam
// ═══════════════════════════════════════════════════════════════════════════════

using 'main.bicep'

// Azure region — South Africa North is closest to Ghana/West Africa
// Alternatives: westeurope, uksouth, eastus
param location = 'southafricanorth'

param environment = 'prod'

// SQL Server administrator credentials
// Password must be 8–128 chars, mix of upper/lower/digit/symbol
param sqlAdminLogin    = 'sqladmin'
param sqlAdminPassword = 'Des!c0n-L0g!5t!cs#2026'

// Your Entra ID (Azure AD) Tenant ID — already in appsettings.json
param entraTenantId    = '77e479f1-fefd-4238-a1a9-6b1f692f20b8'

// API App Registration Client ID — already in appsettings.json
param entraApiClientId = '1b657901-dc12-4927-b9a1-2889fd021c1f'

// Leave as placeholder until after first deployment — update with real SWA URL
// then re-run: az deployment group create ... to update CORS
param corsAllowedOrigin = 'https://REPLACE_AFTER_FIRST_DEPLOY.azurestaticapps.net'
