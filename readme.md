# Azure Industrial IoT Platform

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/Azure.Industrial-IoT?branchName=main)](https://msazure.visualstudio.com/One/_build/latest?definitionId=86580&branchName=main)

The Azure Industrial IoT Platform is a Microsoft product that fully embraces openness. We use Azure's managed Platform as a Service (PaaS) services, open-source software leveraging the MIT license throughout, open international standards for communication (OPC UA, MQTT) and interfaces (OpenAPI) and open industrial data models (OPC UA) on the edge and in the cloud.

## Discover, register and manage your industrial assets with Azure

The Azure Industrial IoT Platform allows you to discover industrial assets on-site and automatically registers them in the cloud for easy access there. It leverages managed Azure PaaS services. On top of the Azure PaaS services, we have built a number of edge and cloud micro-services that must be used together, leveraging OPC UA as the data model. This is also the first cloud platform to leverage the OPC UA PubSub telemetry format (both JSON and binary, on top of MQTT). If your assets don't support OPC UA as an interface, we have worked with our large partner network to support all types of industrial interfaces through the use of adapters, fully integrated with our platform. Please check out the [Azure IoT Edge Marketplace](https://azuremarketplace.microsoft.com/marketplace/apps/category/internet-of-things?page=1&subcategories=iot-edge-modules). So far, we support modules from Softing and CopaData.

An overview architecture is depicted below:

![diagram](docs/media/IIoT-Diagram.png)

The edge services are implemented as Azure IoT Edge modules and run on on-premises. The cloud services are implemented as ASP.NET micro-services with a REST interface and run on managed Azure Kubernetes Services or stand-alone on Azure App Service. For both edge and cloud services, we have provided pre-built Docker containers in the Microsoft Container Registry (MCR), so you don't have to build them yourself. The edge and cloud services are leveraging each other and must be used together. We have also provided easy-to-use deployment scripts that allow you to deploy the entire platform in a step-by-step fashion.

We have also built an application running on Azure that lets you access the services through a simple UI.

## Getting started

### OPC Publisher - Standalone

To learn how to use OPC Publisher outside the context of Industrial IoT Platform (as container or IoT Edge module) please have a look [here](docs/modules/publisher.md).

### Industrial IoT Platform

To [deploy the Azure Industrial IoT Platform](docs/deploy/readme.md), clone the repository:

  ```bash
  git clone https://github.com/Azure/Industrial-IoT
  cd Industrial-IoT
  ```

And start the deployment

On Windows:

  ```pwsh
  .\deploy
  ```

On Linux:

  ```bash
  ./deploy.sh
  ```

For more information see the [detailed instructions](docs/deploy/howto-deploy-all-in-one.md) and [alternative deployment options](docs/deploy/readme.md).

For detailed documentation of Azure Industrial IoT Platform, please refer to [Operations Manual](docs/manual/readme.md).

### Learn more

- [Documentation and tutorials](https://azure.github.io/Industrial-IoT/).
- [Releases of the platform](https://github.com/Azure/Industrial-IoT/releases).

## Mitigations for known vulnerabilities

To mitigate known vulnerabilities external to the Industrial-IoT Platform please review [this](docs/security/readme.md) documentation.

## Get support

Please report any security related issues by following our [security process](security.md).

If you are an Azure customer, please create an Azure Support Request. More information can be found [here](https://azure.microsoft.com/en-us/support/create-ticket/). (Azure Support SLA apply).

Otherwise, please report bugs, feature requests, or suggestions as [GitHub issues](https://github.com/Azure/Industrial-IoT/issues). (No SLA available).

## Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](contributing.md). A friendly bot will remind you about it when you submit a pull-request.

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.  
