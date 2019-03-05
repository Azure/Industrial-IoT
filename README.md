# Azure Industrial IoT OPC UA Components

This repo contains the core business logic for the Azure Industrial IoT OPC UA services and modules and consists of the following packages:

* [Microsoft.Azure.IIoT.OpcUa](src/Microsoft.Azure.IIoT.OpcUa/src) contains shared and common code among...
  * [Microsoft.Azure.IIoT.OpcUa.Protocol](src/Microsoft.Azure.IIoT.OpcUa.Protocol/src) contains the OPC UA stack and protocol used in the Gateway and Edge.
  * [Microsoft.Azure.IIoT.OpcUa.Edge](src/Microsoft.Azure.IIoT.OpcUa.Edge/src) contains the services hosted by the [OPC Device Twin module](https://github.com/Azure/azure-iiot-opc-twin-module).
  * [Microsoft.Azure.IIoT.OpcUa.Twin](src/Microsoft.Azure.IIoT.OpcUa.Twin/src) contains services hosted by the [OPC Device Twin service](https://github.com/Azure/azure-iiot-services).
  * [Microsoft.Azure.IIoT.OpcUa.Registry](src/Microsoft.Azure.IIoT.OpcUa.Registry/src) contains the [OPC UA Device Registry](https://github.com/Azure/azure-iiot-services) micro service business logic.
  * [Microsoft.Azure.IIoT.OpcUa.Gateway](src/Microsoft.Azure.IIoT.OpcUa.Gateway/src) contains the business logic of the [OPC Device Twin Gateway](https://github.com/Azure/azure-iiot-services).
* [Microsoft.Azure.IIoT.OpcUa.Servers](src/Microsoft.Azure.IIoT.OpcUa.Servers/src) contains test servers.

The consuming services are part of our [Azure Industrial IoT (IIoT) components](#Other-Azure-Industrial-IoT-components) suite.

## Build

### Building with Visual Studio or VS Code

1. [Install .NET Core 2.1+][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Open and build the solution file in Visual Studio or VS Code

### Building from the command line

1. [Install .NET Core 2.1+][dotnet-install]
1. Open a command line window or terminal session in the repo root
1. Run `dotnet build -c Release`
1. Run `dotnet pack`

## Contributing

Refer to our [contribution guidelines](CONTRIBUTING.md).

## Feedback

Please enter issues, bugs, or suggestions as GitHub Issues [here](https://github.com/Azure/azure-iiot-components/issues).

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
