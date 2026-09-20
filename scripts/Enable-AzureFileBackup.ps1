<#
.SYNOPSIS
  Enables Azure Backup protection for the CarehomeSystem document storage file share.

.DESCRIPTION
  Run once after Bicep deploy when enableDocumentBackup=true.
  Registers the storage account with the Recovery Services Vault and applies the daily policy.

.EXAMPLE
  .\scripts\Enable-AzureFileBackup.ps1 -ResourceGroup rg-carehome -RecoveryVaultName carehomepilotrsv -StorageAccountName carehomepilotdocs
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$RecoveryVaultName,

    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,

    [string]$DocumentsShareName = 'carehome-documents',

    [string]$BackupPolicyName = 'DailyAzureFiles'
)

$ErrorActionPreference = 'Stop'

$az = Get-Command az -ErrorAction SilentlyContinue
if (-not $az) {
    throw "Azure CLI (az) not found. Install: winget install --exact --id Microsoft.AzureCLI"
}

Write-Host "Enabling Azure Files backup..."
Write-Host "  Vault:   $RecoveryVaultName"
Write-Host "  Storage: $StorageAccountName / $DocumentsShareName"
Write-Host "  Policy:  $BackupPolicyName"

az backup protection enable-for-azurefileshare `
    --resource-group $ResourceGroup `
    --vault-name $RecoveryVaultName `
    --storage-account $StorageAccountName `
    --azure-file-share $DocumentsShareName `
    --policy-name $BackupPolicyName `
    --output none

Write-Host "Azure Files backup enabled. First backup may take up to 24 hours."
Write-Host "Verify with: .\scripts\Verify-BackupReadiness.ps1 -ResourceGroup $ResourceGroup -SqlServerName <server> -RecoveryVaultName $RecoveryVaultName"
