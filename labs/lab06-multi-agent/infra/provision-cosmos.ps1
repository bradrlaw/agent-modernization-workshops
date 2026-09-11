<#
.SYNOPSIS
  Provisions the Azure Cosmos DB backing store for the Lab 06 "--memory cosmos" tier.

.DESCRIPTION
  Creates a SERVERLESS Cosmos DB (NoSQL) account, a database, and a container partitioned by
  /userId — one document per customer, so a customer's whole memory is a single point read.

  It then grants the chosen identity the Cosmos DB **Built-in Data Contributor** role. This is the
  DATA-PLANE role and it is the part everyone forgets: control-plane roles (Owner/Contributor) let
  you manage the account but do NOT grant read/write to documents. Without this assignment a keyless
  app (DefaultAzureCredential) gets a silent 403 on the first read/write.

  Auth is keyless end to end — the app never needs a key. For a no-Azure alternative use the local
  Cosmos DB emulator instead (set COSMOS_CONNECTION_STRING); see README Part E.

.EXAMPLE
  ./provision-cosmos.ps1 -ResourceGroup rg-agent-memory -AccountName myteam-agentmem-dev -Location eastus

.NOTES
  Requires the Azure CLI (az) and an authenticated session (az login) in the target subscription.
  Tear everything down with:  az group delete --name <ResourceGroup>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,

    # Cosmos account names are GLOBALLY unique, 3-44 chars, lowercase letters/digits/hyphens.
    [Parameter(Mandatory = $true)] [string] $AccountName,

    [string] $Location      = "eastus",
    [string] $DatabaseName  = "agentmemory",
    [string] $ContainerName = "conversations",

    # Identity to grant data access to. Defaults to the signed-in user (great for a local demo).
    # For an app/CI, pass the managed identity or service principal object (principal) id.
    [string] $PrincipalId
)

$ErrorActionPreference = "Stop"
# Make native (az) non-zero exit codes terminate the script (PowerShell 7.3+), so a failed step
# doesn't cascade into confusing follow-on errors.
$PSNativeCommandUseErrorActionPreference = $true

function Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

if ((az group exists --name $ResourceGroup) -eq 'true') {
    Step "Resource group '$ResourceGroup' already exists — reusing it"
}
else {
    Step "Creating resource group '$ResourceGroup' in '$Location'"
    az group create --name $ResourceGroup --location $Location --output none
}

Step "Creating SERVERLESS Cosmos DB account '$AccountName' (this can take a few minutes)"
# --capabilities EnableServerless => pay-per-request, no throughput to manage. Do NOT pass
# throughput flags meant for provisioned accounts.
az cosmosdb create `
    --name $AccountName `
    --resource-group $ResourceGroup `
    --locations "regionName=$Location" `
    --capabilities EnableServerless `
    --output none

Step "Creating database '$DatabaseName'"
az cosmosdb sql database create `
    --account-name $AccountName `
    --resource-group $ResourceGroup `
    --name $DatabaseName `
    --output none

Step "Creating container '$ContainerName' partitioned by /userId"
# Serverless: DO NOT pass --throughput; the account bills per request. Partition on /userId so each
# customer's record lives in one logical partition (matches CosmosStore's point read/upsert by userId).
az cosmosdb sql container create `
    --account-name $AccountName `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --name $ContainerName `
    --partition-key-path "/userId" `
    --output none

if (-not $PrincipalId) {
    Step "Resolving signed-in user object id (no -PrincipalId supplied)"
    $PrincipalId = az ad signed-in-user show --query id --output tsv
}

Step "Assigning Cosmos DB Built-in Data Contributor (DATA-PLANE) to principal $PrincipalId"
# 00000000-0000-0000-0000-000000000002 = Cosmos DB Built-in Data Contributor (a fixed, well-known id).
# Scope "/" = the whole account. THIS is what makes keyless DefaultAzureCredential able to read/write.
az cosmosdb sql role assignment create `
    --account-name $AccountName `
    --resource-group $ResourceGroup `
    --role-definition-id "00000000-0000-0000-0000-000000000002" `
    --principal-id $PrincipalId `
    --scope "/" `
    --output none

$endpoint = az cosmosdb show --name $AccountName --resource-group $ResourceGroup --query documentEndpoint --output tsv

Write-Host ""
Write-Host "Done. Cosmos DB is ready for keyless '--memory cosmos'." -ForegroundColor Green
Write-Host "Set these (or put them in src-dotnet/BankingConcierge/.env):" -ForegroundColor Green
Write-Host ""
Write-Host "  `$env:COSMOS_ENDPOINT  = `"$endpoint`""
Write-Host "  `$env:COSMOS_DATABASE  = `"$DatabaseName`""
Write-Host "  `$env:COSMOS_CONTAINER = `"$ContainerName`""
Write-Host ""
Write-Host "Then: dotnet run -- --pattern sequential --memory cosmos --customer CUST-2001 --task '...'"
Write-Host "Note: a fresh data-plane role assignment can take a minute or two to propagate." -ForegroundColor Yellow
