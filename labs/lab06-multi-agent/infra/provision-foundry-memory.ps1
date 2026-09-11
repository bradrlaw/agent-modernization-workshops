<#
.SYNOPSIS
  Prepare an Azure AI Foundry project for the Lab 06 "--memory foundry" (platform-managed memory) demo.

.DESCRIPTION
  Foundry-managed Memory extracts, embeds, and recalls memories SERVER-SIDE. For that to work, the
  Foundry service must be able to CALL your chat and embedding deployments — and it does so as the
  system-assigned managed identity of the AI Services ACCOUNT and its PROJECT (not your user token).
  If those identities lack model access you get: HTTP 401 "Authentication to the Azure OpenAI resource
  failed" while the service is extracting memories.

  This script (idempotent, customer-neutral):
    1. Confirms a chat and an embedding deployment exist on the account.
    2. Grants BOTH managed identities the "Cognitive Services OpenAI User" DATA-PLANE role on the account.

  Prerequisites: `az login` to the subscription that holds the Foundry resource; Foundry
  "Memory (preview)" enabled on the project; permission to create role assignments (Owner or
  User Access Administrator on the account).

.EXAMPLE
  ./infra/provision-foundry-memory.ps1 -Account my-ai-resource -ResourceGroup my-rg -Project my-project
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Account,
    [Parameter(Mandatory)][string]$ResourceGroup,
    [string]$Project,
    [string]$EmbeddingDeployment = "text-embedding-3-small",
    [string]$Role = "Cognitive Services OpenAI User"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Write-Host "==> Resolving account '$Account' in resource group '$ResourceGroup'..."
$acctId = az cognitiveservices account show -n $Account -g $ResourceGroup --query id -o tsv
if (-not $acctId) { throw "Account '$Account' not found in resource group '$ResourceGroup'." }

Write-Host "==> Checking model deployments..."
$raw = az cognitiveservices account deployment list -n $Account -g $ResourceGroup --query "[].name" -o tsv
$deployNames = @($raw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
Write-Host "    Deployments: $($deployNames -join ', ')"
if ($deployNames -notcontains $EmbeddingDeployment) {
    Write-Warning ("No deployment named '$EmbeddingDeployment'. Foundry-managed Memory needs an EMBEDDING " +
        "deployment for vector recall — create one (e.g. text-embedding-3-small) or pass -EmbeddingDeployment <name>.")
}

# Gather the identities that the memory service authenticates as: the account MI + the project MI.
$principals = @()
$acctMi = az cognitiveservices account show -n $Account -g $ResourceGroup --query identity.principalId -o tsv
if ($acctMi) {
    $principals += [pscustomobject]@{ Name = "account"; Id = $acctMi }
} else {
    Write-Warning "Account has no system-assigned managed identity. Enable one:`n  az cognitiveservices account identity assign -n $Account -g $ResourceGroup"
}

if ($Project) {
    $projMi = az resource show --ids "$acctId/projects/$Project" --query identity.principalId -o tsv 2>$null
    if ($projMi) {
        $principals += [pscustomobject]@{ Name = "project/$Project"; Id = $projMi }
    } else {
        Write-Warning "Project '$Project' has no system-assigned managed identity (or was not found)."
    }
}

if (-not $principals) { throw "No managed identities found to grant. Enable a system-assigned identity first." }

foreach ($p in $principals) {
    $count = az role assignment list --scope $acctId --assignee $p.Id --query "[?roleDefinitionName=='$Role'] | length(@)" -o tsv
    if ($count -and [int]$count -gt 0) {
        Write-Host "==> $($p.Name) identity already has '$Role'. Skipping."
    } else {
        Write-Host "==> Granting '$Role' to $($p.Name) identity ($($p.Id))..."
        az role assignment create --assignee-object-id $p.Id --assignee-principal-type ServicePrincipal --role $Role --scope $acctId | Out-Null
    }
}

Write-Host ""
Write-Host "Done. The data-plane role can take a few minutes to propagate."
Write-Host "Next: set these (or put them in src-dotnet/.env) and run --memory foundry:"
Write-Host "  FOUNDRY_PROJECT_ENDPOINT           = <your project endpoint>"
Write-Host "  FOUNDRY_MODEL                      = <your chat deployment, e.g. gpt-4o>"
Write-Host "  AZURE_AI_EMBEDDING_DEPLOYMENT_NAME = $EmbeddingDeployment"
Write-Host "  AZURE_AI_MEMORY_STORE_ID          = lab06-agent-memory   (any name; created on first run)"
