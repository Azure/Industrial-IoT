# OPC UA Device Management Architecture

The following diagrams illustrate the OPC UA Device Management architecture and how its components interact.

## Discovery and Activation

1. The operator enables network scanning on the [OPC Twin module](module.md) or sends a one-time discovery using a discovery URL. The discovered endpoints and server application information is sent via telemetry to the onboarding agent for processing.  The [OPC UA onboarding agent](onboarding.md) processes these discovery events sent. The discovery events result in application registration and updates in Azure IoT Hub.  

   ![How OPC Twin works](media/twin1.png)

1. The operator inspects the certificate of the discovered endpoint and activates the registered endpoint twin for access using the Activation REST API of the [OPC Registry Micro service](registry.md).​ 

   ![How OPC Twin works](media/twin2.png)

## Interact with a Server Endpoint

1. Once activated, the operator can use the [OPC Twin service](twin.md) REST API to browse or inspect the server information model, read/write object variables and call methods.  The API expects the Azure IoT Hub identity of one of the registered Server endpoints.  

   ![How OPC Twin works](media/twin3.png)

1. The [OPC Twin service](twin.md) REST interface can also be used to create monitored items and subscriptions inside the OPC Publisher module. The OPC Publisher sends variable changes and events in the OPC UA server as telemetry to Azure IoT Hub. For more information about OPC Publisher, see the [OPC Publisher](https://github.com/Azure/iot-edge-opc-publisher) repository on GitHub. 

   ![How OPC Twin works](media/twin4.png)

## Next steps

- [Deploy OPC Device Management to Azure](../howto-deploy-services-md)
