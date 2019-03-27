# What is Azure IoT OPC UA Device management?

Azure IoT OPC UA Device Management, also known as "**OPC Twin**", consists of several micro services that use Azure IoT Edge and IoT Hub to connect the cloud and factory networks. 

OPC UA Device Management uses [Azure Industrial IoT OPC UA components](https://github.com/Azure/azure-iiot-opc-ua) to provide discovery, registration, and remote control of industrial devices through REST APIs.  Applications using the REST API do not require an OPC UA SDK, and can be implemented in any programming language and framework that can call an HTTP endpoint. 

The OPC UA Device Management Services consist of the following services and edge modules (click on each link to get more information):

- [OPC Registry service](registry.md)
- [OPC Twin Edge Module](module.md)
- [OPC Onboarding Agent](onboarding.md)
- [OPC Twin service](twin.md)
- [OPC Gateway](gateway.md)

## Next steps

- [Explore the Architecture](architecture.md)
- [Deploy OPC UA Device Management services to Azure](../howto-deploy-services.md)
- [Register a server and browse its address space](howto-use-cli.md) 
- [Explore the REST API](../api/readme.md)
- [Explore the OPC UA source code](https://github.com/Azure/azure-iiot-opc-ua)