# Desicon Engineering — Logistics Platform: Azure Deployment Guide

## Overview

This guide takes the platform from local Docker to production on Azure. Follow the phases in order — each one builds on the previous.

**What gets deployed:**

| Component | Azure Service | Cost (est.) |
|-----------|--------------|-------------|
| .NET 8 API | App Service (Linux B1) | ~$13/month |
| React Frontend | Static Web App (Free) | Free |
| Database | Azure SQL S0 | ~$15/month |
| Storage (receipts, docs) | Storage Account LRS | ~$1/month |
| Secrets | Key Vault Standard | ~$1/month |
| Monitoring | App Insights + Log Analytics | ~$2/month |
| **Total** | | **~$32/month** |

---

## Prerequisites

Install these tools on your machine if not already present:

```bash
# Azure CLI (Windows — run in PowerShell as Admin)
winget install Microsoft.AzureCLI

# Verify installation
az --version      # should show 2.x.x
git --version
```

Log in to Azure:
```bash
az login
az account show   # confirm you're on the correct subscription
az account list   # if you have multiple — pick the right one
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

---

## Phase 1: Entra ID App Registration

Your API registration already exists (`1b657901-dc12-4927-b9a1-2889fd021c1f`). You need to create a **separate** registration for the React SPA, then add production redirect URIs.

### 1a — Create SPA App Registration

1. Go to [portal.azure.com](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**
2. Name: `Desicon Logistics Web`
3. Supported account types: **Accounts in this organizational directory only**
4. Redirect URI: select **Single-page application (SPA)** — leave URI blank for now
5. Click **Register**
6. **Copy the Application (client) ID** — you'll need this as `VITE_ENTRA_CLIENT_ID`

### 1b — Expose the API scope (on the API registration)

1. Go to App registrations → **Desicon Logistics API** (client ID `1b657901-...`)
2. **Expose an API** → if Application ID URI is not set, click **Add** (accept default `api://1b657901-...`)
3. **Add a scope** → Scope name: `Logistics.Access` → Admin consent display name: `Access Logistics API` → **Add scope**

### 1c — Grant the Web App permission to call the API

1. Go to the **Desicon Logistics Web** registration you just created
2. **API permissions** → **Add a permission** → **My APIs** → **Desicon Logistics API**
3. Select `Logistics.Access` → **Add permissions**
4. Click **Grant admin consent for Desicon Engineering**

---

## Phase 2: Provision Azure Infrastructure

### 2a — Create Resource Group

```bash
az group create \
  --name rg-desicon-logistics-prod \
  --location southafricanorth
```

> **Why South Africa North?** It's the closest Azure region to West Africa with full service support. Alternative: `uksouth` (slightly more services available).

### 2b — Fill in parameters

Open `infra/main.bicepparam` and fill in:

```
param sqlAdminPassword = 'REPLACE_WITH_STRONG_PASSWORD'
```

Use a strong password: minimum 12 characters, with uppercase, lowercase, number, and symbol.  
Example format: `Des!c0n-L0g!st!cs#2026`

> **Important:** Do NOT commit this file to GitHub with the real password in it. Either replace the password each time before deployment or use `--parameters` flags on the CLI.

### 2c — Deploy Bicep template

```bash
# From the project root:
az deployment group create \
  --resource-group rg-desicon-logistics-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --name main
```

This takes approximately 5–10 minutes. When complete, capture the outputs:

```bash
az deployment group show \
  --resource-group rg-desicon-logistics-prod \
  --name main \
  --query properties.outputs \
  --output table
```

You'll see:
- `apiAppName` — the App Service name (looks like `app-deslogistics-api-prod-abc123`)
- `apiUrl` — the API URL
- `sqlServerFqdn` — the SQL server hostname
- `storageAccountName` — the storage account name
- `keyVaultName` — the Key Vault name
- `staticWebAppUrl` — the frontend URL (placeholder hostname until first deploy)
- `staticWebAppDeployToken` — **copy and save this** — needed for GitHub Actions

