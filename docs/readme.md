# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

### Discover, register and manage your Industrial Assets with Azure

Azure [Industrial IoT](industrial-iot-components.md) allows plant operators to discover OPC UA enabled servers in a factory network and register them in Azure IoT Hub.  

### Analyze, react to events, and control equipment from anywhere

Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world.  The Microservices' REST APIs mirror the [OPC UA](opcua.md) services edge-side and are secured using OAUTH authentication and authorization backed by Azure Active Directory (AAD).  This enables your cloud applications to browse server address spaces or read/write variables and execute methods using HTTPS and simple OPC UA JSON payloads.  

### Simple developer experience

The [REST API](api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices.  For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

### Manage certificates and trust groups

Azure Industrial IoT manages OPC UA Application Certificates and Trust Lists of factory floor machinery and control systems to keep OPC UA client to server communication secure. It restricts which client is allowed to talk to which server.  Storage of private keys and signing of certificates is backed by Azure Key Vault, which supports hardware based security (HSM).

## Get started

Deploying Azure Industrial IoT includes deploying the Azure Industrial IoT Microservices to Azure and the required edge modules to Azure IoT Edge.

To get started

* [Deploy the platform components and Edge Gateway](deploy/readme.md) 
* [Run through the Tutorials](tutorials/readme.md).

## Learn more

* Read up on the Azure Industrial IoT platform [architecture](architecture.md) and [flow](architecture-flow.md).
* Read about the [Industrial IoT cloud Microservices](services/readme.md)
  * [What is OPC Twin?](services/twin.md)
  * [What is OPC Vault?](services/vault.md)
* [Explore and work with the REST API](api/readme.md)
* [Explore the code structure](code-structure.md)
