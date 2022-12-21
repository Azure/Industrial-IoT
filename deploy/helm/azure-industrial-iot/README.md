# Azure Industrial IoT <!-- omit in toc -->

[Azure Industrial IoT](https://github.com/Azure/Industrial-IoT) allows users to discover OPC UA
enabled servers in a factory network, register them in Azure IoT Hub and start collecting data from them.

## Table Of Contents <!-- omit in toc -->

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
  * [Required Azure Resources](#required-azure-resources)
    * [Azure AAD Tenant](#azure-aad-tenant)
    * [Azure IoT Hub](#azure-iot-hub)
    * [Azure Cosmos DB Account](#azure-cosmos-db-account)
    * [Azure Storage Account](#azure-storage-account)
      * [Data Protection Container (Optional)](#data-protection-container-optional)
    * [Azure Event Hub Namespace](#azure-event-hub-namespace)
      * [Azure Event Hub](#azure-event-hub)
      * [Azure Event Hub Consumer Groups](#azure-event-hub-consumer-groups)
    * [Azure Service Bus Namespace](#azure-service-bus-namespace)
    * [Azure Key Vault](#azure-key-vault)
      * [Data Protection Key (Optional)](#data-protection-key-optional)
  * [Recommended Azure Resources](#recommended-azure-resources)
    * [Azure AAD App Registration](#azure-aad-app-registration)
  * [Optional Azure Resources](#optional-azure-resources)
    * [Azure SignalR](#azure-signalr)
    * [Azure Application Insights](#azure-application-insights)
    * [Azure Log Analytics Workspace](#azure-log-analytics-workspace)
* [Installing the Chart](#installing-the-chart)
* [Configuration](#configuration)
  * [Image](#image)
  * [Azure Resources](#azure-resources)
  * [Load Configuration From Azure Key Vault](#load-configuration-from-azure-key-vault)
  * [External Service URL](#external-service-url)
  * [RBAC](#rbac)
  * [Service Account](#service-account)
  * [Application Runtime Configuration](#application-runtime-configuration)
  * [Deployed Components](#deployed-components)
    * [Deployment Resource Configuration](#deployment-resource-configuration)
    * [Service Resource Configuration](#service-resource-configuration)
    * [Ingress Resource Configuration](#ingress-resource-configuration)
  * [Prometheus](#prometheus)
  * [Minimal Configuration](#minimal-configuration)
* [Special Notes](#special-notes)
  * [Resource Requests And Limits](#resource-requests-and-limits)
  * [Data Protection](#data-protection)
    * [Azure Storage Account Container](#azure-storage-account-container)
    * [Azure Key Vault Key](#azure-key-vault-key)
  * [Swagger](#swagger)
  * [NGINX Ingress Controller](#nginx-ingress-controller)
    * [Controller configuration](#controller-configuration)
    * [Ingress Annotations](#ingress-annotations)

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
$ az account show --query "tenantId"
"XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
```

#### Azure IoT Hub

You would need to have an existing Azure IoT Hub. Here are the steps to
[create an IoT Hub using the Azure portal](https://docs.microsoft.com/azure/iot-hub/iot-hub-create-through-portal).
S1 Standard tier with 1 IoT Hub unit capacity IoT Hub would suffice.

The following details of the Azure IoT Hub would be required:

* Details of built-it Event Hub-compatible endpoint. Details on how to get this information from Azure portal
  can be found [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin#read-from-the-built-in-endpoint).

  * Endpoint in `sb://<iot_hub_unique_identifier>.servicebus.windows.net/` format.
    This can be obtained with the following command:

    ```bash
    $ az iot hub show --name MyIotHub --query "properties.eventHubEndpoints.events.endpoint"
    "sb://iothub-ns-XXXXXX-XXX-XXXXXXX-XXXXXXXXXX.servicebus.windows.net/"
    ```

  * Several consumer groups. Please create new consumer groups for components of Azure Industrial IoT.
    The consumer groups can be created with the following commands:

    ```bash
    $ az iot hub consumer-group create --hub-name MyIotHub --name events

    $ az iot hub consumer-group create --hub-name MyIotHub --name telemetry

    $ az iot hub consumer-group create --hub-name MyIotHub --name onboarding
    ```

    Here are our recommended names for them:

    * `events`: will be used by `eventsProcessor` microservices
    * `telemetry`: will be used by `telemetryProcessor` microservices
    * `onboarding`: will be used by `onboarding` microservices

* Connection string of `iothubowner` policy for the Azure IoT Hub.
  This can be obtained with the following command:

  ```bash
  $ az iot hub show-connection-string --name MyIotHub --policy-name iothubowner --query "connectionString"
  "HostName=MyIotHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
  ```

  `iothubowner` policy is required because the components will perform management activities on Azure IoT Hub
  (such as adding a new device).

#### Azure Cosmos DB Account

You would need to have an existing Azure Cosmos DB account. You can follow these steps to
[create an Azure Cosmos DB account from the Azure portal](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal#create-an-azure-cosmos-db-account).
For the `API` please select `Core (SQL)`.

The following details of the Azure Cosmos DB account would be required:

* Connection string. This can be obtained with the following command:

  ```bash
  $ az cosmosdb keys list --resource-group MyResourceGroup --name MyCosmosDBDatabaseAccount --type connection-strings --query "connectionStrings[0].connectionString"
  "AccountEndpoint=https://MyCosmosDBDatabaseAccount.documents.azure.com:443/;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;"
  ```

  If you remove `--query "primaryMasterKey"` part, you would see four connection strings in output. We require
  either Primary or Secondary SQL Connection String, **not** Read-Only ones. This is required because Azure
  Industrial IoT components will create containers and databases in the Azure Cosmos DB account.

#### Azure Storage Account

You would need to have an existing Azure Storage account. Here are the steps to
[create an Azure Storage account](https://docs.microsoft.com/azure/storage/common/storage-account-create).
When creating it please make sure to:

* Set `Account kind` to `StorageV2 (general-purpose v2)`, step 7
* Set `Hierarchical namespace` to `Disabled`, step 8

The following details of the Azure Storage account would be required:

* Connection string for Azure Storage account.
  This can be obtained with the following command:

  ```bash
  $ az storage account show-connection-string --name MyStorageAccount --query "connectionString"
  "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=MyStorageAccount;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
  ```

##### Data Protection Container (Optional)

> Data protection functionality is present only if `engineeringTool` component is enabled.

[Data protection](https://docs.microsoft.com/aspnet/core/security/data-protection/introduction?view=aspnetcore-3.1)
is an ASP.Net Core feature that is used by `engineeringTool` component. It takes care of encryption of cookies that
are used by the `engineeringTool`.

We use Azure Storage as
[storage provider](https://docs.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-3.1#azure-storage)
for storing data protection keys, and as such we require an Azure Storage Container. You can specify the
name of Azure Storage Container to be used for storing keys or the value will default to `dataprotection`.
This Azure Storage Container will be created automatically (if it doesn't already exist) when `engineeringTool`
component is enabled.

Configuration parameter for data protection Azure Storage Container is
`azure.storageAccount.container.dataProtection.name`.

#### Azure Event Hub Namespace

You would need to have an existing Azure Event Hub namespace. Here are the steps to
[create an Event Hubs namespace](https://docs.microsoft.com/azure/event-hubs/event-hubs-create#create-an-event-hubs-namespace).
Please note that we require Event Hub Namespace with **Standard** pricing tier because we need it to have
two consumer groups.

The following details of the Azure Event Hub namespace would be required:

* Connection string of `RootManageSharedAccessKey` policy for the Azure Event Hub namespace.
  This can be obtained with the following command:

  ```bash
  $ az eventhubs namespace authorization-rule keys list --resource-group MyResourceGroup --namespace-name mynamespace --name RootManageSharedAccessKey --query "primaryConnectionString"
  "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
  ```

  Both `primaryConnectionString` and `secondaryConnectionString` would work. `RootManageSharedAccessKey` is
  required because components will perform management activities, such as creating an Event Hub.

##### Azure Event Hub

Please create an Azure Event Hub in your Azure Event Hub Namespace. This can be created with the command
bellow. Please note that you need to specify your Resource Group, Event Hub Namespace, desired name of Event
Hub, desired message retention period (in days) and desired number of partitions. More details about the
command and its parameters can be found
[here](https://docs.microsoft.com/cli/azure/eventhubs/eventhub?view=azure-cli-latest#az-eventhubs-eventhub-create).

```bash
$ az eventhubs eventhub create --resource-group MyResourceGroup --namespace-name mynamespace --name myeventhub --message-retention 2 --partition-count 2
```

##### Azure Event Hub Consumer Groups

Please create a consumer group for the Event Hub. For example, you can call it `telemetry_ux`.
These can be created with the commands bellow. More details about the command and its
parameters can be found
[here](https://docs.microsoft.com/cli/azure/eventhubs/eventhub/consumer-group?view=azure-cli-latest#az-eventhubs-eventhub-consumer-group-create).

```bash
$ az eventhubs eventhub consumer-group create --resource-group MyResourceGroup --namespace-name mynamespace --eventhub-name myeventhub --name telemetry_ux
```

#### Azure Service Bus Namespace

You would need to have an existing Azure Service Bus namespace. Here are the steps to
[create a Service Bus namespace in the Azure portal](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal#create-a-namespace-in-the-azure-portal).

The following details of the Azure Event Hub namespace would be required:

* Connection string of `RootManageSharedAccessKey` policy for the Azure Service Bus namespace.
  This can be obtained with the following command:

  ```bash
  $ az servicebus namespace authorization-rule keys list --resource-group MyResourceGroup --namespace-name mynamespace --name RootManageSharedAccessKey --query "primaryConnectionString"
  "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
  ```

  Both `primaryConnectionString` and `secondaryConnectionString` would work. `RootManageSharedAccessKey` is
  required because components will perform management activities, such as creating Service Bus topics.

#### Azure Key Vault

You would need to have an existing Azure Key Vault. Here are the steps to
[create a Key Vault in the Azure portal](https://docs.microsoft.com/azure/key-vault/quick-create-portal#create-a-vault).

The following details of the Azure Key Vault would be required:

* URI, also referred as DNS Name. This can be obtained with the following command:

  ```bash
  $ az keyvault show --name MyKeyVault --query "properties.vaultUri"
  "https://MyKeyVault.vault.azure.net/"
  ```

##### Data Protection Key (Optional)

> Data protection functionality is present only if `engineeringTool` component is enabled.

[Data protection](https://docs.microsoft.com/aspnet/core/security/data-protection/introduction?view=aspnetcore-3.1)
is an ASP.Net Core feature that is used by `engineeringTool` component. It takes care of encryption of cookies that
are used by the `engineeringTool`.

We use a key in Azure Key Vault to protect keys that are stored in
[data protection Azure Storage Container](#data-protection-container-(optional)). So you can either create
your own key in Azure Key Vault and provide that or one will be created for you automatically. More
precisely, if the key with the provided name already exists in the Key Vault, then it will be used.
Otherwise a key with the provided name will be created. If a key name is not specified then it defaults
to `dataprotection`.

Configuration parameter for data protection key in Azure Key Vault is `azure.keyVault.key.dataProtection`.

### Recommended Azure Resources

#### Azure AAD App Registration

Required for:

* `engineeringTool`

Details of AAD App Registration are required if you want to enable authentication for components of
Azure Industrial IoT solution. If authentication is enabled, web APIs of components will require an Access Token for
each API call. If using Swagger, you would have to click on `Authorize` button to authenticate, before you
can try out API calls. The authentication will happen against your Azure Active Directory (AAD). For this
we require two apps to be registered in your AAD:

* One for components of Azure Industrial IoT, we refer to this as **ServicesApp**
* One for web clients accessing web interfaces of Azure Industrial IoT components, we refer to this as
  **ClientsApp**.

Here are the steps to [create AAD App Registrations](../../../docs/deploy/howto-register-aad-applications.md).

> **NOTE:** For any production deployment of Azure Industrial IoT solution it is required that those AAD
App Registrations are created and details are provided to the chart. And we strongly recommend having those
for non-production deployments as well, particularly if you have enabled Ingress. If you choose to not have
them, then you would have to disable authentication by setting `azure.auth.required=false`.

The following details of **ServicesApp** AAD App Registrations will be required:

* Application (client) ID for **ServicesApp**. This is also referred to as AppId.
  This can be obtained with the following command:

  ```bash
  $ az ad app show --id 00000000-0000-0000-0000-000000000000 --query "appId"
  "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  ```

  Here you should use object ID of **ServicesApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

* Client secret for **ServicesApp**. Client secret is also referred to as password. Here you can either
  provide client secret that you got when creating AAD App Registration. Or you can create a new client
  secret and use that.

  Here are the steps to create new client secret
  [using portal](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal#create-a-new-application-secret),
  or you can create a new client secret (password) using the following command:

  ```bash
  $ az ad app credential reset --id 00000000-0000-0000-0000-000000000000 --append
  {
    "appId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "name": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "password": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "tenant": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  }
  ```

  Here you should use object ID of **ServicesApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

* Application ID URI for **ServicesApp**. This can be obtained with the following command:

  ```bash
  $ az ad app show --id 00000000-0000-0000-0000-000000000000 --query "identifierUris[0]"
  "https://XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX/aiiotApp-service"
  ```

  Here you should use object ID of **ServicesApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

The following details of **ClientsApp** AAD App Registrations will be required:

* Application (client) ID for **ClientsApp**. This is also referred to as AppId.
  This can be obtained with the following command:

  ```bash
  $ az ad app show --id 00000000-0000-0000-0000-000000000000 --query "appId"
  "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  ```

  Here you should use object ID of **ClientsApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

* Client secret for **ClientsApp**. Client secret is also referred to as password. Here you can either
  provide client secret that you got when creating AAD App Registration. Or you can create a new client
  secret and use that.

  Here are the steps to create new client secret
  [using portal](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal#create-a-new-application-secret),
  or you can create a new client secret (password) using the following command:

  ```bash
  $ az ad app credential reset --id 00000000-0000-0000-0000-000000000000 --append
  {
    "appId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "name": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "password": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
    "tenant": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  }
  ```

  Here you should use object ID of **ClientsApp** AAD App Registrations instead of
  `00000000-0000-0000-0000-000000000000`.

### Optional Azure Resources

#### Azure SignalR

If you want to enable deployment of Engineering Tool as part of deployment of the chart then you will need
to have an existing Azure SignalR instance. Here are the steps to
[create an Azure SignalR Service instance](https://docs.microsoft.com/azure/azure-signalr/signalr-quickstart-azure-functions-csharp#create-an-azure-signalr-service-instance).
When creating Azure SignalR instance please set `Service mode` to `Default` in step 3.

You can also create an Azure SignalR service using [Azure CLI](https://docs.microsoft.com/cli/azure/signalr?view=azure-cli-latest#az-signalr-create):

```bash
$ az signalr create --name MySignalR --resource-group MyResourceGroup --sku Standard_S1 --unit-count 1 --service-mode Default
```

The following details of Azure SignalR service would be required:

* Connection string. This can be obtained with the following command:

  ```bash
  $ az signalr key list --name MySignalR --resource-group MyResourceGroup --query "primaryConnectionString"
  "Endpoint=https://mysignalr.service.signalr.net;AccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;Version=1.0;"
  ```

* Service Mode. Use the value that you set when creating Azure SignalR instance.

#### Azure Application Insights

You can enable delivery of telemetry and logs from components of Azure Industrial IoT to an Azure
Application Insights instance if you have one. Here are the steps to
[create an Application Insights resource](https://docs.microsoft.com/azure/azure-monitor/app/create-new-resource).

To run the command below you will require `application-insights` extension for Azure CLI. To install it
run the following command:

```bash
$ az extension add --name application-insights
```

The following details of the Azure Application Insights would be required:

* Instrumentation key for your Azure Application Insights instance.
  This can be obtained with the following command:

  ```bash
  $ az monitor app-insights component show --resource-group MyResourceGroup --app MyAppInsights --query "instrumentationKey"
  "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  ```

#### Azure Log Analytics Workspace

You can use Azure Log Analytics Workspace to collect metrics and logs from IoT Edge modules of Azure
Industrial IoT solution. Metrics and log collection and delivery to Azure Log Analytics Workspace will be
performed by `metricscollector` module.

Please follow these steps to [create a workspace](https://docs.microsoft.com/azure/azure-monitor/learn/quick-create-workspace#create-a-workspace).

The following details of the Azure Log Analytics Workspace would be required:

* Workspace Id. This can be obtained with the following command:

  ```bash
  $ az monitor log-analytics workspace show --resource-group MyResourceGroup --workspace-name MyWorkspace --query "customerId"
  This command group is in preview. It may be changed/removed in a future release.
  "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  ```

* Shared key for the workspace. This can be obtained with the following command:

  ```bash
  az monitor log-analytics workspace get-shared-keys --resource-group MyResourceGroup --workspace-name MyWorkspace
  This command group is in preview. It may be changed/removed in a future release.
  {
    "primarySharedKey": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "secondarySharedKey": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
  }
  ```

  Either one of the keys would work.

## Installing the Chart

This chart installs `2.8.4` version of components by default.

To install the chart first ensure that you have added `azure-iiot` repository:

```bash
$ helm repo add azure-iiot https://azure.github.io/Industrial-IoT/helm
```

After that make sure to create the namespace were you will deploy the chart:

```bash
# creating azure-iiot-ns namespace
$ kubectl create namespace azure-iiot-ns
namespace/azure-iiot-ns created
```

Then, to install the chart with the release name `azure-iiot` you would run the following command **changing
all values in `<>`** with the ones obtained by running commands in [Prerequisites](#prerequisites) section:

> **NOTE:** The command bellow explicitly **disables** authentication for the components
(`azure.auth.required=false`). For any production deployment of Azure Industrial IoT solution it is required
that AAD App Registrations are created and details are provided to the chart. And we strongly recommend
having those for non-production deployments as well, particularly if you have enabled Ingress.

```bash
$ helm install azure-iiot azure-iiot/azure-industrial-iot --namespace azure-iiot-ns \
  --set azure.tenantId=<TenantId> \
  --set azure.iotHub.eventHub.endpoint=<IoTHubEventHubEndpoint> \
  --set azure.iotHub.eventHub.consumerGroup.events=<IoTHubEventHubEventsConsumerGroup> \
  --set azure.iotHub.eventHub.consumerGroup.telemetry=<IoTHubEventHubTelemetryConsumerGroup> \
  --set azure.iotHub.eventHub.consumerGroup.onboarding=<IoTHubEventHubOnboardingConsumerGroup> \
  --set azure.iotHub.sharedAccessPolicies.iothubowner.connectionString=<IoTHubConnectionString> \
  --set azure.cosmosDB.connectionString=<CosmosDBConnectionString> \
  --set azure.storageAccount.connectionString=<StorageAccountConnectionString> \
  --set azure.eventHubNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString=<EventHubNamespaceConnectionString> \
  --set azure.eventHubNamespace.eventHub.name=<EventHubName> \
  --set azure.eventHubNamespace.eventHub.consumerGroup.telemetryUx=<EventHubTelemetryUxConsumerGroup> \
  --set azure.serviceBusNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString=<ServiceBusNamespaceConnectionString> \
  --set azure.keyVault.uri=<KeyVaultURI> \
  --set azure.auth.required=false
```

Alternatively, a YAML file that specifies the values for the parameters can be provided while installing
the chart. For example:

```bash
$ helm install azure-iiot azure-iiot/azure-industrial-iot --namespace azure-iiot-ns -f values.yaml
```

For reference sample of this `values.yam` file please check [Minimal Configuration](#minimal-configuration)
section.

## Configuration

The following table lists the configurable parameters of the Azure Industrial IoT chart and their default
values.

### Image

| Parameter           | Description                              | Default             |
|---------------------|------------------------------------------|---------------------|
| `image.registry`    | URL of Docker Image Registry             | `mcr.microsoft.com` |
| `image.tag`         | Image tag                                | `2.8.4`             |
| `image.pullPolicy`  | Image pull policy                        | `IfNotPresent`      |
| `image.pullSecrets` | docker-registry secret names as an array | `[]`                |

### Azure Resources

| Parameter                                                                                   | Description                                                                                      | Default                              |
|---------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|--------------------------------------|
| `azure.tenantId`                                                                            | Azure tenant id (GUID)                                                                           | `null`                               |
| `azure.iotHub.eventHub.endpoint`                                                            | Event Hub-compatible endpoint of built-in EventHub of IoT Hub                                    | `null`                               |
| `azure.iotHub.eventHub.consumerGroup.events`                                                | Consumer group of built-in EventHub of IoT Hub for `eventsProcessor`                             | `null`                               |
| `azure.iotHub.eventHub.consumerGroup.telemetry`                                             | Consumer group of built-in EventHub of IoT Hub for `telemetryProcessor`                          | `null`                               |
| `azure.iotHub.eventHub.consumerGroup.onboarding`                                            | Consumer group of built-in EventHub of IoT Hub for `onboarding`                                  | `null`                               |
| `azure.iotHub.sharedAccessPolicies.iothubowner.connectionString`                            | Connection string of `iothubowner` policy of IoT Hub                                             | `null`                               |
| `azure.cosmosDB.connectionString`                                                           | Cosmos DB connection string with read-write permissions                                          | `null`                               |
| `azure.storageAccount.connectionString`                                                     | Storage account connection string                                                                | `null`                               |
| `azure.storageAccount.container.dataProtection.name`                                        | Name of storage account container for [data protection](#data-protection-container-(optional))   | `dataprotection`                     |
| `azure.eventHubNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString`   | Connection string of `RootManageSharedAccessKey` key of Event Hub namespace                      | `null`                               |
| `azure.eventHubNamespace.eventHub.name`                                                     | Name of secondary Event Hub within Event Hub Namespace                                           | `null`                               |
| `azure.eventHubNamespace.eventHub.consumerGroup.telemetryUx`                                | Name of the consumer group for `telemetryUxProcessor`                                            | `null`                               |
| `azure.serviceBusNamespace.sharedAccessPolicies.rootManageSharedAccessKey.connectionString` | Connection string of `RootManageSharedAccessKey` key of Service Bus namespace                    | `null`                               |
| `azure.keyVault.uri`                                                                        | Key Vault URI, also referred as DNS Name                                                         | `null`                               |
| `azure.keyVault.key.dataProtection`                                                         | Key in Key Vault that should be used for [data protection](#data-protection-key-(optional))      | `dataprotection`                     |
| `azure.applicationInsights.instrumentationKey`                                              | Instrumentation key of Application Insights instance                                             | `null`                               |
| `azure.logAnalyticsWorkspace.id`                                                            | Workspace id of Log Analytics Workspace instance                                                 | `null`                               |
| `azure.logAnalyticsWorkspace.key`                                                           | Shared key for connecting a device to Log Analytics Workspace instance                           | `null`                               |
| `azure.signalR.connectionString`                                                            | SignalR connection string                                                                        | `null`                               |
| `azure.signalR.serviceMode`                                                                 | Service mode of SignalR instance                                                                 | `null`                               |
| `azure.auth.required`                                                                       | If true, authentication will be required for all exposed web APIs                                | `true`                               |
| `azure.auth.corsWhitelist`                                                                  | Cross-origin resource sharing whitelist for all web APIs                                         | `*`                                  |
| `azure.auth.authority`                                                                      | Authority that should authenticate users and provide Access Tokens                               | `https://login.microsoftonline.com/` |
| `azure.auth.servicesApp.appId`                                                              | Application (client) ID of AAD App Registration for **ServicesApp**, also referred to as `AppId` | `null`                               |
| `azure.auth.servicesApp.secret`                                                             | Client secret (password) of AAD App Registration for **ServicesApp**                             | `null`                               |
| `azure.auth.servicesApp.audience`                                                           | Application ID URI of AAD App Registration for **ServicesApp**                                   | `null`                               |
| `azure.auth.clientsApp.appId`                                                               | Application (client) ID of AAD App Registration for **ClientsApp**, also referred to as `AppId`  | `null`                               |
| `azure.auth.clientsApp.secret`                                                              | Client secret (password) of AAD App Registration for **ClientsApp**                              | `null`                               |

### Load Configuration From Azure Key Vault

If you are deploying this chart to an Azure environment that has been created by either `deploy.ps1` script
or `Microsoft.Azure.IIoT.Deployment` application then you can use the fact that both of those methods push
secrets to Azure Key Vault describing Azure resources IDs and connection details. Those secrets can be
consumed by components of Azure Industrial IoT solution as configuration parameters similar to configuration
environment variables that are injected to the Pods. To facilitate this method of configuration management
through Azure Key Vault the chart provides `loadConfFromKeyVault` parameters. If it is set to `true` then a
configuration provider that reads application configuration parameters from Azure Key Vault would be enabled.
In this case the chart will loosen requirement on provided values and only values necessary to connect to
Azure Key Vault will be required.

When `loadConfFromKeyVault` is set to `true`, our microservices will try to read configuration secrets from
your Azure Key Vault instance. So you would have to make sure that `servicesApp` has enough permissions to
access the Azure Key Vault. We require the following Access Policies to be set for service principal of
`servicesApp` so that microservices can function properly:

* Key Permissions: `get`, `list`, `sign`, `unwrapKey`, `wrapKey`, `create`
* Secret Permissions: `get`, `list`, `set`, `delete`
* Certificate Permissions: `get`, `list`, `update`, `create`, `import`

When `loadConfFromKeyVault` is set to `true`, then only the following parameters of `azure.*` parameter group
are required:

* `azure.tenantId`
* `azure.keyVault.uri`
* `azure.auth.servicesApp.appId`
* `azure.auth.servicesApp.secret`

A few notes about `loadConfFromKeyVault`:

* Any additional parameters provided to the chart will also be applied. They will act as overrides to the
  values coming from Azure Key Vault.
* Values defining Kubernetes resources or deployment logic will not be affected by `loadConfFromKeyVault`
  parameter and should be set independent of it.
* We recommend setting `externalServiceUrl` regardless of the value of `loadConfFromKeyVault` so that correct
  URL for jobs orchestrator service (`edgeJobs`) is generated.
* Setting `loadConfFromKeyVault` to `true` in conjunction with setting `azure.auth.required` to `false` will
  result in an error. This is because for loading configuration from Azure Key Vault we require both
  `azure.auth.servicesApp.appId` and `azure.auth.servicesApp.secret`.
* You should use `loadConfFromKeyVault` only when Azure environment has been created for the same version
  (major and minor) of Azure Industrial IoT components. That is, you should not use it to install the chart that
  deploys `2.8.x` or `2.7.x` version of components to the environment that has been created for `2.6.x` version of
  components.

| Parameter              | Description                                                                                          | Default |
|------------------------|------------------------------------------------------------------------------------------------------|---------|
| `loadConfFromKeyVault` | Determines whether components of Azure Industrial IoT should load configuration from Azure Key Vault | `false` |

### External Service URL

External Service URL is URL on which components of Azure Industrial IoT solution will be available externally.
This parameter is required so that Publisher Edge Module can communicate with jobs orchestrator service
(`edgeJobs`) for reporting its status and requesting publisher jobs. If parameter is not set, then Publisher
Edge Module will be presented with a Kubernetes internal URL, which will be accessible only from within
Kubernetes cluster. Format of Kubernetes internal URL is `http://<service_name>.<namespace>:<service_port>`.

**Documentation**: [Publisher Edge Module](../../../docs/modules/publisher.md)

| Parameter            | Description                                                                | Default |
|----------------------|----------------------------------------------------------------------------|---------|
| `externalServiceUrl` | URL on which components of Azure Industrial IoT solution will be available | `null`  |

### RBAC

| Parameter     | Description                            | Default |
|---------------|----------------------------------------|---------|
| `rbac.create` | If true, create and use RBAC resources | `true`  |

### Service Account

| Parameter               | Description                                         | Default |
|-------------------------|-----------------------------------------------------|---------|
| `serviceAccount.create` | If true, create and use ServiceAccount resources    | `true`  |
| `serviceAccount.name`   | Name of the server service account to use or create | `null`  |

### Application Runtime Configuration

Those are application runtime configuration parameters, mostly ASP.NET Core specific. They define the
following aspects of application runtime for microservices:

* URL path base: this determines URL path base that specified microservice with an external APIs should
  run on. So for example, if the value for `registry` component is `/registry`, then a sample API path
  would be `/registry/v2/applications`.

  > **NOTE:**: values of `apps.urlPathBase` should be aligned with value of `deployment.ingress.paths`.
  They are separated because one might want to have a regex in Ingress path.

* [Processing of forwarded headers](https://docs.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer).
  This feature enables components with APIs to determine origin incoming HTTP requests.

* OpenAPI server host. If set, it determines OpenAPI (Swagger) server host that should be used for serving
  OpenAPI definitions and performing API calls from Swagger UI. If the parameter is not set, then we will
  use Host header of incoming request as server host for OpenAPI definitions and Swagger UI.

  This value is useful, if services are behind a reverse proxy that does not properly apply HTTP forwarded
  headers (X-Forwarded-*). In this case, our microservices will not be able to determine original host that
  request came to to determine server host for OpenAPI definitions. So with this parameter you can enforce
  value of server host that should be used.

| Parameter                                       | Description                                                                      | Default           |
|-------------------------------------------------|----------------------------------------------------------------------------------|-------------------|
| `apps.urlPathBase.registry`                     | URL path base for `registry` component                                           | `/registry`       |
| `apps.urlPathBase.twin`                         | URL path base for `twin` component                                               | `/twin`           |
| `apps.urlPathBase.history`                      | URL path base for `history` component                                            | `/history`        |
| `apps.urlPathBase.publisher`                    | URL path base for `publisher` component                                          | `/publisher`      |
| `apps.urlPathBase.edgeJobs`                     | URL path base for `edgeJobs` component                                           | `/edge/publisher` |
| `apps.urlPathBase.events`                       | URL path base for `events` component                                             | `/events`         |
| `apps.urlPathBase.engineeringTool.`             | URL path base for `engineeringTool` component                                    | `/frontend`       |
| `apps.aspNetCore.forwardedHeaders.enabled`      | Determines whether processing of HTTP forwarded headers should be enabled or not | `true`            |
| `apps.aspNetCore.forwardedHeaders.forwardLimit` | Determines limit on number of entries in HTTP forwarded headers                  | `1`               |
| `apps.openApi.serverHost`                       | Determines OpenAPI (Swagger) server host                                         | `null`            |

### Deployed Components

**Documentation**: [Azure Industrial IoT Platform Components](../../../docs/services/readme.md)

Azure Industrial IoT comprises of fifteen micro-services that this chart will deploy as
[Deployment](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/) resources. Eight of them
expose web APIs, and one has a UI. And for those nine the chart will also create [Service](https://kubernetes.io/docs/concepts/services-networking/service/)
resources. For those nine we also provide one [Ingress](https://kubernetes.io/docs/concepts/services-networking/ingress/)
that can be enabled.

All micro-services have the same configuration parameters in `values.yaml`, so we will list them only for
one service (`registry`) bellow. The ones that also have a Service resource associated with them have
additional configuration parameters for that. But again, we will list configuration parameters for Service
resource only for one micro-service (`registry`). Please consult `values.yaml` for detailed view of all
parameters.

Here is the list of all Azure Industrial IoT components that are deployed by this chart. Currently only
`engineeringTool` is disabled by default.

| Name in `values.yaml`   | Description                                                                 | Enabled by Default |
|-------------------------|-----------------------------------------------------------------------------|--------------------|
| `registry`              | [Registry Microservice](../../../docs/services/registry.md)                 | `true`             |
| `sync`                  | [Registry Synchronization Agent](../../../docs/services/registry-sync.md)   | `true`             |
| `twin`                  | [OPC Twin Microservice](../../../docs/services/twin.md)                     | `true`             |
| `history`               | [OPC Historian Access Microservice](../../../docs/services/twin-history.md) | `true`             |
| `publisher`             | [OPC Publisher Service](../../../docs/services/publisher.md)                | `true`             |
| `events`                | [Events Service](../../../docs/services/events.md)                          | `true`             |
| `edgeJobs`              | [Publisher jobs orchestrator service](../../../docs/services/publisher.md)  | `true`             |
| `onboarding`            | [Onboarding Processor](../../../docs/services/processor-onboarding.md)      | `true`             |
| `eventsProcessor`       | [Edge Event Processor](../../../docs/services/processor-events.md)          | `true`             |
| `telemetryProcessor`    | [Edge Telemetry processor](../../../docs/services/processor-telemetry.md)   | `true`             |
| `engineeringTool`       | [Engineering Tool](../../../docs/services/engineeringtool.md)               | `false`            |

#### Deployment Resource Configuration

[Deployment](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/) resource parameters in `values.yaml`.

Use names from Azure Industrial IoT components list instead of `registry` for parameters for a different
micro-service.

| Parameter                                                 | Description                                                              | Default                    |
|-----------------------------------------------------------|--------------------------------------------------------------------------|----------------------------|
| `deployment.microServices.registry.enabled`               | If true, resources associated with `registry` component will be created  | `true`                     |
| `deployment.microServices.registry.deploymentAnnotations` | Annotations for the Deployment resource                                  | `{}`                       |
| `deployment.microServices.registry.podAnnotations`        | Annotations for the Pod within the Deployment resource                   | `{}`                       |
| `deployment.microServices.registry.extraLabels`           | Extra labels for the Deployment resource (will also be added to the Pod) | `{}`                       |
| `deployment.microServices.registry.replicas`              | Number of replicas                                                       | `1`                        |
| `deployment.microServices.registry.imageRepository`       | Docker Image Repository                                                  | `iot/opc-registry-service` |
| `deployment.microServices.registry.extraArgs`             | Extra arguments to pass to the Pod                                       | `[]`                       |
| `deployment.microServices.registry.extraEnv`              | Extra environment variables to set for the Pod                           | `[]`                       |
| `deployment.microServices.registry.resources`             | Definition of resource requests and limits for the Pod                   | `{}`                       |

Please note that the only parameter that has different values for different components is `imageRepository`.
Those are the values of `imageRepository` for all components:

| Configuration Parameter for Components                             | Default Image Repository                       |
|--------------------------------------------------------------------|------------------------------------------------|
| `deployment.microServices.registry.imageRepository`                | `iot/opc-registry-service`                     |
| `deployment.microServices.sync.imageRepository`                    | `iot/opc-registry-sync-service`                |
| `deployment.microServices.twin.imageRepository`                    | `iot/opc-twin-service`                         |
| `deployment.microServices.history.imageRepository`                 | `iot/opc-history-service`                      |
| `deployment.microServices.publisher.imageRepository`               | `iot/opc-publisher-service`                    |
| `deployment.microServices.edgeJobs.imageRepository`                | `iot/opc-publisher-edge-service`               |
| `deployment.microServices.events.imageRepository`                  | `iot/industrial-iot-events-service`            |
| `deployment.microServices.onboarding.imageRepository`              | `iot/opc-onboarding-service`                   |
| `deployment.microServices.eventsProcessor.imageRepository`         | `iot/industrial-iot-events-processor`          |
| `deployment.microServices.telemetryProcessor.imageRepository`      | `iot/industrial-iot-telemetry-processor`       |
| `deployment.microServices.engineeringTool.imageRepository`         | `iot/industrial-iot-frontend`                  |

#### Service Resource Configuration

[Service](https://kubernetes.io/docs/concepts/services-networking/service/) resource parameters in `values.yaml`.

Use names from Azure Industrial IoT components list instead of `registry` for parameters for a different
micro-service.

| Parameter                                                            | Description                                                                                                                                                | Default     |
|----------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------|
| `deployment.microServices.registry.service.annotations`              | Annotations for the Service resource                                                                                                                       | `{}`        |
| `deployment.microServices.registry.service.type`                     | Type of Service                                                                                                                                            | `ClusterIP` |
| `deployment.microServices.registry.service.port`                     | Port that service will be exposing                                                                                                                         | `9042`      |
| `deployment.microServices.registry.service.clusterIP`                | [Cluster IP](https://kubernetes.io/docs/concepts/services-networking/service/#choosing-your-own-ip-address) address of the Service                         | `null`      |
| `deployment.microServices.registry.service.externalIPs`              | [External IPs](https://kubernetes.io/docs/concepts/services-networking/service/#external-ips) for Service                                                  | `[]`        |
| `deployment.microServices.registry.service.loadBalancerIP`           | Load balancer IP address for Services of type [`LoadBalancer`](https://kubernetes.io/docs/concepts/services-networking/service/#loadbalancer)              | `null`      |
| `deployment.microServices.registry.service.loadBalancerSourceRanges` | Client IPs can access the Network Load Balancer                                                                                                            | `[]`        |
| `deployment.microServices.registry.service.nodePort`                 | Port to be used as the service NodePort, used for Services of type [`NodePort`](https://kubernetes.io/docs/concepts/services-networking/service/#nodeport) | `null`      |

Please note that the only parameter that has different values for different components is `port`.
Those are the service ports exposed by components:

| Configuration Parameter for Components                       | Default Service Port |
|--------------------------------------------------------------|----------------------|
| `deployment.microServices.registry.service.port`             | `9042`               |
| `deployment.microServices.twin.service.port`                 | `9041`               |
| `deployment.microServices.history.service.port`              | `9043`               |
| `deployment.microServices.publisher.service.port`            | `9045`               |
| `deployment.microServices.edgeJobs.service.port`             | `9046`               |
| `deployment.microServices.events.service.port`               | `9050`               |
| `deployment.microServices.engineeringTool.service.httpPort`  | `80`                 |
| `deployment.microServices.engineeringTool.service.httpsPort` | `443`                |

#### Ingress Resource Configuration

Our Ingress resource template uses
[fanout](https://kubernetes.io/docs/concepts/services-networking/ingress/#simple-fanout)
configuration to expose components with web APIs or UI.

Here are [Ingress](https://kubernetes.io/docs/concepts/services-networking/ingress/) resource parameters
in `values.yaml`. Note that Ingress is disabled by default.

| Parameter                                  | Description                                                                                                 | Default           |
|--------------------------------------------|-------------------------------------------------------------------------------------------------------------|-------------------|
| `deployment.ingress.enabled`               | If true, one Ingress resource will be created for enabled Services                                          | `false`           |
| `deployment.ingress.hostName`              | Host for the Ingress rule, multiple hosts are not supported                                                 | `null`            |
| `deployment.ingress.extraLabels`           | Extra labels for the Ingress resource                                                                       | `{}`              |
| `deployment.ingress.annotations`           | Annotations for the Ingress resource                                                                        | `{}`              |
| `deployment.ingress.tls`                   | Ingress [TLS configuration](https://kubernetes.io/docs/concepts/services-networking/ingress/#tls)           | `[]`              |
| `deployment.ingress.paths.registry`        | Path on which `registry` component should be exposed. Should be set to enable for `registry`.               | `/registry`       |
| `deployment.ingress.paths.twin`            | Path on which `twin` component should be exposed. Should be set to enable for `twin`.                       | `/twin`           |
| `deployment.ingress.paths.history`         | Path on which `history` component should be exposed. Should be set to enable for `history`.                 | `/history`        |
| `deployment.ingress.paths.publisher`       | Path on which `publisher` component should be exposed. Should be set to enable for `publisher`.             | `/publisher`      |
| `deployment.ingress.paths.events`          | Path on which `events` component should be exposed. Should be set to enable for `events`.                   | `/events`         |
| `deployment.ingress.paths.edgeJobs`        | Path on which `edgeJobs` component should be exposed. Should be set to enable for `edgeJobs`.               | `/edge/publisher` |
| `deployment.ingress.paths.engineeringTool` | Path on which `engineeringTool` component should be exposed. Should be set to enable for `engineeringTool`. | `/frontend`       |

> **NOTE:** `deployment.ingress.paths` values here should be aligned with value of `apps.urlPathBase`. They are separated because one might want to have a regex in Ingress paths.

If you are using [NGINX Ingress Controller](https://www.nginx.com/products/nginx/kubernetes-ingress-controller/),
below are reference values for `deployment.ingress`. Please check
[special notes on NGINX Ingress Controller](#nginx-ingress-controller) for more details.

```yaml
deployment:
  ingress:
    enabled: true
    annotations:
      kubernetes.io/ingress.class: nginx
      nginx.ingress.kubernetes.io/affinity: cookie
      nginx.ingress.kubernetes.io/session-cookie-name: affinity
      nginx.ingress.kubernetes.io/session-cookie-expires: "14400"
      nginx.ingress.kubernetes.io/session-cookie-max-age: "14400"
      nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
      nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
      nginx.ingress.kubernetes.io/proxy-connect-timeout: "30"
```

### Prometheus

The following value determines whether the chart adds Pod annotations for Prometheus metrics scraping or not.
By default metrics scraping is enabled.

| Parameter           | Description                                                            | Default |
|---------------------|------------------------------------------------------------------------|---------|
| `prometheus.scrape` | If true, Pod annotations will be added for Prometheus metrics scraping | `true`  |

If enabled, here are Pod annotations that the chart will add (those are for `registry` component):

```yaml
prometheus.io/scrape: "true"
prometheus.io/path: "/metrics"
prometheus.io/port: "9042"
```

### Minimal Configuration

Below is a reference of minimal `values.yaml` to provide to the chart. You have to **change all value
in `<>`** with the ones obtained by running commands in [Prerequisites](#prerequisites) section:

> **NOTE:** `values.yaml` sample bellow explicitly **disables** authentication for the components
(`azure.auth.required=false`). For any production deployment of Azure Industrial IoT solution it is required
that AAD App Registrations are created and details are provided to the chart. And we strongly recommend
having those for non-production deployments as well, particularly if you have enabled Ingress.

```yaml
azure:
  tenantId: <TenantId>

  iotHub:
    eventHub:
      endpoint: <IoTHubEventHubEndpoint>
      consumerGroup:
        events: <IoTHubEventHubEventsConsumerGroup>
        telemetry: <IoTHubEventHubTelemetryConsumerGroup>
        onboarding: <IoTHubEventHubOnboardingConsumerGroup>

    sharedAccessPolicies:
      iothubowner:
        connectionString: <IoTHubConnectionString>

  cosmosDB:
    connectionString: <CosmosDBConnectionString>

  storageAccount:
    connectionString: <StorageAccountConnectionString>

  eventHubNamespace:
    sharedAccessPolicies:
      rootManageSharedAccessKey:
        connectionString: <EventHubNamespaceConnectionString>

    eventHub:
      name: <EventHubName>
      consumerGroup:
        telemetryUx: <EventHubTelemetryUxConsumerGroup>

  serviceBusNamespace:
    sharedAccessPolicies:
      rootManageSharedAccessKey:
        connectionString: <ServiceBusNamespaceConnectionString>

  keyVault:
    uri: <KeyVaultURI>

  signalR:
    connectionString: <SignalRConnectionString>
    serviceMode: <SignalRServiceMode>

  auth:
    required: false
```

## Special Notes

### Resource Requests And Limits

Helm chart does not set any [resource requests or limits](https://kubernetes.io/docs/concepts/configuration/manage-compute-resources-container/)
for Pod Containers. We recommend to set memory requests to at least `"64Mi"` and cpu requests to at least
`"20m"`. If you set resource limits, be informed that some components might require up to `"256Mi"` of
memory, such as `engineeringTool`, `registry`, `publisher` and a few others.

### Data Protection

> Data protection functionality is present only if `engineeringTool` component is enabled.

[Data protection](https://docs.microsoft.com/aspnet/core/security/data-protection/introduction?view=aspnetcore-3.1)
is an ASP.Net Core feature that is used by `engineeringTool` component. It takes care of encryption of cookies that
are used by the `engineeringTool`.

#### Azure Storage Account Container

Reference: [Data Protection Container](#data-protection-container-(optional))

We use Azure Storage as
[storage provider](https://docs.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-3.1#azure-storage)
for storing data protection keys.

You can specify the name of Azure Storage Container to be used for storing keys or the value will default to `dataprotection`.
If it doesn't already exist, this Azure Storage Container will be created automatically on startup of `engineeringTool` component.
Configuration parameter for data protection Azure Storage Container is
`azure.storageAccount.container.dataProtection.name`.

#### Azure Key Vault Key

Reference: [Data Protection Key](#data-protection-key-(optional))

We use a key in Azure Key Vault to protect keys that are stored in
[data protection Azure Storage Container](#data-protection-container-(optional)).

You can specify the name of the key in Azure Key Vault to be used or the value will default to `dataprotection`.
If it doesn't already exist, this key in Azure Key Vault will be created automatically on startup of `engineeringTool` component
Configuration parameter for data protection key in Azure Key Vault is `azure.keyVault.key.dataProtection`.

### Swagger

Ten of our components provide Swagger interfaces for trying out APIs. URL path for Swagger has the
following general structure:

`/<url_path_base>/swagger/index.html`

Note that `<url_path_base>` above corresponds to value of `apps.urlPathBase.<component_name>` parameter
of `values.yaml`. Consult [Application Runtime Configuration](#application-runtime-configuration) for
default values for components.

To open Swagger UI for a specific component you can:

* Use [kubectl port-forward](https://kubernetes.io/docs/tasks/access-application-cluster/port-forward-access-application-cluster/#forward-a-local-port-to-a-port-on-the-pod)
  to forward local port to a port on the Service or Pod. For example, for `registry` Service the command
  would look something like this:

  ```bash
  $ kubectl port-forward --namespace azure-iiot-ns svc/aiiot-registry 9042
  ```

  Then you can access Swagger UI for `registry` component on the following URL:
  `http://localhost:9042/registry/swagger/index.html`

* If Ingress is enabled, Swagger UI can be access with Ingress address. You can get the address with
  a command like this:

  ```bash
  $ kubectl get ingresses --namespace azure-iiot-ns
  NAME            HOSTS   ADDRESS         PORTS   AGE
  aiiot-ingress   *       40.XXX.XX.XXX   80      26d
  ```

  Then, if the Ingress address is accessible from your network, you can access Swagger UI for `registry`
  component on the following URL: `https://40.XXX.XX.XXX/registry/swagger/index.html`

Here is the full list of components with Swagger UIs:

| Component       | Default Service Port | Default Swagger UI Path             |
|-----------------|----------------------|-------------------------------------|
| `registry`      | `9042`               | `/registry/swagger/index.html`      |
| `twin`          | `9041`               | `/twin/swagger/index.html`          |
| `history`       | `9043`               | `/history/swagger/index.html`       |
| `publisher`     | `9045`               | `/publisher/swagger/index.html`     |
| `events`        | `9050`               | `/events/swagger/index.html`        |
| `edgeJobs`      | `9051`               | `/edge/publisher/swagger/index.html`|

### NGINX Ingress Controller

We tested Azure Industrial IoT solution with NGINX Ingress Controller. And it required a few configuration
tweaks to make Ingress work smoothly.

#### Controller configuration

The following values need to be added to NGINX Ingress Controller configuration
[ConfigMap](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/configmap/).

```json
  "data": {
    "use-forward-headers": "true",
    "compute-full-forward-for": "true",
    "proxy-buffer-size": "32k",
    "client-header-buffer-size": "32k"
  }
```

This configuration makes sure that:

* processing of forwarded headers is enabled:
  * [`use-forward-headers`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/configmap/#use-forwarded-headers)
  * [`compute-full-forward-for`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/configmap/#compute-full-forwarded-for)

* authentication response from Azure AAD is delivered to components:
  * [`proxy-buffer-size`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/configmap/#proxy-buffer-size)

* WebSocket connection is initialized and working properly for [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor).
  Blazor is framework that we use in `engineeringTool` component.
  * [`client-header-buffer-size`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/configmap/#client-header-buffer-size)

#### Ingress Annotations

We recommend setting the following addition [Ingress annotations](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/)
through `deployment.ingress.annotations` in `values.yaml` if you are using NGINX Ingress Controller. Note
that `nginx.ingress.kubernetes.io/*` annotations are there to enable smooth functionality of `engineeringTool`
component. If `engineeringTool` is not enabled, you can omit those and only keep `kubernetes.io/ingress.class: nginx`.

```yaml
deployment:
  ingress:
    enabled: true
    annotations:
      kubernetes.io/ingress.class: nginx
      nginx.ingress.kubernetes.io/affinity: cookie
      nginx.ingress.kubernetes.io/session-cookie-name: affinity
      nginx.ingress.kubernetes.io/session-cookie-expires: "14400"
      nginx.ingress.kubernetes.io/session-cookie-max-age: "14400"
      nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
      nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
      nginx.ingress.kubernetes.io/proxy-connect-timeout: "30"
```

These annotations make sure that:

* Sticky sessions are enabled. This is required for Blazor:
  * [`nginx.ingress.kubernetes.io/affinity`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#session-affinity)
  * [`nginx.ingress.kubernetes.io/session-cookie-name`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#cookie-affinity)
  * [`nginx.ingress.kubernetes.io/session-cookie-expires`](https://kubernetes.github.io/ingress-nginx/examples/affinity/cookie/)
  * [`nginx.ingress.kubernetes.io/session-cookie-max-age`](https://kubernetes.github.io/ingress-nginx/examples/affinity/cookie/)

  Here are Microsoft recommendations for [hosting a Blazor server in Kubernetes](https://docs.microsoft.com/aspnet/core/host-and-deploy/blazor/server?view=aspnetcore-3.1#kubernetes).

  Here are general NGINX Ingress Controller [Ingress Annotations for sticky sessions](https://kubernetes.github.io/ingress-nginx/examples/affinity/cookie/).

* WebSocket connection is kept alive. For this we set [adequate timeout values](https://kubernetes.github.io/ingress-nginx/user-guide/miscellaneous/#websockets) for:
  * [`nginx.ingress.kubernetes.io/proxy-read-timeout`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#custom-timeouts)
  * [`nginx.ingress.kubernetes.io/proxy-send-timeout`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#custom-timeouts)
  * [`nginx.ingress.kubernetes.io/proxy-connect-timeout`](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#custom-timeouts)

  Here are general NGINX Ingress Controller
  [recommendations for WebSocket](https://kubernetes.github.io/ingress-nginx/user-guide/miscellaneous/#websockets).
