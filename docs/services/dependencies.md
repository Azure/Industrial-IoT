# Industrial IoT dependencies

The Industrial IoT Microservices depend on many other services. This includes

## OPC Vault

OPC Vault Microservice is used to provision OPC modules with official OPC UA client certificates and manages trust between servers and modules for secure communication.

## Azure IoT Hub and IoT Edge

The Azure IoT Hub is used as cloud broker for Edge to Cloud and Cloud to Edge messaging.  Edge components only need to open an outbound SSL connection to enable bidirectional services.   Edge modules are deployed through IoT Hub to [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/). to provide protocol translation and a management plane.

## Azure Storage

A storage account is used by the [onboarding](../services/onboarding.md) Microservice to persist Azure IoT Hub Event Hub Endpoint read offsets and partition information to support partitioned and reliable access from multiple instances.

## Azure Active Directory

All Microservices are registered as Application in Active Directory to integrate with Enterprise Authentication and Authorization policies.

## Next steps

- [Deploy dependencies for local development](../howto-deploy-dependencies.md)
- [Deploy Microservices](../howto-deploy-microservices.md)
- [Learn about Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/).