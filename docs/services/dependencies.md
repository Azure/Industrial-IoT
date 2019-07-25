# Industrial IoT dependencies

The Industrial IoT Microservices depend on several other services and technology. This includes

## OPC UA Reference Stack

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## Azure IoT Hub and IoT Edge

The Azure IoT Hub is used as cloud broker for Edge to Cloud and Cloud to Edge messaging.  Edge components only need to open an outbound SSL connection to enable bidirectional services.   Edge modules are deployed through IoT Hub to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/). to provide protocol translation and a management plane.

## Azure Storage

A storage account is used by the [onboarding](../services/onboarding.md) Microservice to persist Azure IoT Hub Event Hub Endpoint read offsets and partition information to support partitioned and reliable access from multiple instances.

## Azure Active Directory

All Microservices are registered as Application in Active Directory to integrate with Enterprise Authentication and Authorization policies.

## Next steps

* [Deploy dependencies for local development](../howto-deploy-dependencies.md)
* [Deploy Microservices](../howto-deploy-microservices.md)
* [Learn about Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/).
