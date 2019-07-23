# Azure Industrial IoT Microservices

Azure Industrial IoT Microservices use Azure IoT Edge and IoT Hub to connect the cloud and factory networks. 

These microservices use [Azure Industrial IoT OPC UA components](https://github.com/Azure/azure-iiot-opc-ua) to provide discovery, registration, and remote control of industrial devices through REST APIs.  Applications using the REST API do not require an OPC UA SDK, and can be implemented in any programming language and framework that can call an HTTP endpoint. 

Following are Microservices:

- [OPC Registry Microservice](registry.md)
- [OPC Onboarding Agent](onboarding.md)
- [OPC Twin Microservice](twin.md)
- [OPC Gateway](gateway.md)

## Next steps

- [Explore the Architecture](../architecture.md)
- [Deploy Microservices to Azure](../howto-deploy-microservices.md)
- [Register a server and browse its address space](howto-use-cli.md) 
- [Explore the REST API](../api/readme.md)
- [Explore the OPC UA source code](https://github.com/Azure/azure-iiot-opc-ua)