# Azure Industrial IoT

[Azure Industrial IoT](https://github.com/Azure/Industrial-IoT) allows users to discover OPC UA
enabled servers in a factory network, register them in Azure IoT Hub and start collecting data from them.

## Introduction

This chart bootstraps [Azure Industrial IoT](https://github.com/Azure/Industrial-IoT) deployment on a
[Kubernetes](https://kubernetes.io/) cluster using the [Helm](https://helm.sh/) package manager.

## Prerequisites

* Helm 3.0+
* [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) to run the commands bellow.

The chart requires several Azure resources to be present for components of Azure Industrial IoT solution to function.
Their details should be passed via either `values.yaml` or command-line arguments.

### Required Azure Resources

The following Azure resources are mandatory for the operation of Azure Industrial IoT components.

#### Azure AAD Tenant

This will be present if you have an Azure account. The chart only needs Guid representing the TenantId.
This can be obtained with the following command:

```bash
az account show --query "tenantId"
```

#### Azure IoT Hub

You would need to have an existing Azure IoT Hub. Here are the steps to
[create an IoT Hub using the Azure portal](https://docs.microsoft.com/azure/iot-hub/iot-hub-create-through-portal).
S1 Standard tier with 1 IoT Hub unit capacity IoT Hub would suffice.

The following details of the Azure IoT Hub would be required:

* Name of the Azure IoT Hub
* Details of built-it Event Hub-compatible endpoint. Details on how to get this information from Azure portal
  can be found [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin#read-from-the-built-in-endpoint).
  * Endpoint in `sb://<iot_hub_unique_identifier>.servicebus.windows.net/` format.
    This can be obtained with the following command:

    ```bash
    az iot hub show --name MyIotHub --query "properties.eventHubEndpoints.events.endpoint"
    ```

  * Number of partitions.
    This can be obtained with the following command:

    ```bash
    az iot hub show --name MyIotHub --query "properties.eventHubEndpoints.events.partitionCount"
    ```

  * A consumer group name. Please create a new consumer group for components of Azure Industrial IoT.
    You can call it `onboarding`, for example. This can be created with the following command:

    ```bash
    az iot hub consumer-group create --hub-name MyIotHub --name onboarding
    ```

* Connection string of `iothubowner` policy for the Azure IoT Hub.
  This can be obtained with the following command:

  ```bash
  az iot hub show-connection-string --name MyIotHub --policy-name iothubowner
  ```

  `iothubowner` policy is required because the components will perform management activities on Azure IoT Hub
  (such as adding a new device).

#### Azure Cosmos DB Account

You would need to have an existing Azure Cosmos DB account. You can follow these steps to
[create an Azure Cosmos DB account from the Azure portal](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal#create-an-azure-cosmos-db-account). For the `API` please select `Core (SQL)`.

The following details of the Azure Cosmos DB account would be required:

* Connection string. This can be obtained with the following command:

  ```bash
  az cosmosdb keys list --resource-group MyResourceGroup --name MyCosmosDBDatabaseAccount --query "primaryMasterKey"
  ```

  Either `primaryMasterKey` or `secondaryMasterKey` is required because Azure Industrial IoT components
  will create containers and databases in the Azure Cosmos DB account.

#### Azure Storage Account

You would need to have an existing Azure Storage account. Here are the steps to
[create an Azure Storage account](https://docs.microsoft.com/azure/storage/common/storage-account-create).

The following details of the Azure Storage account would be required:

* Name of the Azure Storage account
* Access Key for Azure Storage account.
  This can be obtained with the following command:

  ```bash
  az storage account keys list --account-name MyStorageAccount --query "[0].value"
  ```

#### Azure Event Hub Namespace

You would need to have an existing Azure Event Hub namespace. Here are the steps to
[create an Event Hubs namespace](https://docs.microsoft.com/azure/event-hubs/event-hubs-create#create-an-event-hubs-namespace).

The following details of the Azure Event Hub namespace would be required:

* Connection string of `RootManageSharedAccessKey` policy for the Azure Event Hub namespace.
  This can be obtained with the following command:

  ```bash
  az eventhubs namespace authorization-rule keys list --resource-group MyResourceGroup --namespace-name mynamespace --name RootManageSharedAccessKey --query "primaryConnectionString"
  ```

  Both `primaryConnectionString` and `secondaryConnectionString` would work. `RootManageSharedAccessKey` is
  required because components will perform management activities, such as creating an Event Hub.

#### Azure Service Bus Namespace

You would need to have an existing Azure Service Bus namespace. Here are the steps to
[create a Service Bus namespace in the Azure portal](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal#create-a-namespace-in-the-azure-portal).

The following details of the Azure Event Hub namespace would be required:

* Connection string of `RootManageSharedAccessKey` policy for the Azure Service Bus namespace.
  This can be obtained with the following command:

  ```bash
  az servicebus namespace authorization-rule keys list --resource-group MyResourceGroup --namespace-name mynamespace --name RootManageSharedAccessKey --query "primaryConnectionString"
  ```

  Both `primaryConnectionString` and `secondaryConnectionString` would work. `RootManageSharedAccessKey` is
  required because components will perform management activities, such as creating Service Bus topics.

#### Azure Key Vault

You would need to have an existing Azure Key Vault. Here are the steps to
[create a Key Vault in the Azure portal](https://docs.microsoft.com/azure/key-vault/quick-create-portal#create-a-vault).

The following details of the Azure Key Vault would be required:

* URI, also referred as DNS Name. This can be obtained with the following command:

  ```bash
  az keyvault show --name MyKeyVault --query "properties.vaultUri"
  ```

### Recommended Azure Resources

#### Azure AAD App Registration

Details of AAD App Registration are required if you want to enable authentication for components of
Azure Industrial IoT solution. In that case, web APIs of components will require an Access Token for
each API call. If using Swagger, you would have to click on `Authorize` button to authenticate, before you
can try out API calls. The authentication will happen against your Azure Active Directory (AAD). For this
we require two apps to be registered in your AAD:

* One for components of Azure Industrial IoT, we refer to this as **ServicesApp**
* One for web clients accessing web interfaces of Azure Industrial IoT components, we refer to this as
  **ClientsApp**.

Here are the steps to [create AAD App Registrations](https://github.com/Azure/Industrial-IoT/blob/master/docs/deploy/howto-register-aad-applications.md).

> **NOTE:** For any production deployment of Azure Industrial IoT solution it is required that those AAD
App Registrations are created and details are provided to the chart. And we strongly recommend having those
for non-production deployments as well, particularly if you have enables Ingress. If you choose to not have
them, then you would have to disable authentication by setting `azure.auth.required=false`.

The following details of AAD App Registrations will be required:

* Application ID URI for **ServicesApp**. This can be obtained with the following command:

  ```bash
  az ad app show --id 00000000-0000-0000-0000-000000000000 --query "identifierUris[0]"
  ```

  Here you should use object ID of **ServicesApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

* Application (client) ID for **ClientsApp**. This is also referred to as AppId.
  This can be obtained with the following command:

  ```bash
  az ad app show --id 00000000-0000-0000-0000-000000000000 --query "appId"
  ```

  Here you should use object ID of **ClientsApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

### Optional Azure Resources

#### Azure Application Insights

You can enable delivery of telemetry and logs from components of Azure Industrial IoT to an Azure
Application Insights instance if you have one. Here are the steps to
[create an Application Insights resource](https://docs.microsoft.com/azure/azure-monitor/app/create-new-resource).

To run the command below you will require `application-insights` extension for Azure CLI. To install it
run the following command:

```bash
az extension add --name application-insights
```

The following details of the Azure Application Insights would be required:

* Name of your Azure Application Insights instance.
* Instrumentation key for your Azure Application Insights instance.
  This can be obtained with the following command:

  ```bash
  az monitor app-insights component show --resource-group MyResourceGroup --app MyAppInsights --query "instrumentationKey"
  ```

## Configuration

The following table lists the configurable parameters of the Azure Industrial IoT chart and their default
values.

### Image

| Parameter           | Description                              | Default                 |
|---------------------|------------------------------------------|-------------------------|
| `image.registry`    | URL of Docker Image Registry             | `mcr.microsoft.com/iot` |
| `image.tag`         | Image tag                                | `2.5.2`                 |
| `image.pullPolicy`  | Image pull policy                        | `IfNotPresent`          |
| `image.pullSecrets` | docker-registry secret names as an array | `[]`                    |

### Azure Resources

| Parameter                                                                                   | Description                                                                   | Default                              |
|---------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------|--------------------------------------|
| `azure.tenantId`                                                                            | Azure tenant id (GUID)                                                        | `null`                               |
| `azure.iotHub.name`                                                                         | Name of IoT Hub                                                               | `null`                               |
| `azure.iotHub.eventHub.endpoint`                                                            | Event Hub-compatible endpoint of built-in EventHub of IoT Hub                 | `null`                               |
| `azure.iotHub.eventHub.partitionCount`                                                      | Number of partitions of built-in EventHub of IoT Hub                          | `null`                               |
| `azure.iotHub.eventHub.consumerGroup`                                                       | Consumer group for built-in EventHub of IoT Hub                               | `null`                               |
| `azure.iotHub.sharedAccessPolicies.iothubowner.connectionString`                            | Connection string of `iothubowner` policy of IoT Hub                          | `null`                               |
| `azure.cosmosDB.connectionString`                                                           | Cosmos DB connection string with read-write permissions                       | `null`                               |
| `azure.storageAccount.name`                                                                 | Name of Storage Account                                                       | `null`                               |
| `azure.storageAccount.accessKey`                                                            | Access key for storage account, **not** connection string                     | `null`                               |
| `azure.storageAccount.endpointSuffix`                                                       | Blob endpoint suffix of Azure Environment                                     | `core.windows.net`                   |
| `azure.eventHubNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString`   | Connection string of `RootManageSharedAccessKey` key of Event Hub namespace   | `null`                               |
| `azure.serviceBusNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString` | Connection string of `RootManageSharedAccessKey` key of Service Bus namespace | `null`                               |
| `azure.keyVault.uri`                                                                        | Key Vault URI, also referred as DNS Name                                      | `null`                               |
| `azure.applicationInsights.name`                                                            | Name of Application Insights instance                                         | `null`                               |
| `azure.applicationInsights.instrumentationKey`                                              | Instrumentation key of Application Insights instance                          | `null`                               |
| `azure.auth.required`                                                                       | If true, authentication will be required for all exposed web APIs             | `true`                               |
| `azure.auth.corsWhitelist`                                                                  | Cross-origin resource sharing whitelist for all web APIs                      | `*`                                  |
| `azure.auth.servicesApp.audience`                                                           | Application ID URI for **ServicesApp**                                        | `null`                               |
| `azure.auth.clientsApp.appId`                                                               | Application (client) ID for **ClientsApp**, also referred to as `AppId`       | `null`                               |
| `azure.auth.clientsApp.authority`                                                           | Authority that should authenticate users and provide Access Tokens            | `https://login.microsoftonline.com/` |

### RBAC

| Parameter     | Description                            | Default |
|---------------|----------------------------------------|---------|
| `rbac.create` | If true, create and use RBAC resources | `true`  |

### Service Account

| Parameter               | Description                                         | Default |
|-------------------------|-----------------------------------------------------|---------|
| `serviceAccount.create` | If true, create and use ServiceAccount resources    | `true`  |
| `serviceAccount.name`   | Name of the server service account to use or create | `null`  |

### Deployed Components

| Parameter | Description | Default |
|-----------|-------------|---------|
|           |             |         |
