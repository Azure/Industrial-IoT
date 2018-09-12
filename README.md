# OPC Twin Core Packages (Preview)

This repo contains the core business logic for the OPC Twin Industrial IIoT services and modules and consists of the following packages:

* [Microsoft.Azure.IIoT.OpcUa](src/Microsoft.Azure.IIoT.OpcUa/src) contains shared and common code among...
  * [Microsoft.Azure.IIoT.OpcUa.Edge](src/Microsoft.Azure.IIoT.OpcUa.Edge/src) contains the services hosted by the [OPC Twin module](https://github.com/Azure/azure-iiot-opc-twin-module).
  * [Microsoft.Azure.IIoT.OpcUa.Twin](src/Microsoft.Azure.IIoT.OpcUa.Twin/src) is the business logic behind the [OPC Twin service](https://github.com/Azure/azure-iiot-opc-twin-service)
  * [Microsoft.Azure.IIoT.OpcUa.Registry](src/Microsoft.Azure.IIoT.OpcUa.Registry/src) is the core logic of the [OPC Twin Registry](https://github.com/Azure/azure-iiot-opc-twin-registry) service.

The consuming services are part of our [Industrial IoT (IIoT) solution accelerator components](#Other-Industrial-IoT-Solution-Accelerator-components) suite.

## Build

### Building with Visual Studio or VS Code

1. [Install .NET Core 2.1+][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017] [dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Open and build the solution file in Visual Studio or VS Code

### Building on the command line

1. [Install .NET Core 2.1+][dotnet-install]
1. Open a command line window or terminal session in the repo root
1. Run `dotnet build -c Release`
1. Run `dotnet pack`

## Other Industrial IoT Solution Accelerator components

* [OPC Twin service](https://github.com/Azure/azure-iiot-opc-twin-service)
* [OPC Twin Registry service](https://github.com/Azure/azure-iiot-opc-twin-registry)
* [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-onboarding)
* [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module)
* [OPC Twin API](https://github.com/Azure/azure-iiot-opc-twin-api)
* OPC GDS Vault service (Coming soon)
* [OPC Publisher IoT Edge module](https://github.com/Azure/iot-edge-opc-publisher)

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
[iotedge-url]: https://github.com/Azure/iotedge
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
