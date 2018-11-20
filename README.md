# Azure Industrial IoT API

This repository contains:

1. The handcrafted C# API for both [OPC UA Registry and Device Twin Services](https://github.com/Azure/azure-iiot-services).
1. A command line interface (CLI) that allows you to exercise the OPC UA API.
1. (Coming soon) A set of AutoREST generated libraries that enable access to the above services using a wider variety of languages.

The API and dependent services are part of our [Azure Industrial IoT (IIoT) components](#Other-Azure-Industrial-IoT-components) suite.

## Prerequisites

Clone this repository.

### Setup Dependencies

The command line interface (CLI) and APIs depend on the [OPC UA Services](https://github.com/Azure/azure-iiot-services). 

If you have [Docker][docker-url] installed, you can start the corresponding services by changing into the repository root and running `docker-compose up`.
Otherwise, follow the instructions at the respective link above to ensure that these dependencies are running before using the API or command line interface.

### Setup Environment variables (CLI)

Unless you are running the above services on your local machine (localhost), the following environment variables need to be set before running the command line interface (CLI):

* `OpcTwinServiceUrl` = {http|https}://{hostname}:9041
* `OpcRegistryServiceUrl` = {http|https}://{hostname}:9042

They define the respective location of the services' endpoints.

For more help on setting environment variables on your system:

* [This page][windows-envvars-howto-url] describes how to setup env vars in Windows.
* For Linux and MacOS, and depending on OS and terminal, there are ways to set variables globally, for more information [this](https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux), [this](https://help.ubuntu.com/community/EnvironmentVariables), or [this](https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x) page should help.

## Build and Run

Clone this repository.  If you have [Docker][docker-url] installed, you can start the corresponding services by changing into the repository root and running ```docker-compose up```.

### Building and running the CLI with Visual Studio or VS Code

1. [Install .NET Core 2.1+][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Set the [required environment variables](#Setup-Environment-variables-CLI)
1. Open the solution in Visual Studio or VS Code
1. In the launchsettings.json add as command line argument `console` to start the CLI in console mode rather than in single command mode.  (In Visual Studio this can also be accomplished in the Properties->Debug tab).
1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. Run the `Microsoft.Azure.IIoT.OpcUa.Api.Cli` project (e.g. press F5).
1. Type `help` to see the available options.

### Building and running the CLI on the command line

1. [Install .NET Core 2.1+][dotnet-install]
1. Open a terminal window or command line window at the repo root.
1. Set the [required environment variables](#Setup-Environment-variables-CLI)
1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. Run the following command:
    ```bash
    cd src
    cd Microsoft.Azure.IIoT.OpcUa.Api
    cd cli
    dotnet run console
    ```

### Building and running the CLI using Docker

1. Make sure [Docker][docker-url] is installed.
1. Set the [required environment variables](#Setup-Environment-variables-CLI)
1. Change into the repo root and build the docker image using `docker build -t azure-iiot-opc-twin-api .`
1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. To run the image run `docker run -e OpcTwinServiceUrl=$OpcTwinServiceUrl -e OpcRegistryServiceUrl=$OpcRegistryServiceUrl -it azure-iiot-opc-twin-api console`.  
1. Type `help` to see the available options.

## Other Azure Industrial IoT components

* [Azure Industrial IoT Micro Services](https://github.com/Azure/azure-iiot-services)
  * OPC UA Certificate Management service (Coming soon)
* [Azure Industrial IoT Service API](https://github.com/Azure/azure-iiot-services-api)
* Azure Industrial IoT Edge Modules
  * [OPC Publisher module](https://github.com/Azure/iot-edge-opc-publisher)
  * [OPC Proxy module](https://github.com/Azure/iot-edge-opc-proxy)
  * [OPC Device Twin module](https://github.com/Azure/azure-iiot-opc-twin-module)

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
