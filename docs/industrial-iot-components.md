# The Industrial IoT Platform and its Components

[Home](readme.md)

Industrial IoT or IIoT connects machines and devices in industries. This connectivity allows for data collection, exchange and analysis, potentially  improving productivity and efficiency. To read more about Microsoft Azure IoT, click [here](https://azure.microsoft.com/overview/iot/) and about Industry 4.0, [here](https://azure.microsoft.com/overview/iot/industry/discrete-manufacturing/).   

## Industrial IoT Components

The Industrial IoT platform heavily utilizes [OPC UA](opcua.md) as an open standard for connectivity and data modelling.   The platform encompasses:

### A set of Industrial IoT specific Microservices

The management and processing plane of the Industrial IoT platform is implement in the form of several Microservices which either provide daemon like services, such as processing data, or REST API's you can program against.   A list of these Microservices and more details around each can be found [here](services/readme.md).

### IoT Hub and other Azure services as as supporting Infrastructure

The IoT Hub acts as a central message hub for bi-directional communication between IoT application and the devices it manages. This is an open and flexible cloud platform as a service that supports open-source SDKs and multiple protocols. Read more about IoT Hub [here](https://azure.microsoft.com/en-us/services/iot-hub/).

Other Azure services the platform depends on are listed [here](services/dependencies.md).

### IoT Edge devices

An IoT Edge device acts as a local Gateway and is comprised of Edge Runtime and Edge modules. 

- *Edge modules* are docker containers which are the smallest unit of computation e.g. OPC Publisher and OPC Twin in our case. 
- *Edge device* is used to deploy such modules which act as mediator between OPC UA server and IoT Hub in cloud. More information about IoT Edge is [here](https://azure.microsoft.com/en-us/services/iot-edge/).

#### Industrial IoT Edge Modules

- *OPC Publisher*: The OPC Publisher runs inside IoT Edge. It connects to OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format to Azure IoT Hub. All transport protocols supported by the Azure IoT Hub client SDK can be used, i.e. HTTPS, AMQP and MQTT.
- *OPC Twin*: The OPC Twin consists of microservices that use Azure IoT Edge and IoT Hub to connect the cloud and the factory network. OPC Twin provides discovery, registration, and remote control of industrial devices through REST APIs. OPC Twin does not require an OPC Unified Architecture (OPC UA) SDK, is programming language agnostic, and can be included in a serverless workflow.
- *Discovery*: The discovery module, represented by the discoverer identity, provides discovery services on the edge which include OPC UA server discovery.  If discovery is configured and enabled, the module will send the results of a scan probe via the IoT Edge and IoT Hub telemetry path to the Onboarding service. The service processes the results and updates all related Identities in the Registry.

## Learn more

- [Complete code structure](code-structure.md)
- [Architecture](architecture.md)
- [Deploy Azure Industrial IoT Platform](deploy/readme.md)
