# Code Structure

The azure-iiot-components repository includes all Azure Industrial IoT component repositories as its submodules which are:

* Components and protocol stacks including
  * [OPC Unified Architecture (OPC UA)](https://github.com/Azure/azure-iiot-opc-ua)
* IoT Edge modules
  * [OPC Publisher module](https://github.com/Azure/iot-edge-opc-publisher)
  * [OPC Twin module](https://github.com/Azure/azure-iiot-opc-twin-module)
* [Microservices](https://github.com/Azure/azure-iiot-services)
  * [OPC Twin Microservices](services/twin.md)  and the complete architecture is [here](architecture.md)
    * [Registry Service](services/registry.md), [OPC Twin Service](services/twin.md), [OPC Onboarding Agent](services/onboarding.md), [OPC Gateway (preview)](services/gateway.md), OPC Historic Access Service
  * [OPC Vault Microservice](https://github.com/Azure/azure-iiot-opc-vault-service)
* [API](api/readme.md)

## Learn more

* [Deploy Azure Industrial IoT](readme.md)
* [Deploy the Microservices](howto-deploy-microservices.md)
