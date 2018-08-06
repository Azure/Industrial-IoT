# OPC Twin Module (Preview)

![Build Status](https://msazure.visualstudio.com/_apis/public/build/definitions/b32aa71e-8ed2-41b2-9d77-5bc261222004/33979/badge)

The OPC Twin module runs on the edge and provides serveral edge services to the [OPC Twin](https://github.com/Azure/azure-iiot-opc-twin-service) and [Registry](https://github.com/Azure/azure-iiot-opc-registry-service) micro services. Core of the module is the Supervisor identity.

The supervisor manages endpoint twins, which correspond to OPC UA server endpoints that were activated in the OPC Twin registry.  These endpoint twins translate OPC UA Json received from the Twin micro service running in the cloud into OPC UA binary messages which are sent over a stateful secure channel to the managed endpoint.

The supervisor also provides discovery services which send plug and play events to the [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-onboarding) for processing, where these events result in updates to the OPC Twin registry.

The OPC Twin module is intended to be part of a module deployment in [IoT Edge][iotedge-url].  For development and testing purposes it can be run standalone following the instructions [below](#Build-and-Run).  The OPC Twin module is part of our [Industrial IoT (IIoT) solution accelerator components](#Other-Industrial-IoT-Solution-Accelerator-components) suite.

## Prerequisites

### Deploy Azure Services

This service has a dependency on the following Azure resources:

* [Azure IoT Hub][iothub-docs-url]

Follow the instructions for [Deploy the Azure services][deploy-local] to deploy the required resources.

## Build, run, and deploy

### Building the module using Docker

1. Make sure [Docker][docker-url] is installed.
1. Make sure the [Prerequisites](#prerequisites) are set up.
1. Change into the repo root and build the docker image using `docker build -t azure-iiot-opc-twin-module .`
1. Push the module to an accesible registry, e.g. Azure Container Registry, or [dockerhub][dockerhub-url].

### Deploy the module to IoT Edge

For production scenarios build a [Docker](#Building-the-module-using-Docker) image, push it into a registry of your choice and deploy it to a set of IoT Edge devices in the previously created Azure IoT Hub instance.  Detailed IoT Edge help can be found [here][iotedge-docs-url], specifically:

* Instructions on how to [deploy](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal) a module to one or a set of IoT Edge targets.

* Instructions on how to install IoT Edge [on Linux](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart-linux) and [on Windows](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart).

> Install one IoT Edge target per your factory network.  For real world usage, make sure you run one or more OPC UA servers in the network.

### Building and running the module with Visual Studio or VS Code

1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. [Install .NET Core 2.1+][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017] [dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Set the required environment variables as per the instructions [here](#Development-configuration).
1. Open the solution in Visual Studio or VS Code
1. Start the `Microsoft.Azure.IIoT.OpcUa.Modules.Twin` project (e.g. press F5).

#### Development configuration

When running the module stand alone (for testing or development purposes), a Module identity connection string needs to be provided to the module (something the EdgeAgent would otherwise do).  There are two ways of doing this:

1. Assuming you have a Module connection string, you can set the following environment variable on your system to it:
    * `EdgeHubConnectionString` = { Module identity connection string }
1. If you do not have one, you can set the following environment variables and the module will create a new Module identity for you:
    * `EdgeDeviceId` = { The identity of the 'fake' IoT Edge *device* }
    * `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string from [Deploy Azure Services](#deploy-azure-services)}
        * More information on where to find your IoT Hub connection string can be found [here][iothub-connstring-blog].

More information on environment variables can be found [below](#Configuration-And-Environment-Variables).

## Configuration and Environment variables

The module can be configured in its [appsettings.json](src/appsettings.json) file.  Alternatively, all configuration can be overridden on the command line, or through environment variables.  

* [This page][windows-envvars-howto-url] describes how to setup env vars in Windows.
* For Linux and MacOS, we suggest to create a shell script to set up the environment variables each time before starting the service host (e.g. VS Code or docker). Depending on OS and terminal, there are ways to persist values globally, for more information [this](https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux), [this](https://help.ubuntu.com/community/EnvironmentVariables), or [this](https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x) page should help.

> Make sure to restart your editor or IDE after setting your environment variables to ensure they are picked up.

## Other Industrial IoT Solution Accelerator components

* OPC GDS service (Coming soon)
* [OPC Twin service](https://github.com/Azure/azure-iiot-opc-twin-service)
* [OPC Twin Registry service](https://github.com/Azure/azure-iiot-opc-registry-service)
* [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-onboarding)
* OPC Twin management agent service (Coming soon)
* OPC Twin common business logic (Coming soon)
* [OPC Publisher IoT Edge module](https://github.com/Azure/iot-edge-opc-publisher)
* [OPC Twin API](https://github.com/Azure/azure-iiot-opc-twin-api)

## Contributing

Refer to our [contribution guidelines](CONTRIBUTING.md).

## Feedback

Please enter issues, bugs, or suggestions as GitHub Issues [here](https://github.com/Azure/azure-iiot-opc-twin-service/issues).

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.

[run-with-docker-url]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#run-the-microservices-in-docker
[rm-arch-url]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-sample-walkthrough
[postman-url]: https://www.getpostman.com
[dockerhub-url]: https://dockerhub.io
[iotedge-url]: https://github.com/Azure/iotedge
[iotedge-docs-url]: https://docs.microsoft.com/azure/iot-edge/
[iothub-docs-url]: https://docs.microsoft.com/azure/iot-hub/
[docker-url]: https://www.docker.com/
[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017
[omnisharp-url]: https://github.com/OmniSharp/omnisharp-vscode
[windows-envvars-howto-url]: https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10
[iothub-connstring-blog]: https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub/
[deploy-rm]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy
[deploy-local]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#deploy-the-azure-services
[disable-auth]: https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/wiki/Developer-Reference-Guide#disable-authentication
