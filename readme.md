# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

### Discover, register and manage your Industrial Assets with Azure

Azure Industrial IoT allows plant operators to discover OPC UA enabled servers in a factory network and register them in Azure IoT Hub.  

### Analyze, react to events, and control equipment from anywhere

Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world.  The Microservices' REST APIs mirror the OPC UA services edge-side and are secured using OAUTH authentication and authorization backed by Azure Active Directory (AAD).  This enables your cloud applications to browse server address spaces or read/write variables and execute methods using HTTPS and simple OPC UA JSON payloads.  

### Simple developer experience

The [REST API](docs/api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices.  For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

### Manage certificates and trust groups

Azure Industrial IoT manages OPC UA Application Certificates and Trust Lists of factory floor machinery and control systems to keep OPC UA client to server communication secure. It restricts which client is allowed to talk to which server.  Storage of private keys and signing of certificates is backed by Azure Key Vault, which supports hardware based security (HSM).

## Architecture

The overall Azure Industrial IOT architecture looks like below.

  ![architecture](docs/media/architecture.PNG)

Details on how things work together can be found at [architectural code flow](docs/architecture.md).
## Components

[![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/Azure.Industrial-IoT?branchName=master)](https://msazure.visualstudio.com/One/_build/latest?definitionId=86580&branchName=master)

This repository includes the following Industrial IoT components:

* Cloud Management and Data Plane, including
   * [OPC Twin](docs/services/twin.md) Microservices provide discovery, registration, and remote control of industrial devices through REST APIs.  
   * [OPC Vault](docs/services/vault.md) enables secure communication among OPC UA enabled devices and the cloud. 
   * A REST based [API](docs/api/readme.md) to access service functionality.
* Edge components
  * [OPC Twin module](docs/modules/twin.md)
  * [OPC Publisher module](docs/modules/publisher.md)

## Learn more

* [Deploy Azure Industrial IoT](docs/readme.md)
* Explore the samples
  * [Connected Factory](https://github.com/Azure/Azure-IoT-Connected-Factory) Solution Accelerator which you can try out [here](https://www.azureiotsolutions.com/Accelerators).
  * [OPC Vault Dashboard](https://github.com/Azure/azure-iiot-opc-vault-service/tree/master/app)
  * [OPC Twin Browser](https://github.com/Azure/azure-iiot-opc-twin-webui)
* Read more about Industrial IoT Components [here](docs/industrial-iot-components.md)
* See the complete code structure [here](docs/code-structure.md)

### Give feedback and report bugs

Please report any security related issues by following our [Security](security.md) process.

Please enter all other bugs, documentation issues, or suggestions as GitHub Issues [here](https://github.com/Azure/Industrial-IoT/issues).   

### Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](docs/contributing.md). A friendly bot will remind you about it when you submit a pull-request.

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.  

