# Microsoft OPC Publisher and Azure Industrial IoT Platform

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status%2FOneBranch%2FIndustrial-IoT%2FIndustrial-IoT-Official?repoName=Industrial-IoT&branchName=main)](https://msazure.visualstudio.com/One/_build/latest?definitionId=364945&repoName=Industrial-IoT&branchName=main)

## Discover, register and manage your OPC UA enabled Assets with Azure

Microsoft [OPC Publisher](docs/opc-publisher/readme.md) and the optional Azure Industrial IoT Platform companion web service allow you to discover and operate OPC UA enabled industrial assets.

With OPC Publisher you can  harness the power of OPC UA and Azure IoT. OPC Publisher is a fully compliant OPC UA PubSub telemetry publisher (supporting JSON, JSON+Gzip, and UADP binary encoding) and provides a large subset of the OPC UA services through its control plane. OPC Publisher is an Azure IoT Edge module that runs on on-premises. OPC Publisher API can be accessed via HTTP(s) (Preview), an MQTT Broker (Preview) or through Azure IoT Hub device methods.

We worked with our large partner network to support all types of industrial protocols through the use of adapters if your industrial equipment doesn't support OPC UA.  These modules are fully integrated with our platform. Check out [Azure IoT Edge Marketplace](https://azuremarketplace.microsoft.com/marketplace/apps/category/internet-of-things?page=1&subcategories=iot-edge-modules) for more information.

The [companion cloud service](docs/web-api/readme.md) provided in this repository (Preview) with REST interface runs inside Azure App Service and provides a cloud side REST API to command the OPC Publisher at the edge. An easy-to-use deployment script guides you step-by-step through deploying the web service and its dependencies, as well as an optional simulation environment in Azure.

Microsoft provides pre-built Docker containers in the Microsoft Container Registry (MCR) for OPC Publisher and the other tools included in this repository.

## Get started

* Learn about [OPC Publisher](docs/opc-publisher/readme.md) and how to use it.
* See all [Features](docs/opc-publisher/features.md) of OPC Publisher and their support level.
* More [Industrial IoT documentation](docs/readme.md).
* Find [release announcements](docs/release-announcement.md) and all [releases of the platform](https://github.com/Azure/Industrial-IoT/releases).

## Get support

Please report any security related issues by following our [security process](./SECURITY.md).

If you are an Azure customer, please create an Azure Support Request. More information can be found [here](https://azure.microsoft.com/support/create-ticket/). (Azure Support SLA applies).

Otherwise, please report bugs, feature requests, or suggestions as [GitHub issues](https://github.com/Azure/Industrial-IoT/issues) (No SLA available).

### Supported releases and support policy

Our releases are tagged following semantic versioning (“semver”) conventions. Minor and patch releases do not break backwards compatibility. Releases not shown in the table (e.g., 2.4.x, 2.5.x, 2.6.x, or 2.7.x) are out of support already.

| Release (tag)      | Last release (tag) | End of support | Successor (tag)       | Update instructions |
|--------------------|--------------------|----------------|-----------------------|---------------------|
| Industrial IoT 2.8 | [2.8.6](https://github.com/Azure/Industrial-IoT/tree/release/2.8.6) | 7/15/2023  | OPC Publisher 2.9 | [Migration Path](docs/opc-publisher/migrationpath.md) |
| OPC Publisher 2.8  | [2.8.7](https://github.com/Azure/Industrial-IoT/tree/release/2.8.7) | 12/15/2023 | OPC Publisher 2.9 | N/A |
| OPC Publisher 2.9  | [2.9.9](https://github.com/Azure/Industrial-IoT/tree/release/2.9.9) | 11/10/2026 | TBA               | [Migration Path](docs/opc-publisher/migrationpath.md) |

We only support the latest patch version of a release which per semantic versioning convention is identified by the 3rd part of the version string. Preview releases, preview and experimental features are only supported through GitHub issues.

If you are using a container image with a major.minor version tag that is supported per above table, but a patch version lower than the latest patch version, you need to update your images to the latest version to ensure secure operation and take advantage of the latest fixes. If you unexpectedly encounter bugs and require help, please ensure you are running the latest patch release as we might already have addressed the issue you are seeing. If you are not, please update first and try to reproduce the issue on the latest patch version.

Security-critical updates are made to the last patch version of the major.minor release containing the vulnerability. Bug fixes that are not security related are made only to the main branch and to the last supported release. The version the fix will be in can be found in the version.json file of the respective branch.

Our [official Microsoft support](https://azure.microsoft.com/support/create-ticket/) and any related SLA only covers officially released docker containers obtained from MCR (Microsoft Container Registry) and deployed to Azure in Azure App Services (Web API container) or IoT Edge (OPC Publisher module container) using the documentation and deployment scripts provided as part of the latest release. Experimental and Preview features are excluded. Also, all Azure services deployed, the installed IoT Edge runtime, as well as Operating System and other middleware and combinations thereof must be officially supported as per their published support policy and SLA.

In all other cases, support is provided on a best effort basis through [GitHub issues](https://github.com/Azure/Industrial-IoT/issues). We aim to release patch releases on a regular cadence (approximately every 1-2 months), so if you are blocked, and you can suggest or contribute fixes, the chances of getting it into the next patch release are high.

## Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](./contributing.md). A friendly bot will remind you about it when you submit a pull-request.

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.
