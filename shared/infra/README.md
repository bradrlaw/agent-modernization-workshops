# Shared Infrastructure

Bicep and Terraform templates for provisioning shared Azure resources used across labs.

## Contents

<!-- TODO: Add templates as labs are developed -->

- Azure AI Foundry project
- Azure AI Search index
- Application Insights workspace
- Key Vault
- Managed identities

## Usage

```bash
# Example: Deploy shared resources with Azure CLI + Bicep
az deployment group create \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters @parameters.json
```
