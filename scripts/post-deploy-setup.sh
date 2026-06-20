#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# Desicon Logistics Platform — Post-Deployment Setup Script
#
# Run this ONCE after the Bicep deployment to:
#   1. Set the SQL connection string on App Service (never stored in code)
#   2. Set the Storage connection string on App Service
#   3. Store secrets in Key Vault
#   4. Update CORS once the Static Web App URL is known
#
# Prerequisites:
#   • Azure CLI installed and logged in (az login)
#   • Bicep deployment already completed (infra/main.bicep)
#   • jq installed (brew install jq / apt-get install jq)
#
# Usage:
#   chmod +x scripts/post-deploy-setup.sh
#   ./scripts/post-deploy-setup.sh
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Configuration — pre-filled from Bicep deployment outputs ──────────────────
RESOURCE_GROUP="rg-desicon-logistics-prod"
SUBSCRIPTION_ID=""          # Leave blank to use default subscription

API_APP_NAME="app-deslogistics-api-prod-jk4s6b"
SQL_SERVER_FQDN="sql-deslogistics-prod-jk4s6b.database.windows.net"
SQL_DB_NAME="sqldb-logistics"
SQL_ADMIN_LOGIN="sqladmin"
SQL_ADMIN_PASSWORD="Des!c0n-L0g!5t!cs#2026"       # ← FILL THIS IN: the password from main.bicepparam
STORAGE_ACCOUNT_NAME="stdeslogprodjk4s6b"
KEY_VAULT_NAME="kv-deslog-prod-jk4s6b"
STATIC_WEB_APP_URL=""       # Leave blank — fill in after GitHub Actions deploys the frontend

# ── Colour output ──────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()    { echo -e "${GREEN}[INFO]${NC} $1"; }
warn()    { echo -e "${YELLOW}[WARN]${NC} $1"; }
error()   { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# ── Validate required values ───────────────────────────────────────────────────
[[ -z "$API_APP_NAME" ]]        && error "Set API_APP_NAME at the top of this script"
[[ -z "$SQL_SERVER_FQDN" ]]     && error "Set SQL_SERVER_FQDN at the top of this script"
[[ -z "$SQL_ADMIN_PASSWORD" ]]  && error "Set SQL_ADMIN_PASSWORD at the top of this script"
[[ -z "$STORAGE_ACCOUNT_NAME" ]] && error "Set STORAGE_ACCOUNT_NAME at the top of this script"
[[ -z "$KEY_VAULT_NAME" ]]      && error "Set KEY_VAULT_NAME at the top of this script"

info "Starting post-deployment configuration for: $API_APP_NAME"

# ── Set subscription (optional) ───────────────────────────────────────────────
if [[ -n "$SUBSCRIPTION_ID" ]]; then
  info "Setting subscription: $SUBSCRIPTION_ID"
  az account set --subscription "$SUBSCRIPTION_ID"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 1: Store SQL password in Key Vault
# ═══════════════════════════════════════════════════════════════════════════════
info "Storing SQL password in Key Vault: $KEY_VAULT_NAME"
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "sql-admin-password" \
  --value "$SQL_ADMIN_PASSWORD" \
  --output none

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 2: Get Storage Account connection string and store in Key Vault
# ═══════════════════════════════════════════════════════════════════════════════
info "Retrieving Storage Account connection string..."
STORAGE_CONN_STR=$(az storage account show-connection-string \
  --resource-group "$RESOURCE_GROUP" \
  --name "$STORAGE_ACCOUNT_NAME" \
  --query connectionString \
  --output tsv)

info "Storing Storage connection string in Key Vault..."
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "storage-connection-string" \
  --value "$STORAGE_CONN_STR" \
  --output none

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 3: Set App Service configuration (connection strings as app settings)
# ═══════════════════════════════════════════════════════════════════════════════
info "Setting App Service configuration..."

SQL_CONN_STR="Server=tcp:${SQL_SERVER_FQDN},1433;Initial Catalog=${SQL_DB_NAME};Persist Security Info=False;User ID=${SQL_ADMIN_LOGIN};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$API_APP_NAME" \
  --settings \
    "Sql__ConnectionString=${SQL_CONN_STR}" \
    "Storage__ConnectionString=${STORAGE_CONN_STR}" \
  --output none

info "Connection strings configured on App Service"

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 4: Update CORS with Static Web App URL (if provided)
# ═══════════════════════════════════════════════════════════════════════════════
if [[ -n "$STATIC_WEB_APP_URL" ]]; then
  info "Updating CORS allowed origin to: $STATIC_WEB_APP_URL"
  az webapp config appsettings set \
    --resource-group "$RESOURCE_GROUP" \
    --name "$API_APP_NAME" \
    --settings "Cors__AllowedOrigins__0=${STATIC_WEB_APP_URL}" \
    --output none
  info "CORS updated"
else
  warn "STATIC_WEB_APP_URL not set — CORS will use placeholder. Re-run after web deployment."
fi

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 5: Restart App Service to pick up new settings
# ═══════════════════════════════════════════════════════════════════════════════
info "Restarting App Service to apply configuration..."
az webapp restart \
  --resource-group "$RESOURCE_GROUP" \
  --name "$API_APP_NAME" \
  --output none

info "Waiting 30 seconds for restart to complete..."
sleep 30

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 6: Verify health endpoint
# ═══════════════════════════════════════════════════════════════════════════════
API_URL="https://${API_APP_NAME}.azurewebsites.net/health"
info "Verifying API health: $API_URL"

HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL" || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
  echo -e "${GREEN}✓ API is healthy (HTTP 200)${NC}"
else
  warn "Health check returned HTTP $HTTP_STATUS — API may still be starting up."
  warn "Check Azure Portal > App Service > Log stream for details."
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════════════════════════════════════
echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Post-deployment setup complete!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════${NC}"
echo ""
echo "  API URL:    https://${API_APP_NAME}.azurewebsites.net"
echo "  Health:     https://${API_APP_NAME}.azurewebsites.net/health"
if [[ -n "$STATIC_WEB_APP_URL" ]]; then
  echo "  Frontend:   $STATIC_WEB_APP_URL"
fi
echo ""
echo "Next steps:"
echo "  1. Add GitHub Secrets for CI/CD (see DEPLOYMENT.md)"
echo "  2. Push to main branch to trigger first deployment"
echo "  3. Update Entra ID app registration with production redirect URIs"
echo ""