---

## Phase 3: Post-Deployment Configuration

### 3a — Run the setup script

Open `scripts/post-deploy-setup.sh` and fill in the values from the Bicep outputs above:

```bash
API_APP_NAME=""             # from Bicep output: apiAppName
SQL_SERVER_FQDN=""          # from Bicep output: sqlServerFqdn
SQL_ADMIN_PASSWORD=""       # the password you put in main.bicepparam
STORAGE_ACCOUNT_NAME=""     # from Bicep output: storageAccountName
KEY_VAULT_NAME=""           # from Bicep output: keyVaultName
STATIC_WEB_APP_URL=""       # leave blank for now — fill in after web deploys
```

Then run it (in Git Bash or WSL):

```bash
chmod +x scripts/post-deploy-setup.sh
./scripts/post-deploy-setup.sh
```

This sets the connection strings on App Service and stores secrets in Key Vault.

---

## Phase 4: GitHub Actions Setup

GitHub Actions will automatically build and deploy the platform whenever you push to `main`.

### 4a — Create Azure Service Principal

This gives GitHub Actions permission to deploy to your Azure subscription:

```bash
az ad sp create-for-rbac \
  --name "sp-desicon-logistics-github" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-desicon-logistics-prod \
  --sdk-auth
```

Copy the **entire JSON output** — you'll need it in the next step.

To find your subscription ID:
```bash
az account show --query id --output tsv
```

### 4b — Add GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.

Add these secrets:

| Secret Name | Value |
|-------------|-------|
| `AZURE_CREDENTIALS` | The full JSON from `az ad sp create-for-rbac` above |
| `AZURE_APP_SERVICE_NAME` | App Service name from Bicep output (e.g. `app-deslogistics-api-prod-abc123`) |
| `AZURE_RESOURCE_GROUP` | `rg-desicon-logistics-prod` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | The `staticWebAppDeployToken` from Bicep output |
| `VITE_API_BASE_URL` | `https://YOUR_APP_SERVICE_NAME.azurewebsites.net/api` |
| `VITE_ENTRA_CLIENT_ID` | Client ID of the **Web** app registration (from Phase 1a) |
| `VITE_ENTRA_TENANT_ID` | `77e479f1-fefd-4238-a1a9-6b1f692f20b8` |
| `VITE_ENTRA_API_SCOPE` | `api://1b657901-dc12-4927-b9a1-2889fd021c1f/Logistics.Access` |

---

## Phase 5: First Deployment

### 5a — Trigger deployment

Push to the `main` branch (or trigger manually in GitHub Actions UI):

```bash
git add -A
git commit -m "feat: Azure deployment configuration"
git push
```

GitHub Actions will run two workflows:
- **Deploy API** → builds .NET 8, deploys to App Service (runs EF migrations on startup)
- **Deploy Web** → builds React with production env vars, deploys to Static Web App

Watch progress at: `https://github.com/YOUR_ORG/YOUR_REPO/actions`

### 5b — Verify API

```bash
curl https://YOUR_APP_SERVICE_NAME.azurewebsites.net/health
# Should return: Healthy
```

### 5c — Update Entra ID redirect URIs

Once the Static Web App is deployed, you'll have a real URL (e.g. `https://wonderful-sea-0abc123.azurestaticapps.net`).

1. Go to **Desicon Logistics Web** app registration
2. **Authentication** → **Single-page application** → **Add URI**
3. Add: `https://YOUR_STATIC_WEB_APP_URL` (no trailing slash)
4. Add: `https://YOUR_STATIC_WEB_APP_URL/` (with trailing slash — some MSAL versions need this)
5. **Save**

### 5d — Update CORS with real Static Web App URL

Re-run the setup script with `STATIC_WEB_APP_URL` filled in, OR run this directly:

```bash
az webapp config appsettings set \
  --resource-group rg-desicon-logistics-prod \
  --name YOUR_APP_SERVICE_NAME \
  --settings "Cors__AllowedOrigins__0=https://YOUR_STATIC_WEB_APP_URL"

az webapp restart \
  --resource-group rg-desicon-logistics-prod \
  --name YOUR_APP_SERVICE_NAME
```

