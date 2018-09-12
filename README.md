# OPC Twin Service (Preview)

![Build Status](https://msazure.visualstudio.com/_apis/public/build/definitions/b32aa71e-8ed2-41b2-9d77-5bc261222004/33977/badge)

The OPC UA twin micro service facilitates communication with factory floor edge OPC UA server devices via the [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module) and exposes OPC UA services (Browse, Read, Write, and Execute) via a REST Web Api.  It is part of our [Industrial IoT (IIoT) solution accelerator components](#Other-Industrial-IoT-Solution-Accelerator-components) suite.

## Prerequisites

### Deploy Azure Services

This service has a dependency on the following Azure resources:

* [Azure IoT Hub][iothub-docs-url]

Follow the instructions for [Deploy the Azure services][deploy-local] to deploy the required resources.

### Setup Dependencies

* This service depends on the [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module).  Start the module following the instructions [here](https://github.com/Azure/azure-iiot-opc-twin-module)).

> For real world usage, run one or more OPC UA servers in the same network that the above module deployment is part of.

### Setup Environment variables

In order to run the service, some environment variables need to be created at least once. More information on environment variables [below](#Configuration-And-Environment-Variables).

* `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string from [Deploy Azure Services](#deploy-azure-services)}
  * More information on where to find your IoT Hub connection string can be found [here][iothub-connstring-blog].

## Build and Run

### Building and running the service with Visual Studio or VS Code

1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. [Install .NET Core 2.1+][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual Studio Code (Windows/MacOS/Linux).
   * If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017] [dotnetcore-tools-url] installed (Windows only).
   * If you already have VS Code installed, then ensure you have the [C# for Visual Studio Code (powered by OmniSharp)][omnisharp-url] extension installed.
1. Set the [required environment variables](#Setup-Environment-variables) as explained [here](#Configuration-And-Environment-Variables)
1. Open the solution in Visual Studio or VS Code
1. Start the `Microsoft.Azure.IIoT.OpcUa.Services.Twin` project (e.g. press F5).
1. Open a browser to `http://localhost:9041/` and test the service using the services' Swagger UI or the [OPC Twin CLI](https://github.com/Azure/azure-iiot-opc-twin-api).

### Building and running the service on the command line

1. Make sure the [Prerequisites](#Prerequisites) are set up.
1. [Install .NET Core 2.1+][dotnet-install]
1. Open a terminal window or command line window at the repo root. 
1. Set the [required environment variables](#Setup-Environment-variables) as explained [here](#Configuration-And-Environment-Variables)
1. Run the following command:
    ```bash
    cd src
    dotnet run
    ```
1. Open a browser to `http://localhost:9041/` and test the service using the services' Swagger UI or the [OPC Twin CLI](https://github.com/Azure/azure-iiot-opc-twin-api).

### Building and running the service using Docker

1. Make sure [Docker][docker-url] is installed.
1. Make sure the [Prerequisites](#prerequisites) are set up.
1. Set the [required environment variables](#Setup-Environment-variables) as explained [here](#Configuration-And-Environment-Variables)
1. Change into the repo root and build the docker image using `docker build -t azure-iiot-opc-twin-service .`
1. To run the image run `docker run -p 9041:9041 -e _HUB_CS=$PCS_IOTHUB_CONNSTRING -it azure-iiot-opc-twin-service` (or `docker run -p 9041:9041 -e _HUB_CS=%PCS_IOTHUB_CONNSTRING% -it azure-iiot-opc-twin-service` on Windows).
1. Open a browser to `http://localhost:9041/` and test the service using the services' Swagger UI or the [OPC Twin CLI](https://github.com/Azure/azure-iiot-opc-twin-api).

### Configuration and Environment variables

The service can be configured in its [appsettings.json](src/appsettings.json) file.  Alternatively, all configuration can be overridden on the command line, or through environment variables.  If you have deployed the dependent services using the [pcs local][deploy-local] command, make sure the environment variables shown at the end of deployment are all set in your environment.

* [This page][windows-envvars-howto-url] describes how to setup env vars in Windows.
* For Linux and MacOS, we suggest to create a shell script to set up the environment variables each time before starting the service host (e.g. VS Code or docker). Depending on OS and terminal, there are ways to persist values globally, for more information [this](https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux), [this](https://help.ubuntu.com/community/EnvironmentVariables), or [this](https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x) page should help.

> Make sure to restart your editor or IDE after setting your environment variables to ensure they are picked up.

## Other Industrial IoT Solution Accelerator components

* OPC GDS Vault service (Coming soon)
* [OPC Twin Registry service](https://github.com/Azure/azure-iiot-opc-twin-registry)
* [OPC Twin Onboarding service](https://github.com/Azure/azure-iiot-opc-twin-onboarding)
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
