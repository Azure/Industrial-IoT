# Release announcement

## Azure Industrial IoT Platform Release 2.8.4

We are pleased to announce the release of version 2.8.4 of our Industrial IoT Platform components as latest patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

### IMPORTANT

- IoT Edge 1.1 LTS is out of support, please [update your IoT Edge gateways to IoT Edge 1.4 LTS](https://learn.microsoft.com/en-us/azure/iot-edge/how-to-update-iot-edge).
- Windows container images are no longer supported in IoT Edge 1.4 LTS and have been removed from this release. Please use [IoT Edge EFLOW](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/azure-iot-edge-for-linux-on-windows) for a Windows based IoT edge solution.
- Simulation deployed as part of the ./deploy.ps1 script now deploys EFLOW on Windows VM Host.
    > Your Azure subscription and chosen region must support Standard_D4_v4 VM which supports nested virtualization or deployment of Windows gatewaay will be skipped.

### Changes in this release

- Updated .net from .net 3.1 LTS which is now EOL to .net 6.0 LTS.
- Updated most of the nuget dependencies to their .net 6 counterpart or latest release.
- Updated IoT Edge dependency from IoT Edge 1.1 LTS which is now EOL to 1.4 LTS.
- [OPC Publisher] Fix for orchestrator infinite loop on publisher worker document update (#1870)
- [OPC Publisher] Rate limit OPC Publisher orchestrator requests.
- [Deployment] IAI: Upgraded Kubernetes version in AKS from 1.22.6 to 1.23.12. (#1885)
- [Deployment] Increased proxy-connect-timeout of NGINX from default 5 seconds to 30. (#1871)
- [OPC Publisher] (Preview) User can set the Data change trigger value of the Data change filter type either as a default for all or per subscription (#1830).
- Add a configuration option to set security option RejectUnknownRevocationStatus (#1777)
- Add mandatory field for edgeHub in the base deployment template to support cloning deployments (#1764)

## Azure Industrial IoT Platform Release 2.8.3

We are pleased to announce the release of version 2.8.3 of our Industrial IoT Platform components as a third patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

> IMPORTANT
> We suggest updating from the version 2.5 or later to ensure secure operations of your deployment. OPC Publisher 2.8.3 addresses backwards compatibilities issues with version 2.5.x.

### Security related fixes

- Updated OPC UA Stack NuGet to the latest (1.4.368.58) addressing various security issues
- Upgraded SSH.NET package to 2020.0.2 to address [CVE-2022-29245](https://nvd.nist.gov/vuln/detail/CVE-2022-29245).

### Fundamentals related fixes

- [OPC Publisher] option to route telemetry to a specific output route was added

### Bug fixes

- [OPC Publisher] Removed timestamps from metrics and updated the affected dashboard queries
- [OPC Publisher] Fixed issue with large configurations when publisher running in orchestrated mode related to CosmosDB continuation tokens handling
- [OPC Publisher] Publisher 2.8.2: Could not send worker heartbeat - eventually crashing and not restarting #1701
- [OPC Publisher] Fix for false alarm sequence number mismatch warning in case of keep-alive messages
- [Deployment] TLS certificate broken after upgrading of the AKS cluster #1389
- [Registry API] Number of MaxWorker not returned while reading publisher configuration

## Azure Industrial IoT Platform Release 2.8.2

We are pleased to announce the release of version 2.8.2 of our Industrial IoT Platform components as a second patch update of the 2.8 Long-Term Support (LTS) release. This release contains important backward compatibility fixes with version 2.5.x, performance optimizations as well as security updates and bugfixes.

> IMPORTANT
> We suggest to update from  version 2.5 or later to ensure secure operations of your deployment. OPC Publisher 2.8.2 addresses backwards compatibilities issues with version 2.5.x.

### Fundamentals related fixes

- [OPC Publisher] Implemented the backwards compatible [Direct Methods API](modules/publisher-directmethods.md) of 2.5.x publisher. The migration path is documented [here](modules/publisher-migrationpath.md)
- [OPC Publisher] Optimizations in opc subscriptions/monitored items management in case of configuration changes. Only incremental changes are applied to a subscription.
- [OPC Publisher] Added support for setting QueueSize on monitored items for publisher in standalone mode.
- [OPC Publisher] Hardened the retry mechanism for activating monitored items.

### Backwards Compatibility Notes

- [OPC Publisher] NodeId shows up now in telemetry in the exact format as specified in the configuration. Before 2.8.2, the NodeId was always reported as `Namespace#NodeId`
    > E.g. : When configuring in pn.json file a NodeId like `nsu=http://mynamespace.com/;i=1`
    >
    > - OPC Publisher 2.8.1 telemetry reports `http://mynamespace.com/#i=1`
    > - OPC Publisher 2.8.2 telemetry reports `nsu=http://mynamespace.com/;i=1`
    >
- [OPC Publisher] configuration of duplicate nodeIds in the same data set writer, respectively same subscription is no longer allowed.

## Azure Industrial IoT Platform Release 2.8.1

We are pleased to announce the release of version 2.8.1 of our Industrial IoT Platform components as the first patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates, bugfixes and performance optimizations.

> IMPORTANT
> Please note that OPC Publisher 2.8.1 is not backwards compatible with version 2.5.x.

## Azure Industrial IoT Platform Release 2.8

We are pleased to announce the release of version 2.8 of our Industrial IoT Platform as well as the declaration of Long-Term Support (LTS) for this version.
While we continue to develop and release updates to our ongoing projects on GitHub, we now also offer a branch that will only get critical bug fixes and security updates starting in July 2021. Customers can rely upon a longer-term support lifecycle for these LTS builds, providing stability and assurance for the planning on longer time horizons our customers require. The LTS branch offers customers a guarantee that they will benefit from any necessary security or critical bug fixes with minimal impact to their deployments and module interactions. At the same time, customers can access the latest updates in the main branch to keep pace with the latest developments and fastest cycle time for product updates.

> IMPORTANT
> We suggest to update from the version 2.6 or later to ensure secure operations of your deployment. 2.8.0 is not backwards compatible with version 2.5.x

Version 2.8.0 includes an updated version of the IoT Edge Runtime, a new Linux base image for all Linux deployments, and several bug fixes. The detailed changes can be found [here](https://github.com/Azure/Industrial-IoT/releases/tag/2.8.0).
