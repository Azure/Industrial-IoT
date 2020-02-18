# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/Azure.Industrial-IoT?branchName=master)](https://msazure.visualstudio.com/One/_build/latest?definitionId=86580&branchName=master)

## Features

### Discover, register and manage your Industrial Assets with Azure

Azure Industrial IoT allows plant operators to discover [OPC UA](docs/opcua.md) enabled servers in a factory network and register them in Azure IoT Hub.  

### Analyze, react to events, and control factory equipment from anywhere

Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world.  The Microservices' REST APIs mirror the OPC UA services edge-side and are secured using OAUTH authentication and authorization backed by Azure Active Directory (AAD).  This enables your cloud applications to browse server address spaces or read/write variables and execute methods using HTTPS and simple OPC UA JSON payloads.  

### Simple developer experience

The [REST API](docs/api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices.  For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

## Getting started

- [Deploy the platform components and Edge Gateway](docs/deploy/readme.md)
- Discover and register Industrial Assets
  - [Discover a server and browse its address space using the CLI](docs/howto-use-cli.md).
  - [Discover a server and browse its address space using Postman](docs/howto-use-postman.md).
- Subscribe to data and visualize the data (COMING SOON)
- [Read the docs](https://azure.github.io/Industrial-IoT/)
- Explore Industrial IoT projects using this platform
  - [Connected Factory](https://github.com/Azure/Azure-IoT-Connected-Factory) Solution Accelerator which you can try out [here](https://www.azureiotsolutions.com/Accelerators).
  - [OPC Vault Dashboard](https://github.com/Azure/azure-iiot-opc-vault-service/tree/master/app)

## Give feedback and report bugs

Please report any security related issues by following our [Security](security.md) process.

Please enter all other bugs, feature requests, documentation issues, or suggestions as GitHub Issues [here](https://github.com/Azure/Industrial-IoT/issues).

## Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](contributing.md). A friendly bot will remind you about it when you submit a pull-request.

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.  
