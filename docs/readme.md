# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

### Discover, register and manage your Industrial Assets with Azure

Azure [Industrial IoT](industrial-iot-components.md) allows plant operators to discover OPC UA enabled servers in a factory network and register them in Azure IoT Hub.  

### Analyze, react to events, and control equipment from anywhere

Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world.  The Microservices' REST APIs mirror the OPC UA services edge-side and are secured using OAUTH authentication and authorization backed by Azure Active Directory (AAD).  This enables your cloud applications to browse server address spaces or read/write variables and execute methods using HTTPS and simple OPC UA JSON payloads.  

### Simple developer experience

The [REST API](docs/api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices.  For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

### Manage certificates and trust groups

Azure Industrial IoT manages OPC UA Application Certificates and Trust Lists of factory floor machinery and control systems to keep OPC UA client to server communication secure. It restricts which client is allowed to talk to which server.  Storage of private keys and signing of certificates is backed by Azure Key Vault, which supports hardware based security (HSM).

## Architecture

Check out the Azure Industrial IoT platform [architecture](architecture.md) and [flow](architecture-flow.md).  The Platform includes several [Azure IoT Edge modules](modules/readme.md), which can be [deployed](howto-deploy-modules.md) and used in conjunction with the included cloud services, or standalone with Azure IoT Hub (but with limited functionality).   These include:

- [OPC Publisher](modules/publisher.md)
- [OPC Twin](modules/twin.md)

## Deploying Azure Industrial IoT

Deploying Azure Industrial IoT includes deploying the Azure Industrial IoT Microservices to Azure and the corresponding modules to Azure IoT Edge. The following articles provide more information about how to deploy both Microservices and edge modules in addition to their dependencies.

- [Deploy all Microservices and dependencies to Azure](howto-deploy-microservices.md)
- For development and testing purposes, one can also [deploy only the Microservices dependencies in Azure](howto-deploy-dependencies.md) and [run Microservices locally](howto-run-microservices-locally.md)
- [Deploy Azure Industrial IoT Edge modules](howto-deploy-modules.md)
- [Enable ASC for IoT](enable-asc-for-iot-and-sentinel-steps.md) to monitor security of OPC UA endpoints

## Learn more

- [Discover a server and browse its address space using the CLI](howto-use-cli.md).
- [Discover a server and browse its address space using Postman](howto-use-postman.md).
- [Industrial IoT cloud Microservices](services/readme.md)
  - [What is OPC Twin?](services/twin.md)
  - [What is OPC Vault?](services/vault.md)
- [Explore and work with the REST API](api/readme.md)
- [Explore the code structure](code-structure.md)

