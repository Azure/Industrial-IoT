# Common Azure Industrial IoT (IIoT) libraries (Preview)

These helper libraries are used across our [Industrial IoT (IIoT) solution accelerator components](#Other-Industrial-IoT-Solution-Accelerator-components) suite and are available as nuget packages on nuget.org:

* [Microsoft.Azure.IIoT.Abstractions](src/Microsoft.Azure.IIoT.Abstractions/src) contains shared code and abstractions for the following...
  * [Microsoft.Azure.IIoT.Auth](src/Microsoft.Azure.IIoT.Auth/src) has several authN and authZ abstraction implementations that can be used outside of a web service context.
  * [Microsoft.Azure.IIoT.Hub](src/Microsoft.Azure.IIoT.Hub/src) contains the IoT Hub service client models and service interfaces as well as the core HttpClient based clients.
    * [Microsoft.Azure.IIoT.Hub.Client](src/Microsoft.Azure.IIoT.Hub.Client/src) contains a IoT Sdk based client for production use.
    * [Microsoft.Azure.IIoT.Hub.Processor](src/Microsoft.Azure.IIoT.Hub.Processor/src) contain the core event / telemetry processor framework for IoT Hub.
  * [Microsoft.Azure.IIoT.Infrastructure](src/Microsoft.Azure.IIoT.Infrastructure/src) contains API wrappers around the Azure management SDK.
  * Microsoft.Azure.IIoT.Module
    * [Microsoft.Azure.IIoT.Module.Deployment](src/Microsoft.Azure.IIoT.Module.Deployment/src) supports programmatic module deployment to IoT Edge.
    * [Microsoft.Azure.IIoT.Module.Framework](src/Microsoft.Azure.IIoT.Module.Framework/src) contains a MVC framework to build modules for IoT Edge similar to Asp.Net Core.
  * [Microsoft.Azure.IIoT.Net](src/Microsoft.Azure.IIoT.Net/src) contains common networking code and helpers.
    * [Microsoft.Azure.IIoT.Net.Scan](src/Microsoft.Azure.IIoT.Net.Scan/src) contains network and port scanner implementations.
    * [Microsoft.Azure.IIoT.Net.Ssh](src/Microsoft.Azure.IIoT.Net.Ssh/src) contains a secure shell interface implementation using Renci.Ssh.
  * [Microsoft.Azure.IIoT.Services](src/Microsoft.Azure.IIoT.Services/src) contains common helpers and utilities for Asp.net Core based web services.

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

## Industrial IoT Solution Accelerator components

* OPC GDS service (Coming soon)
* [OPC Twin service](https://github.com/Azure/azure-iiot-opc-twin-service)
* [OPC Twin Registry service](https://github.com/Azure/azure-iiot-opc-registry-service)
* [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-onboarding)
* OPC Twin management agent service (Coming soon)
* OPC Twin common business logic (Coming soon)
* [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module)
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
