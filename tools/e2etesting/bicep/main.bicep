// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
//
// Declarative replacement for the imperative tools/e2etesting/*.ps1 setup scripts.
// Deploys the e2e test resource group's static resources with local-auth disabled
// on every resource that supports it, and grants the test SP the data-plane RBAC
// roles needed by the test code.
//
// Usage (PowerShell):
//   az deployment group create `
//     --resource-group <rg> `
//     --template-file tools/e2etesting/bicep/main.bicep `
//     --parameters testSuffix=12345 testServicePrincipalObjectId=<sp-objectId>
//
// VerifyDeployment.ps1 (alongside this file) asserts every resource is in the
// expected no-local-auth state after deployment.

@description('Unique 5-digit suffix appended to resource names (e.g. test pipeline build id).')
param testSuffix string

@description('Azure location to deploy resources to. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Object id of the federated Service Principal that runs the tests and pipeline.')
param testServicePrincipalObjectId string

@description('Optional object id of a user-assigned managed identity for ACI container groups.')
param aciManagedIdentityObjectId string = ''

@description('IoT Hub SKU. S1 is the minimum that supports D2C messages at test volume.')
@allowed([ 'S1', 'S2', 'S3' ])
param iotHubSku string = 'S1'

var iotHubName = 'e2etesting-iotHub-${testSuffix}'
var keyVaultName = 'e2etestingkeyvault${testSuffix}'

// Well-known built-in role definition ids. Hard-coded GUIDs are the recommended pattern
// per Microsoft Learn (https://learn.microsoft.com/azure/role-based-access-control/built-in-roles).
var kvSecretsOfficerRoleId       = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7' // Key Vault Secrets Officer
var iotHubDataContributorRoleId  = '4fc6c259-987e-4a07-842e-c321cc9d413f' // IoT Hub Data Contributor
var eventHubsDataReceiverRoleId  = 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde' // Azure Event Hubs Data Receiver
var acrPullRoleId                = '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull

resource keyVault 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC, not access policies. Phase 1.1.
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource iotHub 'Microsoft.Devices/IotHubs@2023-06-30' = {
  name: iotHubName
  location: location
  sku: {
    name: iotHubSku
    capacity: 1
  }
  properties: {
    // Disable shared access policy auth on the data plane; AAD-only. Phase 1.3 + 1.4.
    disableLocalAuth: true
    eventHubEndpoints: {
      events: {
        retentionTimeInDays: 1
        partitionCount: 4
      }
    }
    cloudToDevice: {
      defaultTtlAsIso8601: 'PT1H'
      maxDeliveryCount: 10
      feedback: {
        ttlAsIso8601: 'PT1H'
        lockDurationAsIso8601: 'PT60S'
        maxDeliveryCount: 10
      }
    }
    messagingEndpoints: {
      fileNotifications: {
        ttlAsIso8601: 'PT1H'
        lockDurationAsIso8601: 'PT1M'
        maxDeliveryCount: 10
      }
    }
  }
}

resource testConsumerGroup 'Microsoft.Devices/IotHubs/eventHubEndpoints/ConsumerGroups@2023-06-30' = {
  parent: iotHub
  name: 'events/TestConsumer'
  properties: {
    name: 'TestConsumer'
  }
}

resource testAcr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: 'e2etestacr${testSuffix}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    // Admin user explicitly disabled; AAD-only. Phase 1.5.
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

// Role assignments for the test service principal. Declared as resources for
// idempotency and so VerifyDeployment.ps1 can assert their presence.

resource kvRoleForTestSp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, testServicePrincipalObjectId, kvSecretsOfficerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsOfficerRoleId)
    principalId: testServicePrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

resource iotHubRoleForTestSp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: iotHub
  name: guid(iotHub.id, testServicePrincipalObjectId, iotHubDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', iotHubDataContributorRoleId)
    principalId: testServicePrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

resource eventHubsRoleForTestSp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: iotHub
  name: guid(iotHub.id, testServicePrincipalObjectId, eventHubsDataReceiverRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', eventHubsDataReceiverRoleId)
    principalId: testServicePrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

// Optional: grant the ACI managed identity AcrPull on the test ACR. Only needed if a
// private (non-MCR) image is used for the OPC PLC containers.
resource acrPullForAciMi 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(aciManagedIdentityObjectId)) {
  scope: testAcr
  name: guid(testAcr.id, aciManagedIdentityObjectId, acrPullRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: aciManagedIdentityObjectId
    principalType: 'ServicePrincipal'
  }
}

// Tag the resource group with the test suffix so the test code can discover it.
resource rgTag 'Microsoft.Resources/tags@2023-07-01' = {
  name: 'default'
  properties: {
    tags: {
      TestingResourcesSuffix: testSuffix
    }
  }
}

// ------------------------------------------------------------------------------------
// Outputs — surfaced as az deployment outputs and consumed by the GHA workflow.
// ------------------------------------------------------------------------------------

@description('IoT Hub fully qualified name (use as IOTHUB_HOSTNAME env var).')
output iotHubHostname string = '${iotHub.name}.azure-devices.net'

@description('IoT Hub built-in Event Hub fully qualified namespace (IOTHUB_EVENTHUB_NAMESPACE).')
output iotHubEventHubNamespace string = split(split(iotHub.properties.eventHubEndpoints.events.endpoint, '/')[2], ':')[0]

@description('IoT Hub built-in Event Hub entity name (IOTHUB_EVENTHUB_NAME).')
output iotHubEventHubName string = iotHub.name

@description('Test Key Vault name.')
output keyVaultName string = keyVault.name

@description('Test Key Vault resource id.')
output keyVaultId string = keyVault.id

@description('Test ACR login server (e.g. e2etestacr12345.azurecr.io).')
output acrLoginServer string = testAcr.properties.loginServer
