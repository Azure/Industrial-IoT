# Deploying Azure Industrial IoT Platform

[Home](../readme.md)

## Quickstart

The simplest way to get started is to deploy the [Azure Industrial IoT Platform and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md).
Unless you decide otherwise, it also deploys 2 simulated Edge Gateways and assets.

> The all in one hosting option is intended as a quick start solution. For production deployments that require staging, rollback, scaling and resilience you should deploy the platform into Kubernetes as explained [here](howto-deploy-aks.md).

To connect your physical Assets [install one ore more Azure IoT Edge Gateways](howto-install-iot-edge.md).

## What you get when you deploy the platform

The deployment script allows to select which set of components to deploy using deployment types. The dependencies and deployments types are explained below.

- Minimum dependencies:

  - 1 [IoT Hub](https://azure.microsoft.com/services/iot-hub/) to communicate with the edge and ingress raw OPC UA telemetry data
  - 1 [Key Vault](https://azure.microsoft.com/services/key-vault/), Premium SKU (to manage secrets and certificates)
  - 1 [Service Bus](https://azure.microsoft.com/services/service-bus/), Standard SKU (as integration event bus)
  - 1 [Event Hubs](https://azure.microsoft.com/services/event-hubs/) with 4 partitions and 2 day retention, Standard SKU (contains processed and contextualized OPC UA telemetry data)
  - 1 [Cosmos DB](https://azure.microsoft.com/services/cosmos-db/) with Session consistency level (to persist state that is not persisted in IoT Hub)
  - 1 [Blob Storage](https://azure.microsoft.com/services/storage/) V2, Standard LRS SKU (for event hub checkpointing)

- Standard dependencies: Minimum +

  - 1 [SignalR](https://azure.microsoft.com/services/signalr-service/), Standard SKU (used to scale out asynchronous API notifications)
  - 1 [Device Provisioning Service](https://docs.microsoft.com/azure/iot-dps/), S1 SKU (used for deploying and provisioning the simulation gateways)
  - 1 [Time Series Insights](https://azure.microsoft.com/services/time-series-insights), Pay As You Go SKU, 1 Scale Unit
  - Workbook, [Application Insights](https://azure.microsoft.com/services/monitor/) and Log Analytics Workspace for operations monitoring
  - 1 [Data Lake Storage](https://azure.microsoft.com/services/storage/data-lake-storage/) V2, Standard LRS SKU (used to connect Power BI to the platform, see tutorial)

- Microservices:

  - App Service Plan, 1 [App Service](https://azure.microsoft.com/services/app-service/), B1 SKU for hosting the cloud micro-services [all-in-one](../services/all-in-one.md)

- UI (Web app):

  - App Service Plan (shared with microservices), 1 [App Service](https://azure.microsoft.com/services/app-service/) for hosting the Industrial IoT Engineering Tool cloud application

- Simulation:

  - [Virtual machine](https://azure.microsoft.com/services/virtual-machines/), Virtual network, IoT Edge used for a factory simulation to show the capabilities of the platform and to generate sample telemetry. By default, 4 [Virtual Machines](https://azure.microsoft.com/services/virtual-machines/), 2 B2 SKU (1 Linux IoT Edge gateway and 1 Windows IoT Edge gateway) and 2 B1 SKU (factory simulation).

- [Azure Kubernetes Service](howto-deploy-aks.md) should be used to host the cloud microservices

The types of deployments are the following:

- `minimum`: Minimum dependencies
- `local`: Minimum and Standard dependencies
- `services`: `local` and Microservices
- `simulation`: Minimum dependencies and Simulation components
- `app`: `services` and UI components
- `all` (default): all the above components

## Other hosting and deployment methods

Alternative options to deploy the platform services include:

- Deploying Azure Industrial IoT Platform to [Azure Kubernetes Service (AKS)](howto-deploy-aks.md) as production solution.
- Deploying Azure Industrial IoT Platform microservices into an existing Kubernetes cluster [using Helm](howto-deploy-helm.md).
- Deploying [Azure Kubernetes Service (AKS) cluster on top of Azure Industrial IoT Platform created by deployment script and adding Azure Industrial IoT components into the cluster](howto-add-aks-to-ps1.md).
- For development and testing purposes, you can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](howto-run-microservices-locally.md).
