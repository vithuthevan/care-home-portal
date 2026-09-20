@description('CarehomeSystem Azure: App Service + Azure SQL + Key Vault (same-origin SPA+API)')
param location string = resourceGroup().location
param appName string
param sqlServerName string
param sqlAdminLogin string
@secure()
param sqlAdminPassword string
param sqlDatabaseName string = 'CareHome'
param appServiceSku string = 'B1'
param appServiceSkuTier string = 'Basic'
@description('SQL database SKU. Standard S0+ required for configurable PITR (7-35 days) and long-term retention. Basic is fixed at 7 days PITR.')
param sqlDatabaseSku string = 'S0'
@description('SQL database tier. Use Standard (not Basic) for production backup retention policies.')
param sqlDatabaseSkuTier string = 'Standard'
@description('Point-in-time restore retention in days (7-35). Ignored on Basic tier (fixed 7 days).')
@minValue(7)
@maxValue(35)
param sqlBackupRetentionDays int = 35
@description('Enable long-term retention (weekly/monthly/yearly). Requires Standard tier or higher.')
param enableLongTermRetention bool = true
@description('Long-term weekly retention in weeks.')
@minValue(1)
@maxValue(520)
param ltrWeeklyRetentionWeeks int = 4
@description('Provision Recovery Services Vault and daily Azure Files backup policy for document storage.')
param enableDocumentBackup bool = true
@description('Document backup retention in days (Azure Files backup policy).')
@minValue(1)
@maxValue(365)
param documentBackupRetentionDays int = 30
@description('Object ID of the principal running deployment (for Key Vault secret writes). Leave empty to skip deployer RBAC.')
param deployerObjectId string = ''

var webAppName = appName
var planName = '${appName}-plan'
var documentsShareName = 'carehome-documents'
var keyVaultName = take('${replace(appName, '-', '')}kv', 24)
var recoveryVaultName = take('${replace(appName, '-', '')}rsv', 50)
var keyVaultSecretsUserRoleId = '46334508-882d-41db-b097-3ea7158e9678'
var keyVaultSecretsOfficerRoleId = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
var isStandardOrAbove = sqlDatabaseSkuTier != 'Basic'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: sqlDatabaseSku
    tier: sqlDatabaseSkuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    shortTermRetentionPolicy: isStandardOrAbove ? {
      retentionDays: sqlBackupRetentionDays
      backupIntervalInHours: 12
    } : {}
    longTermRetentionPolicy: (isStandardOrAbove && enableLongTermRetention) ? {
      weeklyRetention: 'P${ltrWeeklyRetentionWeeks}W'
      monthlyRetention: 'P12M'
      yearlyRetention: 'P5Y'
      weekOfYear: 1
    } : {}
  }
}

resource recoveryVault 'Microsoft.RecoveryServices/vaults@2024-04-01' = if (enableDocumentBackup) {
  name: recoveryVaultName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

resource documentBackupPolicy 'Microsoft.RecoveryServices/vaults/backupPolicies@2024-04-01' = if (enableDocumentBackup) {
  parent: recoveryVault
  name: 'DailyAzureFiles'
  properties: {
    backupManagementType: 'AzureStorage'
    workloadType: 'AzureFileShare'
    schedulePolicy: {
      schedulePolicyType: 'SimpleSchedulePolicy'
      scheduleRunFrequency: 'Daily'
      scheduleRunTimes: [
        '02:00'
      ]
    }
    retentionPolicy: {
      retentionPolicyType: 'SimpleRetentionPolicy'
      retentionDuration: 'P${documentBackupRetentionDays}D'
    }
    timeZone: 'UTC'
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: appServiceSku
    tier: appServiceSkuTier
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('${replace(appName, '-', '')}docs', 24)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource documentsShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: documentsShareName
  properties: {
    accessTier: 'TransactionOptimized'
    shareQuota: 50
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      keyVaultReferenceIdentity: 'SystemAssigned'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'WEBSITES_PORT'
          value: '8080'
        }
        {
          name: 'DocumentStorage__RootPath'
          value: '/home/carehome-documents'
        }
        {
          name: 'Https__Redirect'
          value: 'true'
        }
      ]
      azureStorageAccounts: {
        carehomedocs: {
          type: 'AzureFiles'
          accountName: storage.name
          shareName: documentsShareName
          mountPath: '/home/carehome-documents'
          accessKey: storage.listKeys().keys[0].value
        }
      }
    }
  }
  dependsOn: [
    documentsShare
    sqlDatabase
    keyVault
  ]
}

resource webAppSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, webApp.id, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource deployerSecretsOfficerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerObjectId)) {
  scope: keyVault
  name: guid(keyVault.id, deployerObjectId, keyVaultSecretsOfficerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsOfficerRoleId)
    principalId: deployerObjectId
    principalType: 'User'
  }
}

output webAppName string = webApp.name
output webAppHostname string = webApp.properties.defaultHostName
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output sqlDatabaseSku string = sqlDatabase.sku.name
output sqlDatabaseSkuTier string = sqlDatabase.sku.tier
output sqlBackupRetentionDays int = isStandardOrAbove ? sqlBackupRetentionDays : 7
output connectionStringHint string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};User Id=${sqlAdminLogin};Password=<secret>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True'
output storageAccountName string = storage.name
output documentsShareName string = documentsShareName
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output recoveryVaultName string = enableDocumentBackup ? recoveryVault.name : ''
output documentBackupPolicyName string = enableDocumentBackup ? documentBackupPolicy.name : ''
