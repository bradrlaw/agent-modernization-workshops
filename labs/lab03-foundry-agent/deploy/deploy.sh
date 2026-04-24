#!/bin/bash
# Deploy the Virtual Banking Assistant to Azure Container Apps
#
# Prerequisites:
#   - Azure CLI (az) installed and logged in
#   - Docker installed (for local build)
#   - An Azure Container Registry (ACR)
#
# Usage:
#   chmod +x deploy.sh
#   ./deploy.sh

set -euo pipefail

# --- Configuration (edit these) ---
RESOURCE_GROUP="rg-banking-agent"
LOCATION="eastus2"
ACR_NAME="acrbankingagent"
APP_NAME="banking-assistant"
ENVIRONMENT_NAME="banking-agent-env"
IMAGE_TAG="latest"

echo "=== Step 1: Create resource group ==="
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "=== Step 2: Create Azure Container Registry ==="
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic --admin-enabled true

echo "=== Step 3: Build and push container image ==="
cd "$(dirname "$0")/.."
az acr build --registry $ACR_NAME --image banking-assistant:$IMAGE_TAG --file deploy/Dockerfile .

echo "=== Step 4: Create Container Apps environment ==="
az containerapp env create \
    --name $ENVIRONMENT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION

echo "=== Step 5: Deploy container app ==="
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv)

az containerapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $ENVIRONMENT_NAME \
    --image "$ACR_LOGIN_SERVER/banking-assistant:$IMAGE_TAG" \
    --target-port 5000 \
    --ingress external \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password "$ACR_PASSWORD" \
    --env-vars \
        PROJECT_ENDPOINT=secretref:project-endpoint \
        MODEL_DEPLOYMENT_NAME=gpt-4o \
    --min-replicas 0 \
    --max-replicas 3

echo ""
echo "=== Deployment complete! ==="
FQDN=$(az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv)
echo "App URL: https://$FQDN"
echo ""
echo "Next steps:"
echo "  1. Set the PROJECT_ENDPOINT secret:"
echo "     az containerapp secret set --name $APP_NAME --resource-group $RESOURCE_GROUP --secrets project-endpoint='your-endpoint-url'"
echo "  2. Configure managed identity for Azure AI Foundry access"
echo "  3. Restart the app: az containerapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP"
