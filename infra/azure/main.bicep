@description('CarehomeSystem Azure: App Service + Azure SQL (same-origin SPA+API)')
param location string = resourceGroup().location
param appName string
param sqlServerName string
param sqlAdminLogin string
@secure()
param sqlAdminPassword string
param sqlDatabaseName string = 'CareHome'
param appServiceSku string = 'B1'
param appServiceSkuTier string = 'Basic'

var webAppName = appName
var planName = '${appName}-plan'
var documentsShareName = 'carehome-documents'

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
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
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
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
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
          name: 'Email__Mode'
          value: 'Development'
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
  ]
}

output webAppName string = webApp.name
output webAppHostname string = webApp.properties.defaultHostName
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output connectionStringHint string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};User Id=${sqlAdminLogin};Password=<secret>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True'
output storageAccountName string = storage.name
