# Code Structure

The repository (azure-iiot-components) includes all Azure Industrial IoT component repositories as its submodules which are:

- Components and protocol stacks including 
  - [OPC Unified Architecture (OPC UA)](https://github.com/Azure/azure-iiot-opc-ua)
- IoT Edge modules
  - [OPC Publisher module](https://github.com/Azure/iot-edge-opc-publisher)
  - [OPC Twin module](https://github.com/Azure/azure-iiot-opc-twin-module)
- [Microservices](https://github.com/Azure/azure-iiot-services)
  - [OPC Twin Microservices](docs/twin/readme.md)  and the complete architecture is [here](https://github.com/Azure/azure-iiot-components/blob/develop/docs/twin/architecture.md) 
      - [Registry Service](docs/twin/registry.md), [OPC Twin Service](docs/twin/twin.md), [OPC Onboarding Agent](docs/twin/onboarding.md), [OPC Gateway (preview)](docs/twin/gateway.md), OPC Historic Access Service
  - [OPC Vault Microservice](https://github.com/Azure/azure-iiot-opc-vault-service) 
- [API](docs/api/readme.md)

## Learn more 

* [Deploy Azure Industrial IoT](docs/readme.md)
* [Deploy the Microservices](docs/howto-deploy-microservices.md)


