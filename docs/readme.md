# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

Azure [OPC Publisher](./opc-publisher/readme.md) and the optional [Industrial IoT Web service](./web-api/readme.md) allows plant operators to discover [OPC UA](opcua.md) enabled servers in a factory network and register them in Azure IoT Hub. Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world. The APIs mirror the [OPC UA](opcua.md) services and are secured through IoT Hub or optionally using OAUTH authentication and authorization backed by Azure Active Directory (AAD). This enables your applications to browse server address spaces or read/write variables and execute methods using IoT Hub, MQTT or HTTPS with simple JSON payloads.  

The [OPC Publisher API](./opc-publisher/readme.md) and the optional [Industrial IoT Web service REST API](./api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices. For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

## Getting started and learning more

* [Deploy the platform components and Edge Gateway](deploy/readme.md) 
* [Run through the tutorials](tutorials/readme.md)
* Learn more about [OPC Publisher](./opc-publisher/readme.md) and the optional [web api](./web-api/readme.md)
* Read the [Operations Manual](manual/readme.md)
