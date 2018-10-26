# OPC Twin Module

![Build Status](https://msazure.visualstudio.com/_apis/public/build/definitions/b32aa71e-8ed2-41b2-9d77-5bc261222004/33979/badge)

The OPC Twin module runs on the edge and provides serveral edge services to the [OPC Twin](https://github.com/Azure/azure-iiot-opc-twin-service) micro services. Core of the module is the Supervisor identity.

The supervisor manages endpoint twins, which correspond to OPC UA server endpoints that were activated in the OPC Twin registry.  These endpoint twins translate OPC UA JSON received from the Twin micro service running in the cloud into OPC UA binary messages which are sent over a stateful secure channel to the managed endpoint.

The supervisor also provides discovery services which send discovery events to the [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-service) for processing, where these events result in updates to the OPC Twin registry.

The OPC Twin module is intended to be part of a module deployment in [IoT Edge][iotedge-url].  For development and testing purposes it can be run standalone following the instructions [below](#Build-and-Run).  The OPC Twin module is part of our [Industrial IoT (IIoT) solution accelerator components](#Other-Industrial-IoT-components) suite.

## Getting started

### Install any tools and depdendencies

* [Install .NET Core 2.1+][dotnet-install] if you want to build the services locally.
* Install [Docker][docker-url] if you want to build and run the services using docker-compose.
* Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed. 

### Deploy Azure Services

Follow the instructions [here](https://github.com/Azure/azure-iiot-opc-twin-service) to deploy all required services and retrieve the module configuration information, in particular the value for the `PCS_IOTHUB_CONNSTRING` environment variable, which will be needed later on.

## Build and Run

### Building and running locally using Docker (Quick start mode)

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and ensure the .env file containing an entry for `PCS_IOTHUB_CONNSTRING` exists.
1. Start the module by running `docker-compose up`.

> Also, make sure you run one or more OPC UA servers in a network reachable from your development machine to utilize the OPC Twin capabilities.

### Build, run and deploy the module to IoT Edge (Production)

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and build the quick start docker image using `docker build -f docker/linux/amd64/Dockerfile -t azure-iiot-opc-twin-module .`
1. Push the module to an accessible registry, e.g. Azure Container Registry, or [dockerhub][dockerhub-url].
1. Follow the instructions on how to deploy the module to [IoT Edge][iotedge-docs-url]:
  * Instructions on how to [deploy](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal) a module to one or a set of IoT Edge targets.
  * Instructions on how to install IoT Edge [on Linux](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart-linux) and [on Windows](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart).

> Install one IoT Edge target per your factory network.  Make sure you run one or more OPC UA servers in the same network to utilize the OPC Twin capabilities.

### Building and running the module with Visual Studio or VS Code

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Set the `PCS_IOTHUB_CONNSTRING` environment variable in your system.
  * [This page][windows-envvars-howto-url] describes how to setup env vars in Windows.
  * For Linux and MacOS, we suggest to create a shell script to set up the environment variables each time before starting the service host (e.g. VS Code or docker). Depending on OS and terminal, there are ways to persist values globally, for more information [this](https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux), [this](https://help.ubuntu.com/community/EnvironmentVariables), or [this](https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x) page should help.
1. Open the solution in Visual Studio or VS Code
1. Configure the `Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli` project properties to pass `--host` as command line argument when starting. 
1. Start the `Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli` project (e.g. press F5).

## Other Industrial IoT components

* [OPC Twin services](https://github.com/Azure/azure-iiot-opc-twin-service)
* [OPC Twin API](https://github.com/Azure/azure-iiot-opc-twin-api)
* OPC Twin common business logic (Coming soon)
* [OPC Publisher IoT Edge module](https://github.com/Azure/iot-edge-opc-publisher)
* OPC Vault service (Coming soon)

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
