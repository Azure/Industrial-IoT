# Azure Industrial IoT API

This repository contains:

1. The handcrafted C# API for both [OPC UA Registry and Device Twin Services](https://github.com/Azure/azure-iiot-services).
1. A sample command line interface (CLI) that allows you to exercise this API.
1. (Coming soon) A set of AutoREST generated libraries that enable access to the above services using a wider variety of languages.

The API and dependent services are part of our [Azure Industrial IoT (IIoT) components](#Other-Azure-Industrial-IoT-components) suite.

## Using the API

Prebuilt and signed packages of the API assemblies can be installed from nuget.org.  Use them to build your own applications against the corresponding [services](https://github.com/Azure/azure-iiot-services).

## Build and Run

To build and run the sample command line interface for the [services](https://github.com/Azure/azure-iiot-services), clone the repository.  Then...

### Install any tools and dependencies

* [Install .NET Core 2.1+][dotnet-install] if you want to build the services locally.
* Install [Docker][docker-url] if you want to build and run the services using docker-compose.
* Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed. 

### Deploy Azure Services

The command line interface (CLI) and APIs depend on the [OPC UA Services](https://github.com/Azure/azure-iiot-services).

Follow the instructions [here](https://github.com/Azure/azure-iiot-services) to deploy all required services for local development.  Copy the resulting `.env` file into this repository's root (or into the parent folder).

Unless you are running the above services on your local machine (localhost), the following environment variables can be added to your `.env` file to configure the service endpoints:

* `PCS_TWIN_SERVICE_URL` = {http|https}://{hostname}:9041
* `PCS_TWIN_REGISTRY_URL` = {http|https}://{hostname}:9042

### Building and running the CLI with Visual Studio or VS Code

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Change into the repo root and ensure the .env file exists there or in the parent folder.
1. Open the `azure-iiot-services-api.sln` solution file in Visual Studio or VS Code
1. In the launchsettings.json add as command line argument `console` to start the CLI in console mode rather than in single command mode.  (In Visual Studio this can also be accomplished in the Properties->Debug tab).
1. Run the `Microsoft.Azure.IIoT.OpcUa.Api.Cli` project (e.g. press F5).
1. Type `help` to see the available options.

### Building and running the CLI on the command line

1. Make sure the [Prerequisites](#Install-any-tools-and-depdendencies) are set up.
1. Open a terminal window or command line window at the repo root.
1. Ensure the .env file exists in the root or its parent folder.
1. Run the following command:
    ```bash
    cd src
    cd Microsoft.Azure.IIoT.OpcUa.Api
    cd cli
    dotnet run console
    ```

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