---

## Phase 6: First Login and User Setup

### 6a — First login

Open the Static Web App URL in your browser. You'll be redirected to Microsoft login.

Sign in with your **Desicon Engineering Microsoft 365 account**.

On first login, the platform creates your user record automatically via the `/api/auth/me` endpoint and assigns a default role.

### 6b — Assign roles via database

Roles are stored in the `Users` table. Until a role management UI is built, assign roles directly in Azure SQL:

1. Go to [portal.azure.com](https://portal.azure.com) → **SQL databases** → `sqldb-logistics`
2. **Query editor** (left menu) → log in with your SQL admin credentials
3. Run:

```sql
-- View all registered users
SELECT Id, FullName, Role, EntraObjectId, IsActive, CreatedAt
FROM Users
ORDER BY CreatedAt;

-- Set a user as Admin (replace the GUID with the user's Id)
UPDATE Users SET Role = 'Admin' WHERE FullName = 'YOUR NAME';

-- Assign roles to other users
UPDATE Users SET Role = 'Manager'     WHERE FullName = 'MANAGER NAME';
UPDATE Users SET Role = 'Coordinator' WHERE FullName = 'COORDINATOR NAME';
UPDATE Users SET Role = 'Driver'      WHERE FullName = 'DRIVER NAME';
UPDATE Users SET Role = 'Mechanic'    WHERE FullName = 'MECHANIC NAME';
```

### 6c — Pre-register drivers

Drivers need to be pre-registered from the **Drivers** page before they can sign in. Have a Manager log in and add each driver (name, phone, licence number).

---

## Post-Go-Live Checklist

- [ ] API health endpoint returns 200: `GET /health`
- [ ] Login works with a real Microsoft 365 account
- [ ] Dashboard shows data after first trip/vehicle entry
- [ ] At least one Admin user set in the database
- [ ] Drivers pre-registered in the system
- [ ] CORS is correctly set to the Static Web App URL
- [ ] Static Web App redirect URI is in the Entra app registration
- [ ] No `SeedOnStartup` in production (check `Demo:SeedOnStartup = false`)
- [ ] Application Insights is receiving telemetry (check Azure Portal → App Insights → Live Metrics)

---

## Ongoing Deployments

After initial setup, every push to `main` automatically:
1. Builds and deploys the API (if `src/api/**` changed)
2. Builds and deploys the React frontend (if `src/web/**` changed)

No manual steps needed — GitHub Actions handles everything.

---

## Scaling Up

When the platform is being used actively, you may want to upgrade:

| Resource | Current | Upgrade to | When |
|----------|---------|-----------|------|
| App Service | B1 (1 vCore) | B2 or S1 | Response time > 2s |
| SQL Database | S0 (10 DTU) | S1 (20 DTU) | Query timeouts |
| Log retention | 30 days | 90 days | Compliance requirement |

To upgrade App Service plan:
```bash
az appservice plan update \
  --resource-group rg-desicon-logistics-prod \
  --name asp-deslogistics-prod \
  --sku B2
```

---

## Troubleshooting

**API returns 401 Unauthorized**
- Check Entra ID app registration: API permissions, admin consent granted
- Verify `EntraId__TenantId` and `EntraId__ClientId` in App Service config

**API returns 500 on startup**
- Check App Service Log stream (portal → App Service → Log stream)
- Most common cause: SQL connection string wrong or SQL firewall blocking

**React app shows "AADSTS" error**
- The Static Web App URL is not in the Entra redirect URIs → add it (Phase 5c)

**CORS errors in browser console**
- `Cors__AllowedOrigins__0` on App Service doesn't match the Static Web App URL
- Update it and restart the App Service (Phase 5d)

**GitHub Actions deploy fails**
- Check AZURE_CREDENTIALS secret is the full JSON (not just the clientId)
- Check the service principal has Contributor on the resource group
