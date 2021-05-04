# Code Structure

[Home](readme.md)

The Industrial-IoT repository includes all Azure Industrial IoT Platform components:

* **Api**
  The Api folder contains projects and nuget packages that represent the API of the included Microservices. It also includes a handy Command Line Interface to excercise these APIs.
* **Common**
  The common folder includes utility functionality and abstractions used across the entire platform. This includes tools and functionality for Diagnostics, Networking, Crypto, Storage, Messaging as well as IoT Hub functionality and IoT Edge framework code.
* **Components**
  * **OPCUA**
    The OPC UA folder contains OPC UA abstractions and the wrapper around the OPC Foundation .net Standard stack. It also provides the business logic for the OPC UA related services.
* **Modules**
  * **OPC-Twin**
    The OPC Twin is an edge module that provides support for discovery and remote OPC UA service calls. It uses the edge and protocol components in the `Components/OPC UA` folder.
  * **OPC-Publisher**
    The OPC Publisher is an IoT Edge module that streams OPC UA data in the form of PubSub to IoT Hub for processing in downstream services. It uses the edge and protocol components in the `Components/OPC UA` folder.
* **Services**
  The services folder includes the Microservices and Agents running in Azure Cloud and communicating with the Edge modules. They utilize the business logic contained in the `Components` folder.

## Learn more

* [Deploy Azure Industrial IoT](deploy/readme.md)
