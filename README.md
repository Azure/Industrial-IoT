# Azure Industrial IoT OPC Twin Module

The OPC Twin module runs on the edge and provides several edge services to the [OPC Twin and Registry Services](https://github.com/Azure/azure-iiot-services).

Core of the module is the Supervisor identity.  The supervisor manages endpoint "twins", which correspond to OPC UA server endpoints that are activated using the corresponding OPC UA registry API.  These endpoint twins translate OPC UA JSON received from the Twin micro service running in the cloud into OPC UA binary messages which are sent over a stateful secure channel to the managed endpoint.  

The supervisor also provides discovery services which send device discovery events to the [OPC UA Device Onboarding service](https://github.com/Azure/azure-iiot-services) for processing, where these events result in updates to the OPC UA registry.

The OPC Twin module can be deployed in an [IoT Edge][iotedge-url] gateway.  For development and testing purposes it can also be run standalone following the instructions [below](#Build-and-Run).  

This module is part of our suite of [Azure IoT Industrial components](https://github.com/Azure/azure-iiot-components).

## Using the module

A pre-built image exits in Microsoft's container registry and can be obtained by running `docker pull mcr.microsoft.com/iotedge/opc-twin:latest`.  

To use it follow the instructions on how to deploy the module to [IoT Edge][iotedge-docs-url]:

* Instructions on how to [deploy](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal) a module to one or a set of IoT Edge targets.
* Instructions on how to install IoT Edge [on Linux](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart-linux) and [on Windows](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart).

> Install one IoT Edge gateway and module per factory network.  Make sure you run one or more OPC UA servers in the same network to utilize the OPC Twin capabilities.

## Build and Run

To build and run the module yourself, clone the repository.  Then...

### Install any tools and dependencies

* [Install .NET Core 2.2+][dotnet-install] if you want to build the services locally.
* Install [Docker][docker-url] if you want to build and run the services using docker-compose.
* Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
  * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url] installed (Windows only).
  * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.

### Deploy Azure Services

Follow the instructions [here](https://github.com/Azure/azure-iiot-services) to deploy all required services for local development.  Copy the resulting `.env` file into this repository's root (or into the parent folder).

### Building and running locally using Docker (Quick start mode)

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and ensure the .env file containing an entry for `PCS_IOTHUB_CONNSTRING` exists.
1. Start the module by running `docker-compose up`.

> Also, make sure you run one or more OPC UA servers in a network reachable from your development machine to utilize the OPC UA Twin capabilities.

### Build, run and deploy the module to IoT Edge (Production)

1. To build your own module container image and deploy it make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and run
   `docker build -f docker/linux/amd64/Dockerfile -t azure-iiot-opc-twin-module .`
1. Then push the module to an accessible registry, e.g. Azure Container Registry, or [dockerhub][dockerhub-url].
1. Follow the instructions on how to deploy the module to IoT Edge [here][#Using-the-module].

### Building and running the module with Visual Studio or VS Code

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and ensure the .env file containing an entry for `PCS_IOTHUB_CONNSTRING` exists.
1. Open the `azure-iiot-opc-twin-module.sln` solution file in Visual Studio or VS Code
1. Configure the `Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli` project properties to pass `--host` as command line argument when starting.
1. Set the `Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli` as startup project and start debugging (e.g. by pressing F5).

## Contributing

Refer to our [contribution guidelines](CONTRIBUTING.md).

## Feedback

Please enter issues, bugs, or suggestions as GitHub Issues [here](https://github.com/Azure/azure-iiot-services/issues).

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
