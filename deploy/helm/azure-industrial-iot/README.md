# Azure Industrial IoT

[Azure Industrial IoT](https://github.com/Azure/Industrial-IoT) allows users to discover OPC UA
enabled servers in a factory network, register them in Azure IoT Hub amd start collecting data from them.

## Introduction

This chart bootstraps [Azure Industrial IoT](https://github.com/Azure/Industrial-IoT) deployment on a
[Kubernetes](https://kubernetes.io/) cluster using the [Helm](https://helm.sh/) package manager.

## Prerequisites

* Helm 3.0+
* [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)

The chart requires several Azure resources to be present for components of Azure Industrial IoT solution to function.
Their details should be passed via either `values.yaml` or command-line arguments.

### Required Azure Resources

The following Azure resources are mandatory for the operation of Industrial IoT components.

#### Azure AAD Tenant

This will be present if you have an Azure account. The chart only needs Guid representing the TenantId. You
can get it by running the following command:

```bash
az account show --query "tenantId"
```

#### Azure IoT Hub

You would need to have an existing Azure IoT Hub. Here are the steps to
[create an IoT Hub using the Azure portal](https://docs.microsoft.com/azure/iot-hub/iot-hub-create-through-portal).
S1 Standard tier with 1 IoT Hub unit capacity IoT Hub would suffice.

The following details of the IoT Hub would be required:

* Name of the IoT Hub
* Details of built-it Event Hub-compatible endpoint. Details on how to get this information from Azure portal
  can be found [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin#read-from-the-built-in-endpoint).
  * Endpoint in `sb://<iot_hub_unique_identifier>.servicebus.windows.net/` format.
    This can be obtained with the following command:

    ```bash
    az iot hub show --name MyIotHub --query "properties.eventHubEndpoints.events.endpoint"
    ```

  * Number of partitions. This can be obtained with the following command:

    ```bash
    az iot hub show --name MyIotHub --query "properties.eventHubEndpoints.events.partitionCount"
    ```

  * A consumer group name. Please create a new consumer group for components of Azure Industrial IoT. You
    can call it `onboarding`, for example.

    ```bash
    az iot hub consumer-group create --hub-name MyIotHub --name onboarding
    ```

* Connection string of `iothubowner` policy for the IoT Hub. This can be obtained with the following command:

  ```bash
  az iot hub show-connection-string --name MyIotHub --policy-name iothubowner
  ```

  `iothubowner` policy is required because the components will perform management activities on IoT Hub (such
  as adding a new device).

#### Azure Cosmos DB Account

You would need to have an existing Azure Cosmos DB account. You can follow these steps to
[create an Azure Cosmos DB account from the Azure portal](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal#create-an-azure-cosmos-db-account). For the `API` please select `Core (SQL)`.

The following details of the Cosmos DB account would be required:

* Connection string. This can be obtained with the following command:

  ```bash
  az cosmosdb keys list --resource-group MyResourceGroup --name MyCosmosDBDatabaseAccount --query "primaryMasterKey"
  ```

  Either `primaryMasterKey` or `secondaryMasterKey` is required because Industrial IoT components will create
  containers and databases in the Cosmos DB account.

#### Azure Storage Account

#### Azure Event Hub Namespace

#### Azure Service Bus Namespace

#### Azure Key Vault

### Recommended Azure Resources

#### Azure AAD App Registration

### Optional Azure Resources

#### Azure Application Insights

## Configuration

The following table lists the configurable parameters of the Azure Industrial IoT chart and their default values.

### Image

| Parameter           | Description                              | Default                 |
|---------------------|------------------------------------------|-------------------------|
| `image.registry`    | URL of Docker Image Registry             | `mcr.microsoft.com/iot` |
| `image.tag`         | Image tag                                | `2.5.2`                 |
| `image.pullPolicy`  | Image pull policy                        | `IfNotPresent`          |
| `image.pullSecrets` | docker-registry secret names as an array | `[]`                    |

### Azure Resources

| Parameter | Description | Default |
|-----------|-------------|---------|
|           |             |         |

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
