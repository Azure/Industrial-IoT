# Release announcement

## Azure Industrial IoT OPC Publisher Community Preview Release 2.9.0

We are pleased to announce the **preview** release of version 2.9.0 of OPC Publisher. This release contains several requested features and fixes issues discovered.

### IMPORTANT - PLEASE READ

Preview releases are only supported through GitHub issues.

### Changes in this release

- [OPC Publisher] [Alarms and Events](./modules/publisher-event-configuration.md) support to OPC Publisher. You can now subscribe to events in addition to value changes and in the familar ways using the published nodes json configuration and direct methods.
- [OPC Publisher] Full Deadband filtering. We introduced data change triggers in 2.8.4 and are now supporting the full data change filter configuration to configure percent and absolute deadband as defined in OPC UA.
- [OPC Publisher] Support setting discard new configuration on command line.
- [OPC Publisher] Full support for UADP network message encoding, as well as reversible Json profiles (JsonReversible)
- [OPC Publisher] Support for smaller network messages by removing network message and dataset message headers (adding new MessageType.RawDataset and MessageType.DataSetMessages).
- [OPC Publisher] Support for gzip encoded Json (MessageEncoding.JsonGzip and MessageEncoding.JsonReversibleGzip)
- [OPC Publihser] Strict mode to adhere to OPC UA Part 14 and Part 6, including message formats and data type serialization.
- [OPC Publisher] Adding back support for --sf and SkipFirst property to skip the first data change notification to be sent when subscription is created.
- [All IoT Edge modules] Configuration to optionally enable MQTT topic publishing and command control via an MQTT broker instead of IoT Edge EdgeHub.
- [All IoT Edge modules] Update OPC UA stack to latest .371 version.

## Azure Industrial IoT Platform Release 2.8.4

We are pleased to announce the release of version 2.8.4 of our Industrial IoT Platform components as latest patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

### IMPORTANT - PLEASE READ

- IoT Edge 1.1 LTS will be going out of support on 12/13/2022, please [update your IoT Edge gateways to IoT Edge 1.4 LTS](https://learn.microsoft.com/en-us/azure/iot-edge/how-to-update-iot-edge).

  > To continue deploying the 1.1 LTS modules to your environment follow [these instructions](./deploy/howto-install-iot-edge.md).

- Windows container images are no longer supported in IoT Edge 1.4 and consequentially have been removed from this release. Please use [IoT Edge for Linux on Windows (EFLOW) 1.4](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/azure-iot-edge-for-linux-on-windows) as your IoT Edge environment on Windows.  

  > IoT Edge 1.4 LTS EFLOW is supported as a Preview Feature in this release.

  - You must update your Windows based IoT Edge environment to EFLOW **ahead of deploying the platform**.  
  - Simulation deployed as part of the ./deploy.ps1 script now deploys EFLOW on a Windows VM Host (Preview Feature).  This requires nested virtualization.  The Azure subscription and chosen region must support Standard_D4_v4 VM which supports nested virtualization or deployment of simulated Windows gateway will be skipped.
  - Network scanning on IoT Edge 1.4 LTS EFLOW using OPC Discovery is not supported yet. This applies to the deployed [simulation environment](./deploy/howto-deploy-all-in-one.md) and [engineering tool](./services/engineeringtool.md) experience. You can register servers using a discovery url using the [registry service's registration REST API](./services/registry.md).

### Changes in this release

- Updated .net to .net 6.0 LTS from .net core 3.1 LTS which will be going out of support on 12/13/2022.
- Updated nuget dependencies to their .net 6 counterpart or to their latest compatible release.
- [All IoT Edge modules] Updated IoT Edge dependency to IoT Edge 1.4 LTS from 1.1 LTS which will be going out of support on 12/13/2022.
- [All IoT Edge modules] (Preview) Support for [IoT Edge EFLOW 1.4 LTS](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/azure-iot-edge-for-linux-on-windows)
- [OPC Publisher] Fix for orchestrator infinite loop on publisher worker document update (#1870)
- [OPC Publisher] Rate limit OPC Publisher orchestrator requests.
- [OPC Publisher] More explicit error logs in orchestrated mode, also showing transient exceptions in logs for better troubleshooting.
- [OPC Publisher] Add --bi / --batchtriggerinterval command line option to define interval in milliseconds. (#1893)
- [Deployment] IAI: Upgraded Kubernetes version in AKS from 1.22.6 to 1.23.12. (#1885)
- [Deployment] Increased proxy-connect-timeout of NGINX from default 5 seconds to 30. (#1871)
- [OPC Publisher] (Preview) User can set the Data change trigger value of the Data change filter type either as a default for all or per subscription (#1830).
- [OPC Publisher] (Preview) Allow comment in OpcPublisher Configuration when Validation is used (#1892)
- [All IoT Edge modules] Add a configuration option to set security option RejectUnknownRevocationStatus (#1777)
- [All IoT Edge modules] Add mandatory field for edgeHub in the base deployment template to support cloning deployments (#1764)
- [OPC Discovery] Ensure duplicate discovery urls override previous url instead of causing exception (#1903, #1902)
- [OPC Discovery] Enginering tool ad hoc discovery fails with "capacity must be non negative" error found during bug bash. (#1907)
- [OPC Twin] Fix sessions in Twin are not closed when endpoint is deactivated. (#1910)

For features that are marked as Preview, please report any issues through GitHub issues.

### Known issues

- Under EFLOW, by default the Application instance certificate is generated by each module and not automatically shared. To work around this, configure EFLOW and modules deployed manually to use a [shared host volume](https://learn.microsoft.com/en-us/azure/iot-edge/how-to-share-windows-folder-to-vm?view=iotedge-2018-06) to map the pki folders.
- Discovering using IP Address and port ranges is not supported on EFLOW.

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
