# Release announcement <!-- omit in toc -->

## Table Of Contents <!-- omit in toc -->

- [Azure Industrial IoT OPC Publisher 2.9.9](#azure-industrial-iot-opc-publisher-299)
  - [Changes in 2.9.9](#changes-in-299)
- [Azure Industrial IoT OPC Publisher 2.9.8](#azure-industrial-iot-opc-publisher-298)
  - [Changes in 2.9.8](#changes-in-298)
- [Azure Industrial IoT OPC Publisher 2.9.7](#azure-industrial-iot-opc-publisher-297)
  - [Changes in 2.9.7](#changes-in-297)
- [Azure Industrial IoT OPC Publisher 2.9.6](#azure-industrial-iot-opc-publisher-296)
  - [Changes in 2.9.6](#changes-in-296)
- [Azure Industrial IoT OPC Publisher 2.9.5](#azure-industrial-iot-opc-publisher-295)
  - [Changes in 2.9.5](#changes-in-295)
- [Azure Industrial IoT OPC Publisher 2.9.4](#azure-industrial-iot-opc-publisher-294)
  - [Breaking changes in 2.9.4](#breaking-changes-in-294)
  - [Changes in 2.9.4](#changes-in-294)
- [Azure Industrial IoT OPC Publisher 2.9.3](#azure-industrial-iot-opc-publisher-293)
  - [Breaking changes in 2.9.3](#breaking-changes-in-293)
  - [New features in 2.9.3](#new-features-in-293)
  - [Bug fixes in 2.9.3](#bug-fixes-in-293)
- [Azure Industrial IoT OPC Publisher 2.9.2](#azure-industrial-iot-opc-publisher-292)
  - [Changes in 2.9.2](#changes-in-292)
- [Azure Industrial IoT OPC Publisher 2.9.1](#azure-industrial-iot-opc-publisher-291)
  - [Changes in 2.9.1](#changes-in-291)
- [Azure Industrial IoT OPC Publisher 2.9.0](#azure-industrial-iot-opc-publisher-290)
  - [Changes in 2.9.0](#changes-in-290)
- [Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 4](#azure-industrial-iot-opc-publisher-290-community-preview-4)
  - [Changes in 2.9.0 Preview 4](#changes-in-290-preview-4)
- [Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 3](#azure-industrial-iot-opc-publisher-290-community-preview-3)
  - [Changes in 2.9.0 Preview 3](#changes-in-290-preview-3)
- [Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 2](#azure-industrial-iot-opc-publisher-290-community-preview-2)
  - [Changes in 2.9.0 Preview 2](#changes-in-290-preview-2)
- [Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 1](#azure-industrial-iot-opc-publisher-290-community-preview-1)
  - [Changes in 2.9.0 Preview 1](#changes-in-290-preview-1)
- [Azure Industrial IoT Platform Release 2.8.6](#azure-industrial-iot-platform-release-286)
  - [Changes in 2.8.6](#changes-in-286)
- [Azure Industrial IoT Platform Release 2.8.5](#azure-industrial-iot-platform-release-285)
  - [Changes in 2.8.5](#changes-in-285)
- [Azure Industrial IoT Platform Release 2.8.4](#azure-industrial-iot-platform-release-284)
  - [IMPORTANT - PLEASE READ](#important---please-read)
  - [Changes in 2.8.4](#changes-in-284)
  - [Known issues](#known-issues)
- [Azure Industrial IoT Platform Release 2.8.3](#azure-industrial-iot-platform-release-283)
  - [Security related fixes in 2.8.3](#security-related-fixes-in-283)
  - [Fundamentals related fixes in 2.8.3](#fundamentals-related-fixes-in-283)
  - [Bug fixes in 2.8.3](#bug-fixes-in-283)
- [Azure Industrial IoT Platform Release 2.8.2](#azure-industrial-iot-platform-release-282)
  - [Fundamentals related fixes in 2.8.2](#fundamentals-related-fixes-in-282)
  - [Backwards Compatibility Notes for release 2.8.2](#backwards-compatibility-notes-for-release-282)
- [Azure Industrial IoT Platform Release 2.8.1](#azure-industrial-iot-platform-release-281)
- [Azure Industrial IoT Platform Release 2.8](#azure-industrial-iot-platform-release-28)

## Azure Industrial IoT OPC Publisher 2.9.9

We are pleased to announce the release of version 2.9.9 of OPC Publisher and the companion web api service. This release comes with several bug and security fixes and is the latest supported release. We recommend strongly to update from 2.9.8 due to several security fixes and overall instability in 2.9.8 and before in reconnect situations.

### Changes in 2.9.9

- Update OPC UA .net stack to the latest 1.05 version and move forward other dependencies
- Allow configuring a watchdog for monitored items and set behavior when watchdog expires (#2164)
- Limit number of publish requests to 10, allow override using command line argument (#2215)
- Fix issue where the 'v2/configuration/diagnostics' contains negative monitoredOpcNodesSucceededCount (#2202)
- Return better error messages in responses (#2195)
- Fix issue where publisher stops respecting reconnect period after initial successful connection (#2189)
- Added sample to show using NodeRead to read attributes (#2226)
- New command line option to read configuration from first certificate found in own folder addresses several backward compatibility breaks when migrating between OPCPublisher versions (#2166)
- Added call timeouts that can be overridden in request header and automatically cancel after timeout (#2203, #2219, #2213)
- Fix an issue where linger does not work when reference continuation token between calls is the same (#2227)
- Add a command line option to disable runtime metrics (#2200, #2197)
- Added ability to write encoded messages to stdout/stderr for debugging (#2259)
- Improve documentation.

## Azure Industrial IoT OPC Publisher 2.9.8

We are pleased to announce the release of version 2.9.8 of OPC Publisher and the companion web api service. This release comes with several bug and security fixes and is the latest supported release.

### Changes in 2.9.8

- Update OPC UA .net stack to the latest 1.05 version and move forward other dependencies
- Telemetry messages are sent with non default (0) ttl now to support DAPR >= 1.13 (#2243)
- Elevation property in request header does work now when service calls are sent using edge API (#2218)
- Add more Info to Logs in case of publishing errors, e.g. "Too many publish request error" (#2229)
- New Security mode "NotNone" reflecting the legacy "UseSecurity":true setting in nodes. "Best" (any) is persisting after Opc Publisher is restarted (#2228)
- Latest OPC UA .net stack fixes "CRL with zero revoked certificates fails to be decoded (#2214)
- Enable the definition of the GRPC endpoint host name for DAPR (#2235)
- Diagnostic info API returns incorrect information (#2230)
- Make TransferSubscriptionsOnReconnect configurable (via publishednodes.json) (#2205)
- Ability to disable Complex type system loading (#2185)
- Several improvements for cyclic read (#2169)
  - Only submit publish requests for active subscriptions rather than any subscription. (#2184)
  - Better diagnostic messages of number of cyclic read, heartbeat, events per minute (#2175)
  - Track # of missed intervals / samples due to latency of read (#2174)
- BadNodeIdUnknown not re-evaluated despite short retry delays? (#2188)
- Additional information in diagnostic messages including resource consumption (total)
- Persistency of published nodes file now uses .net storage provider which supports enabling file change polling via command line option (needed on some linux setups)

## Azure Industrial IoT OPC Publisher 2.9.7

### Changes in 2.9.7

- Added additional diagnostic logging option to capture the encoded notifications as well

## Azure Industrial IoT OPC Publisher 2.9.6

We are pleased to announce the release of version 2.9.6 of OPC Publisher and the companion web api service. This release comes with several bug and security fixes and is the latest supported release.

### Changes in 2.9.6

- Update OPC UA .net stack to the latest 1.05 version and move forward other dependencies fixing several reported CVE vulnerabilities. 
- Additional option to log encoded notifications to trace data all the way to send path.
- Hardened code paths that made it possible that no more publishing requests are enqueued after a while. Continuously ensure minimum amount of publish requests are in progress when receiving good session keep alives.

## Azure Industrial IoT OPC Publisher 2.9.5

We are pleased to announce the release of version 2.9.5 of OPC Publisher and the companion web api service. This release comes with several bug and security fixes and is the latest supported release.

### Changes in 2.9.5

- Update OPC UA .net stack to the latest 1.05 version and move forward other dependencies
- Fixed a bug where OPC Publisher could not connect when the port provided in the endpoint during discovery is different than the port in of the discovery URL.

## Azure Industrial IoT OPC Publisher 2.9.4

We are pleased to announce the release of version 2.9.4 of OPC Publisher and the companion web api service. This release comes with several bug and security fixes and is the latest supported release.

### Breaking changes in 2.9.4

> IMPORTANT. Please read when updating from previous versions of OPC Publisher

- Arm64 and AMD64 container images are published now with Mariner (Azure) Linux (distroless) as base images instead of Alpine.
- Arm32 (v7) images of OPC Publisher continue to use Alpine as base image. Support transitions to the same model as for "preview" features.  Security updates are released as a result of updates to the AMD64 and ARM64 version of OPC Publisher.
- Swagger UI has been removed without replacement.

### Changes in 2.9.4

- Update OPC UA .net stack to the 1.05 version including latest node set and fixing numerous issues. (#2162)
- ApiKey and other secrets can now be provided ahead of time through docker secrets (or command line) in addition to being only available in the Module Twin. (#2181)
- Send the error of CreateMonitoredItem as part of the keyframe field and in heartbeats if WatchdogLKV heartbeat behavior is used (#2150).
- Credential based authentication uses concrete types for credentials now which are documented in openapi.json (#2152)
- OPC Publisher can now obtain TLS certificates from IoT Edge workload API to secure the HTTPS API (#2101)
- Fix release build issue which broke support for ARM64 images running on RPi4 (#2145).
- Update console diagnostics output to provide better naming, additional diagnostics and reflect other transports than IoT Edge Hub (#2141)
- Add keep alive notification counts to Diagnostics output and messages
- Better diagnostics messages of cyclic reads, heartbeats and events including per minute and second reporting (#2175, #2174)
- Experimental feature to allow publishing model changes in the underlying server address space (change feed) (#2158)
- Add a full version that includes runtime, framework and full version string to runtime state message, twin, diagnostic object, and in console output.
- When only using cyclic reads, the underlying dummy subscription should stay disabled (#2139)
- Recreate session if it expires on server (#2138)
- Log subscription keep alive error only when session is connected (#2137)
- Added the ability to switch publisher to emit logs in syslog or systemd format using --lfm command line option.
- Fix issue where certain publish errors cause reconnect state machine to fail (#2104, #2136)
- Fix issues with cyclic reads not working as expected, subscribing to nodes > 300 not working (#2160, #2165)
- Metric names are not like they were in 2.8, some metrics are missing (#2149)
- SiteId placeholder is not working in TelemetryTopicTemplate (#2161)
- Messaging mode can now be overridden in Strict mode to use a non-compliant mode. (#2167)

## Azure Industrial IoT OPC Publisher 2.9.3

We are pleased to announce the release of version 2.9.3 of OPC Publisher and the companion web api. This release moves OPC Publisher to .net 8 which is the latest LTS version of .net and comes with several new features, bug and security fixes. 2.9.3 is the latest supported release.

### Breaking changes in 2.9.3

> IMPORTANT. Please read when updating from previous versions of OPC Publisher

- Arm64 and AMD64 container images are published now with (Azure) Mariner Linux (distroless) as base images instead of Alpine.
- Arm32 (v7) images of OPC Publisher continue to use Alpine as base image. Support transitions to the same model as for "preview" features.  Security updates are released as a result of updates to the AMD64 and ARM64 version of OPC Publisher.
- Metadata collection has shown to be very taxing on OPC UA servers. When 2.9 was dropped in to replace 2.8 in production, memory consumption was too large and connections would drop. OPC Publisher now defaults to `--dm=true` in 2.9.3 to disable metadata messages to be compatible with 2.8 when `--strict` / `-c` is not specified. If you need meta data messages but do not use strict mode (not recommended) you must explicitly enable it using `--dm=false`.

### New features in 2.9.3

- For security the OPC Publisher Web API container now runs root-less. (#2114)
- New configuration option to specify quality of service. This allows setting QOS0 as alternative to QOS1 (#2085)
- Diagnostic info can now also be periodically published to a topic or IoT Edge output name using new `--dtt` diagnostics topic template. (#2068)
- New Module to module method to get REST endpoint info and API key so that other modules can access the REST API (#2096)
- Fixed Publisher HTTPS API returning SSL_ERROR_SYSCALL error. Now a self signed certificate is the fallback if workload api cannot produce a certificate with private key (#2101)
- Restart announcement now includes additional information, including version, timestamp of (re-)start, module and device ids.
- X509 User Authentication support using secrets reference feature request (#2005)
- New API to manage the PKI infrastructure of the OPC Publisher (certificate stores). You can now list, add and remove certificates from the OPC UA certificate stores remotely. (#1996)
- You can now configure OPC Publisher to re-use a session across writer groups with the same endpoint url and security settings. (#2065)
- Subscription Watchdog monitors keep alive notifications and generates metric and logs when subscription keep alive is missing (#2060)
- Added samples to show how to call OPC Publisher API over MQTT, HTTP and IoT Hub.

### Bug fixes in 2.9.3

- 2.8 Start instrument was missing on 2.9 prometheus endpoint (#2110)
- Fix Publisher cannot get ssl cert from workload api, HTTPS API returning SSL_ERROR_SYSCALL error (#2101)
- Harden when OPC UA server sometimes reports monitored items samples changes unordered in subscription notification and thus samples messages are also unordered (#2108)
- Need to have timestamp information and other information in runtime state reporting message, need to have a special routing path for runtime state messages feature request. Restart announcement now includes additional information, including version, timestamp of (re-)start, module and device ids. (#2111)
- Optimize metadata collection, do not collect metadata from servers for built in types (#2105)
- Fix that it was not possible to configure event subscriptions for multiple events on the same node id (#2098)
- Fix complex type encoding where Json message encoding has value in Binary encoding for complex (multilevel structure) (#2090)
- Dapr now works without requiring state component. Dapr now runs over http instead of https by default. New option to select the url scheme (#2102, #2119, #2117, #2109
- It is now possible to disable retrying subscription re-creation by configuring a value of 0. (#2100)
- Fix that extension field values show up wrong in samples mode. (#2092)
- Fix Event subscription using the REST Api fails with: "The request field is required." (#2078)
- The configuration of the OpcPublisher 2.9.2 fails using the REST Api bug (#2066)
- For each configured in pn.json Dataset publisher must try to reuse an existing session for this EndpointUrl with the identical security settings (if exists). feature request
- Address issues deploying the web api, e.g., getting error when trying to use option 2 to deploy Azure IIoT Deployment and ./aad-register.ps1 errors with "A parameter cannot be found that matches the parameter name 'ReplyUrl'." (#2063, #2064)
- Update documentation, including breaking changes, Add Azure Storage, Azure Key Vault services, and Application Insights to arch diagram, how to setup the OPCPublisher edge module with X.509 certificates documentation, and how to emit ExtensionFields in Pub sub mode using key frame counter. (#1917, #2091, #2083)
- Fix incorrect API definitions in OpenAPI JSON for OPC publisher
- FileSystem target now appends data instead of updating the file content. FileSystem target now supports arbitrary chars in topics.

## Azure Industrial IoT OPC Publisher 2.9.2

We are pleased to announce the release of version 2.9.2 of OPC Publisher. This release comes with several bug and security fixes and is the latest supported release.

### Changes in 2.9.2

- Update to version 1.4.372 of the OPC UA .net stack and updated to latest secure version of nuget dependencies.
- Fixed a bug where data stopped flowing in OPC Publisher bug on reconnect when subscription ids do not match server state (#2055)
- Ability to select the message timestamp and behavior of Heartbeat
  - Ability to generate heartbeats with different sourceTimestamp and serverTimestamp (#2049)
  - Added a behavior option to update the source timestamp relative to the LKV to mimic previous behavior (#2048)
- First Heartbeat message now dows not send anymore GoodNoData status when the monitored item is not yet live (#2041)
- MQTT reconnect and disconnect reasons are now logged as errors (#2025)
- Fixed an issue where Publisher failed to subscribe to a node because namespace table entry was not available during connect/subscribe (#2042) (Thank you @quality-leftovers for contributing this fix)
- The default application name used to create certs is now the same as in 2.8 (#2047)
- Batch size and Batch trigger have same defaults now than 2.8 (#2045)
- Fix a bug where the user was not added correctly as owner in the aad_register.ps1 script (Thank you @0o001 for contributing this fix)

## Azure Industrial IoT OPC Publisher 2.9.1

We are pleased to announce the release of version 2.9.1 of OPC Publisher. 2.9.1 comes with several bug and security fixes and is the latest supported release.

### Changes in 2.9.1

- Update all dependencies to latest version, in particular latest opc ua .net stack release 372
- MQTT samples and making MQTT method calls over RPC working with Mqtt.net (#2039)
- IoT Hub direct method samples (#2032)
- The CLI switches using the environment variable names do no work (#2021)
- New command line setting to select which clock is chosen for message timestamp (publish time or publisher current time) (#2035)
- Fix negative count shown in diagnostics (#2031)
- Documentation updates and fixes

## Azure Industrial IoT OPC Publisher 2.9.0

We are pleased to announce the release of version 2.9.0 of OPC Publisher. This release adds several new features including support for reverse connect.

### Changes in 2.9.0

For the list of changes released so far in preview releases please see:

- [Changes in 2.9.0 Preview 4](#changes-in-290-preview-4)
- [Changes in 2.9.0 Preview 3](#changes-in-290-preview-3)
- [Changes in 2.9.0 Preview 2](#changes-in-290-preview-2)
- [Changes in 2.9.0 Preview 1](#changes-in-290-preview-1)

The final release contains the following changes:

- Restart announcement tests are disabled and need to work again #1988
- Event item filter errors should be an error condition and re-evaluated periodically. #2014
- Feedback / Message / Metric on OPC Server down or wrong credentials provided in OPC Publisher 2.8+ #1445
- Option to directly enable subscription during create rather than using SetPublishingMode request. (BadNodeIdUnkown OPCPublisher) #1773
- Option to enable swagger UI in release build publishers. (OPCPublisher:latest API Server not working) #2009
- Publisher should try first to select the endpoint matching the configured url in Connection model. #2013
- Select security mode and profile in configuration #2008
- Using G8/17 to serialize float/double for precision Float/Double Serializer #1709
- Send extension fields as part of datasetmessage and enable free form configuration of extension fields (move to Variant value) #1940
- Command line option to disable complex type loading. #1953
- publishednodes.json: When OpcAuthenticationUsername is set but OpcAuthenticationPassword is erroneously not, a misleading error message appears #1059
- OPC Publisher REST API and Direct Methods Reference (Publish, Twin, Discovery) documentation  #1976/#1504
- AAD-Register fails on converting System.String to type "System.Security.SecureString" #2015

## Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 4

We are pleased to announce the fourth **preview** release of version 2.9.0 of OPC Publisher. This release adds several new features including support for reverse connect.

> IMPORTANT: Preview releases are only supported through GitHub issues. This particular release due to the number of changes included might have backward compatibility breaks that have not yet been documented. Please file issues and we will try to address them ahead of GA release.

### Changes in 2.9.0 Preview 4

- [OPC Publisher] Better documentation for REST API call with API Key #1991
- [OPC Publisher] Fix Can't run OPC UA Publisher standalone module inside IoT Edge Simulator environment (iotedgehubdev) #1708
- [OPC Publisher] No BypassCertVerification required in Simulation with VS Code / iotedgehubdev #1922
- [OPC Publisher] Fix  standalone OPC-Publisher in Edge Simulator doesn't work #1999
- [OPC Publisher] Fix 2.9 preview 3 not supporting iot edge decrypt of passwords #1998
- [OPC Publisher] Application uri of the publisher should be unique for the deployment. #1986
- [OPC Publisher] Better documentation for --strict mode in OPC Publisher, Better documentation for all other message profiles. #1938
- [OPC Publisher] A way to node id in nsu and ns index format like ns=1;s=CycleCounter using API and telemetry #1057
- [OPC Publisher] Support for Reverse Connect # 1586

## Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 3

We are pleased to announce the third **preview** release of version 2.9.0 of OPC Publisher. This release adds several new features including support for cyclic reads.

> IMPORTANT: Preview releases are only supported through GitHub issues. This particular release due to the number of changes included might have backward compatibility breaks that have not yet been documented. Please file issues and we will try to address them ahead of GA release.

### Changes in 2.9.0 Preview 3

- [OPC Publisher] Heartbeat behavior is not deterministic - implement heartbeat as value change watchdog. #1993
- [OPC Publisher] When enabling client linger in preview2 a failing connection retries forever  #1985
- [OPC Publisher] Alarm condition integration tests have been partially disabled and need to enable again #1989
- [OPC Publisher] Need OPC Publisher to poll data from server periodically rather than subscribing. #1934
- [OPC Publisher] Polling mechanism instead of PubSub in OPCPublisher #605
- [OPC Publisher] Configuration of the published nodes by browse path #47
- [OPC Publisher] deploy.ps1 fails upon deploying "platform" #1981
  [OPC Publisher] Updated documentation and nuget dependencies

## Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 2

We are pleased to announce the second **preview** release of version 2.9.0 of OPC Publisher. This release marks a large change for the Industrial IoT components and features. We have combined all edge functionality into the OPC Publisher edge module which now boasts not just MQTT and IoT Hub direct method access, but a full HTTP REST endpoint that can be used to configure its functionality. The cloud components also have been combined into a single Web API, which we recommend to deploy into Azure App Service for reduced cost and simplified operations.

> IMPORTANT: Preview releases are only supported through GitHub issues. This particular release due to the number of changes included might have backward compatibility breaks that have not yet been documented. Please file issues and we will try to address them ahead of GA release.

### Changes in 2.9.0 Preview 2

- New Namespaces for all projects and simplified code structure. There are now 2 SDK projects, one for the OPC Publisher module, and another for the optional cloud WebAPI companion service.
- Ability to run platform (modules, services) "standalone" on the edge #464
  - [OPC Discovery] has been included into the OPC Publisher module, the container name must be updated to refer to OPC Publisher.
  - [OPC Discovery] A new synchronous FindServer API has been added to allow discovery by discovery url through a single API call.
  - [OPC Twin] has been included into the OPC Publisher module, the container name must be updated to refer to OPC Publisher.
  - [OPC Twin] we removed the Activate and Deactivate calls.
  - [OPC Twin] OPC TWIN Method call #996
  - Support for opc-twin module api direct method calls with input arguments (not requiring OPC Twin micro services) #1512
- Support for a new TestConnection API to test a connection to a server and receiving detailed error information back.
- [OPC Publisher] (breaking change) The publisher id in each message is now always the same value across all writer groups rather than previously where a random guid was used per writer group when a publisher id was not configured.
- [OPC Publisher] Several bug fixes for preview 1 (#1964)
  - [OPC Publisher] DatasetMessage SequenceNumber is now correctly incremented (preview) (#1961)
- [OPC Publisher] Enabling using DisplayNames defined for the event fields in pn.json as keys in the payload of dataset messages (#1963)
- [OPC Publisher] Request opc server's nodes information #1960
- [OPC Publisher] dotnet publish can be used to build a docker container for OPC Publisher #1949
- [OPC Publisher] Metrics output and log output showing number of sessions currently active (related to #1923)
- [OPC Publisher] Added new OPC UA stack which addressess #1937 and latest CVE's
- [All micro services] Have been combined into a single WebAPI with the same resource paths as the 2.8 AKS deployment and all-in-one service.
  - [OPC Registry service] Supervisor, Discoverer entities have been removed, but the API has been layered on top of the publisher entity for backwards compatibiltiy. Do not use these API's anymore.
  - [OPC Registry service] A new RegisterEndpoint API has been added that calls the new sync FindServer API and adds the result into the registry in one call.
  - [Telemetry processor] The telemetry and onboarding processors have been integrated into the WebAPI, but only forwards to SignalR. The secondary event hub has been removed. If you need to post process telemetry you must read telemetry data directly from IoT Hub.
- Document the diagnostics output and troubleshooting guide #1952

## Azure Industrial IoT OPC Publisher 2.9.0 Community Preview 1

We are pleased to announce the first **preview** release of version 2.9.0 of OPC Publisher. This release contains several requested features and fixes issues discovered.

> IMPORTANT: Preview releases are only supported through GitHub issues.

### Changes in 2.9.0 Preview 1

- [OPC Publisher] [Alarms and Events](./opc-publisher/readme.md#configuring-event-subscriptions) support to OPC Publisher. You can now subscribe to events in addition to value changes and in the familar ways using the published nodes json configuration and direct methods.
- [OPC Publisher] Full Deadband filtering. We introduced data change triggers in 2.8.4 and are now supporting the full data change filter configuration to configure percent and absolute deadband as defined in OPC UA.
- [OPC Publisher] Support setting discard new configuration on command line.
- [OPC Publisher] Full support for UADP network message encoding, as well as reversible Json profiles (JsonReversible)
- [OPC Publisher] Support for smaller network messages by removing network message and dataset message headers (adding new MessageType.RawDataset and MessageType.DataSetMessages).
- [OPC Publisher] Support for gzip encoded Json (MessageEncoding.JsonGzip and MessageEncoding.JsonReversibleGzip)
- [OPC Publihser] Strict mode to adhere to OPC UA Part 14 and Part 6, including message formats and data type serialization.
- [OPC Publisher] Adding back support for --sf and SkipFirst property to skip the first data change notification to be sent when subscription is created.
- [All IoT Edge modules] Configuration to optionally enable MQTT topic publishing and command control via an MQTT broker instead of IoT Edge EdgeHub.
- [All IoT Edge modules] Update OPC UA stack to latest .371 version.

## Azure Industrial IoT Platform Release 2.8.6

We are pleased to announce the release of version 2.8.6 of our Industrial IoT Platform components as latest patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

### Changes in 2.8.6

- [All] Update to latest JSON.net nuget dependency.
- [All] Use latest OPC UA .net stack 371.
- Update all images to use latest version of Alpine base image

## Azure Industrial IoT Platform Release 2.8.5

We are pleased to announce the release of version 2.8.5 of our Industrial IoT Platform components as latest patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

### Changes in 2.8.5

- [All] Update to latest dependencies.
- [All] Use latest OPC UA .net stack 371.
- [OPC Publisher] Fix a crash in the diagnostics output timer in orchestrated mode when endpoint url is reported null. (#1955)

## Azure Industrial IoT Platform Release 2.8.4

We are pleased to announce the release of version 2.8.4 of our Industrial IoT Platform components as latest patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

### IMPORTANT - PLEASE READ

- IoT Edge 1.1 LTS will be going out of support on 12/13/2022, please [update your IoT Edge gateways to IoT Edge 1.4 LTS](https://learn.microsoft.com/azure/iot-edge/how-to-update-iot-edge).

  > To continue deploying the 1.1 LTS modules to your environment follow [these instructions](./opc-publisher/readme.md#getting-started).

- Windows container images are no longer supported in IoT Edge 1.4 and consequentially have been removed from this release. Please use [IoT Edge for Linux on Windows (EFLOW) 1.4](https://learn.microsoft.com/windows/iot/iot-enterprise/azure-iot-edge-for-linux-on-windows) as your IoT Edge environment on Windows.  

  > IoT Edge 1.4 LTS EFLOW is supported as a Preview Feature in this release.

  - You must update your Windows based IoT Edge environment to EFLOW **ahead of deploying the platform**.  
  - Simulation deployed as part of the ./deploy.ps1 script now deploys EFLOW on a Windows VM Host (Preview Feature).  This requires nested virtualization.  The Azure subscription and chosen region must support Standard_D4_v4 VM which supports nested virtualization or deployment of simulated Windows gateway will be skipped.
  - Network scanning on IoT Edge 1.4 LTS EFLOW using OPC Discovery is not supported yet. This applies to the deployed [simulation environment](./web-api/readme.md#getting-started) and engineering tool. You can register servers using a discovery url using the [registry service's registration REST API](./web-api/api.md).

### Changes in 2.8.4

- Updated .net to .net 6.0 LTS from .net core 3.1 LTS which will be going out of support on 12/13/2022.
- Updated nuget dependencies to their .net 6 counterpart or to their latest compatible release.
- [All IoT Edge modules] Updated IoT Edge dependency to IoT Edge 1.4 LTS from 1.1 LTS which will be going out of support on 12/13/2022.
- [All IoT Edge modules] (Preview) Support for [IoT Edge EFLOW 1.4 LTS](https://learn.microsoft.com/windows/iot/iot-enterprise/azure-iot-edge-for-linux-on-windows)
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

- Under EFLOW, by default the Application instance certificate is generated by each module and not automatically shared. To work around this, configure EFLOW and modules deployed manually to use a [shared host volume](https://learn.microsoft.com/azure/iot-edge/how-to-share-windows-folder-to-vm?view=iotedge-2018-06) to map the pki folders.
- Discovering using IP Address and port ranges is not supported on EFLOW.

## Azure Industrial IoT Platform Release 2.8.3

We are pleased to announce the release of version 2.8.3 of our Industrial IoT Platform components as a third patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates fixes, performance optimizations and bugfixes.

> IMPORTANT
> We suggest updating from the version 2.5 or later to ensure secure operations of your deployment. OPC Publisher 2.8.3 addresses backwards compatibilities issues with version 2.5.x.

### Security related fixes in 2.8.3

- Updated OPC UA Stack NuGet to the latest (1.4.368.58) addressing various security issues
- Upgraded SSH.NET package to 2020.0.2 to address [CVE-2022-29245](https://nvd.nist.gov/vuln/detail/CVE-2022-29245).

### Fundamentals related fixes in 2.8.3

- [OPC Publisher] option to route telemetry to a specific output route was added

### Bug fixes in 2.8.3

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

### Fundamentals related fixes in 2.8.2

- [OPC Publisher] Implemented the backwards compatible [Direct Methods API](./opc-publisher/directmethods.md) of 2.5.x publisher. The migration path is documented [here](./opc-publisher/migrationpath.md)
- [OPC Publisher] Optimizations in opc subscriptions/monitored items management in case of configuration changes. Only incremental changes are applied to a subscription.
- [OPC Publisher] Added support for setting QueueSize on monitored items for publisher in standalone mode.
- [OPC Publisher] Hardened the retry mechanism for activating monitored items.

### Backwards Compatibility Notes for release 2.8.2

- [OPC Publisher] NodeId shows up now in telemetry in the exact format as specified in the configuration. Before 2.8.2, the NodeId was always reported as `Namespace#NodeId`
    > E.g. : When configuring in pn.json file a NodeId like `nsu=http://mynamespace.com/;i=1`
    >
    > - OPC Publisher 2.8.1 telemetry reports `http://mynamespace.com/#i=1`
    > - OPC Publisher 2.8.2 telemetry reports `nsu=http://mynamespace.com/;i=1`
    >
- [OPC Publisher] configuration of duplicate nodeIds in the same data set writer, respectively same subscription is no longer allowed.

## Azure Industrial IoT Platform Release 2.8.1

We are pleased to announce the release of version 2.8.1 of our Industrial IoT Platform components as the first patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates, bugfixes and performance optimizations.

> IMPORTANT: Please note that OPC Publisher 2.8.1 is not backwards compatible with version 2.5.x.

## Azure Industrial IoT Platform Release 2.8

We are pleased to announce the release of version 2.8 of our Industrial IoT Platform as well as the declaration of Long-Term Support (LTS) for this version.
While we continue to develop and release updates to our ongoing projects on GitHub, we now also offer a branch that will only get critical bug fixes and security updates starting in July 2021. Customers can rely upon a longer-term support lifecycle for these LTS builds, providing stability and assurance for the planning on longer time horizons our customers require. The LTS branch offers customers a guarantee that they will benefit from any necessary security or critical bug fixes with minimal impact to their deployments and module interactions. At the same time, customers can access the latest updates in the main branch to keep pace with the latest developments and fastest cycle time for product updates.

> IMPORTANT: We suggest to update from the version 2.6 or later to ensure secure operations of your deployment. 2.8.0 is not backwards compatible with version 2.5.x

Version 2.8.0 includes an updated version of the IoT Edge Runtime, a new Linux base image for all Linux deployments, and several bug fixes. The detailed changes can be found [here](https://github.com/Azure/Industrial-IoT/releases/tag/2.8.0).
