# Azure Industrial IoT Cloud Platform

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/Azure.Industrial-IoT?branchName=master)](https://msazure.visualstudio.com/One/_build/latest?definitionId=86580&branchName=master)

## Welcome!

The Azure Industrial IoT Cloud Platform is a Microsoft product that has fully embraced openness. We use Azure's managed Platform as a Service (PaaS) services, open-source software leveraging the MIT license throughout, open international standards for communication (OPC UA, MQTT) and interfaces (Open API) and open industrial data models (OPC UA) on the edge and in the cloud.

## Discover, register and manage your industrial assets with Azure

The Azure Industrial IoT Cloud Platform allows you to discover industrial assets on-site and automatically registers them in the cloud for easy access there. It leverages managed Azure PaaS services so you don't have to manage the platform yourself. On top of the Azure PaaS services, we have built a number of edge and cloud services that work in harmony, leveraging OPC UA as the data model. This is also the first cloud platform to leverage the OPC UA PubSub telemetry format (both JSON and binary, on top of MQTT). If your assets don't support OPC UA as an interface, we have worked with our large partner network to support all types of industrial interfaces through the use of adapters, fully integrated with our platform. Please check out the [Azure IoT Edge Marketplace](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/category/internet-of-things?page=1&subcategories=iot-edge-modules) for a list of adapters available.

The edge services are implemented as Azure IoT Edge modules and run on on-premises [Kubernetes](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-kubernetes) (currently in preview), or stand-alone on an on-premises gateway that supports Docker. The  cloud services are implemented as ASP.NET micro-services with a REST interface and run on managed Azure Kubernetes Services (currently in preview) or stand-alone on Azure App Service. For both edge and cloud services, we have provided pre-built Docker containers in the Microsoft Container Registry (MCR), so you don't have to build them yourself. The edge and cloud services are leveraging each other and must be used together. We have also provided easy-to-use deployment scripts that allow you to deploy the entire platform in a step-by-step fashion.

We have also built a configuration application running on Azure that lets you access the services through a simple UI.

## Getting started

- [Read the documentation](https://azure.github.io/Industrial-IoT/)
- Releases can be found [here](https://github.com/Azure/Industrial-IoT/releases)
- [Deploy the pre-built services, the configuration application and edge gateways](docs/deploy/readme.md)
- Explore other Azure Industrial IoT products using this platform
  - [Connected Factory](https://github.com/Azure/Azure-IoT-Connected-Factory) Solution Accelerator which you deploy from [here](https://www.azureiotsolutions.com/Accelerators).
  - [OPC Vault Application](https://github.com/Azure/azure-iiot-opc-vault-service/tree/master/app)

## What you get when you deploy the platform

The minimal deployment script deploys the following managed Azure services into your Azure subscription:

- 1 IoT Hub with 4 partitions, S1 SKU (to communicate with the edge and ingress raw OPC UA telemetry data)
- 1 Key Vault, Premium SKU (to manage secrets and certificates)
- 1 Service Bus, Standard SKU (as integration event bus)
- 1 Event Hub with 4 partitions and 2 day retention, Standard SKU (contains processed and contextualized OPC UA telemetry data)
- 1 Cosmos DB with Session consistency level (to persist state that is not persisted in IoT Hub)
- 1 Blob Storage V2, Standard LRS SKU (for event hub checkpointing)

Optionally, Azure Kubernetes Services can be used to host the cloud micro-services (this is currently in preview, please see our documentation).

The full deployment script deploys the following additional managed Azure services into your Azure subscription:

- 1 Data Lake Storage V2, Standard LRS SKU (used to connect Power BI to the platform, see tutorial)
- 1 Time Series Insights, Pay As You Go SKU, 1 Scale Unit
- 1 Blob Storage V2, Standard LRS SKU (used for long-term storage for Time Series Insights)
- 1 App Service and 2 Azure Webapps, B3 SKU (for hosting the engineering tool application and all-in-on cloud services application)
- 1 SignalR, Standard SKU (used to scale out asynchronous API notifications)
- 3 VMs, B2 SKU (1 Linux IoT Edge gateway, 1 Windows IoT Edge gateway, 1 OPC UA server. (used for a factory simulation to show the capabilities of the platform and to generate sample telemetry)
- 1 Device Provisioning Service, S1 SKU (used for deploying and provisioning the simulation gateways)
- 1 Application Insights and 1 Log Analytics Workspace for Operations Monitoring

## Give feedback and report bugs

Please report any security related issues by following our [security](security.md) process.

Please enter all other bugs, feature requests, documentation issues, or suggestions as GitHub issues [here](https://github.com/Azure/Industrial-IoT/issues).

## Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](contributing.md). A friendly bot will remind you about it when you submit a pull-request.

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.  
