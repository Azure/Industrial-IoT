# Microsoft OPC Publisher <!-- omit in toc -->

[Home](../readme.md)

OPC Publisher is a module that runs on [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) and bridges the gap between industrial assets and the Microsoft Azure cloud. It connects to OPC UA server systems and publishes telemetry data to [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub/) in various formats, including IEC62541 OPC UA PubSub standard format (*not supported in versions < 2.7.x*).

Here you find information about

## Table Of Contents <!-- omit in toc -->

- [Overview](#overview)
- [Getting Started](#getting-started)
  - [Install IoT Edge](#install-iot-edge)
  - [Deploy OPC Publisher from Azure Marketplace](#deploy-opc-publisher-from-azure-marketplace)
  - [Specifying Container Create Options in the Azure portal](#specifying-container-create-options-in-the-azure-portal)
  - [Deploy OPC Publisher using Azure CLI](#deploy-opc-publisher-using-azure-cli)
  - [Deploy OPC Publisher using the Azure Portal](#deploy-opc-publisher-using-the-azure-portal)
- [How OPC Publisher works](#how-opc-publisher-works)
- [Configuring OPC Publisher](#configuring-opc-publisher)
  - [Configuration via Configuration File](#configuration-via-configuration-file)
    - [Configuring Security](#configuring-security)
    - [Configuring event subscriptions](#configuring-event-subscriptions)
      - [Simple event filter](#simple-event-filter)
      - [Advanced event filter configuration](#advanced-event-filter-configuration)
      - [Condition handling options](#condition-handling-options)
  - [Persisting OPC Publisher Configuration](#persisting-opc-publisher-configuration)
- [Discovering OPC UA servers with OPC Publisher](#discovering-opc-ua-servers-with-opc-publisher)
  - [Discovery Configuration](#discovery-configuration)
  - [One-time discovery](#one-time-discovery)
  - [Discovery Progress](#discovery-progress)
- [OPC UA Client (OPC Twin)](#opc-ua-client-opc-twin)
- [OPC Publisher API](#opc-publisher-api)
- [OPC Publisher Telemetry Formats](#opc-publisher-telemetry-formats)
- [OPC UA Certificates management](#opc-ua-certificates-management)
  - [Use custom OPC UA application instance certificate in OPC Publisher](#use-custom-opc-ua-application-instance-certificate-in-opc-publisher)
- [OPC UA stack](#opc-ua-stack)
- [Performance and Memory Tuning OPC Publisher](#performance-and-memory-tuning-opc-publisher)

## Overview

Microsoft OPC Publisher runs on Azure [IoT Edge](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub) and connects OPC UA-enabled servers to Azure. It can be [configured](#configuring-opc-publisher) using Azure IoT Hub, through MQTT/HTTPS locally (Preview) or via configuration file.

OPC Publisher is a feature rich OPC UA client/server to OPC UA Pub/Sub translator. Per configuration it sets up OPC UA subscriptions to monitor data (OPC UA nodes) using an integrated [OPC UA stack](#opc-ua-stack). When a data value change or event of an OPC UA node is reported, it transcodes the OPC UA notification using the configured encoding and publishes it to IoT Hub or MQTT broker of choice.

With OPC Publisher you can also browse a server's data model, read and write ad-hoc data, or call methods on your assets. This [capability](#opc-ua-client-opc-twin) can be accessed from the cloud. OPC Publisher also supports [discovering](#discovering-opc-ua-servers-with-opc-publisher) OPC UA-enabled assets on the shop floor. When it finds an asset either through a discovery url or (optionally) active network scanning, it queries the assets endpoints (including its security configuration) and reports the results to IoT Hub or returns them from the respective [API call as response](api.md#find-server-with-endpoint).

The IoT Edge gateways support nested ISA 95 (Purdue) topologies. It needs to be placed where it has access to all industrial assets that are to be connected, and a IoT Edge device needs to be placed at every layer leading to the internet.

> Note that this might require configuring a specific route from IoT Edge to the public Internet through several on-premise routers. In terms of firewall configuration, IoT Edge just needs a single outbound port to operate, i.e., port 443.

## Getting Started

### Install IoT Edge

The industrial assets (machines and systems) are connected to Azure through modules running on an [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) industrial gateway.

You can purchase industrial gateways compatible with IoT Edge. Please see our [Azure Device Catalog](https://catalog.azureiotsolutions.com/alldevices?filters={"3":["2","9"],"18":["1"]}) for a selection of industrial-grade gateways. Alternatively, you can setup a local VM.

You can also manually [create an IoT Edge instance for an IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device) and install the IoT Edge runtime following the [IoT Edge setup documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). The IoT Edge Runtime can be installed on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-for-linux-on-windows).

For more information check out:

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)

### Deploy OPC Publisher from Azure Marketplace

Use our released docker container for OPC Publisher available in the Microsoft Container Registry rather than building from sources. The easiest way to deploy OPC Publisher is through the [Azure Marketplace](https://azuremarketplace.microsoft.com/marketplace/apps/microsoft_iot.iotedge-opc-publisher).

Select the "Get It Now" button to log into the [Azure portal](https://portal.azure.com) and deploy OPC Publisher. The following steps are required:

1. Pick the Azure subscription to use. If no Azure subscription is available, one must be created.
2. Pick the IoT Hub the OPC Publisher is supposed to send data to. If no IoT Hub is available, one must be created.
3. Pick the IoT Edge device OPC Publisher is supposed to run on. If no IoT Edge device exists, one must be created).
4. Select "Create". The "Set modules on Device" page for the selected IoT Edge device opens.
5. Select on "OPCPublisher" to open the OPC Publisher's "Update IoT Edge Module" page and then select "Container Create Options".
6. Validate "container create options" based on your usage of OPC Publisher. For more information, see next section.

> We recommend to use a floating version tag ("2.9") when deploying the web api container but not "latest".

### Specifying Container Create Options in the Azure portal

Container create options are used to specify the container and configuration command line arguments of OPC Publisher. The docker create options can be specified in the "Update IoT Edge Module" page of OPC Publisher and must be in JSON format. Specifically the OPC Publisher command line arguments can be specified via the "Cmd" key. Here an example for a configuration on a Linux host system:

``` json
{
    "Cmd": [
        "-c", // 2.9+ only
        "--cl=5", // 2.9+ only
        "--PkiRootPath=/mount/pki",
        "--pf=/mount/published_nodes.json",
        "--cf", // 2.9+ only
        "--mm=PubSub",
        "--me=Json",
        "--fd=false",
        "--bs=100",
        "--bi=1000",
        "--aa"
    ],
    "HostConfig": {
        "Binds": [
            "/opcpublisher:/mount"
        ],
        "CapDrop": [
            "CHOWN",
            "SETUID"
        ]
    }
}
```

With these options specified, OPC Publisher will read the configuration file `./published_nodes.json`. The OPC Publisher's working directory is set to `/mount` at startup and thus OPC Publisher will read the file `/mount/published_nodes.json` inside its container.
OPC Publisher's log file will be written to `/mount` and the `CertificateStores` directory (used for OPC UA certificates) will also be created in this directory.

To make these files available in the IoT Edge host file system, the container configuration requires a bind mount volume. The **Mounts** section will  map the directory `/mount` to the host directory `/opcpublisher`. To not loose the OPC Publisher configuration across restarts [all configuration files should be persisted](#persisting-opc-publisher-configuration). This requires a bind mount. Without it all configuration changes will be lost when OPC Publisher is restarted.

> IMPORTANT: The `/opcpublisher` directory must be present on the host file system, otherwise OPC Publisher will fail to start.

The `CapDrop` option will drop the CHOWN (user can’t makes arbitrary changes to file UIDs and GIDs) and SETUID (user can’t makes arbitrary manipulations of process UIDs) capabilities for security reason.

A connection to an OPC UA server using its hostname without a DNS server configured on the network can be achieved by adding an `ExtraHosts` entry to the `HostConfig` section:

``` json
"HostConfig": {
    "ExtraHosts": [
        "opctestsvr:192.168.178.26"
    ]
}
```

### Deploy OPC Publisher using Azure CLI

1. Obtain the IoT Hub name and device id of the [installed IoT Edge](#install-iot-edge) Gateway.

1. Install the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli). You must have at least `v2.0.24`, which you can verify with `az --version`.

1. Add the [IoT Edge Extension](https://github.com/Azure/azure-iot-cli-extension/) with the following commands:

    ```bash
    az extension add --name azure-cli-iot-ext
    ```

To deploy all required modules using Az...  

1. Save the following content into a `deployment.json` file:

    ```json
    {
      "modulesContent": {
        "$edgeAgent": {
          "properties.desired": {
            "schemaVersion": "1.1",
            "runtime": {
              "type": "docker",
              "settings": {
                "minDockerVersion": "v1.25",
                "loggingOptions": "",
                "registryCredentials": {}
              }
            },
            "systemModules": {
              "edgeAgent": {
                "type": "docker",
                "settings": {
                  "image": "mcr.microsoft.com/azureiotedge-agent:1.4",
                  "createOptions": ""
                }
              },
              "edgeHub": {
                "type": "docker",
                "status": "running",
                "restartPolicy": "always",
                "settings": {
                  "image": "mcr.microsoft.com/azureiotedge-hub:1.4",
                  "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"5671/tcp\":[{\"HostPort\":\"5671\"}], \"8883/tcp\":[{\"HostPort\":\"8883\"}],\"443/tcp\":[{\"HostPort\":\"443\"}]}}}"
                },
                "env": {
                  "SslProtocols": {
                    "value": "tls1.2"
                  }
                }
              }
            },
            "modules": {
              "publisher": {
                "version": "1.0",
                "type": "docker",
                "status": "running",
                "restartPolicy": "always",
                "settings": {
                  "image": "mcr.microsoft.com/iotedge/opc-publisher:2.9",
                  "createOptions": "{\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
                }
              }
            }
          }
        },
        "$edgeHub": {
          "properties.desired": {
            "schemaVersion": "1.0",
            "routes": {
              "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
              "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
            },
            "storeAndForwardConfiguration": {
              "timeToLiveSecs": 7200
            }
          }
        }
      }
    }
    ```

1. Use the following command to apply the configuration to an IoT Edge device:

   ```bash
   az iot edge set-modules --device-id [device id] --hub-name [hub name] --content ./deployment.json
   ```

   The `device id` parameter is case-sensitive. The content parameter points to the deployment manifest file that you saved.
    ![az iot edge set-modules output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/set-modules.png)

1. Once you've deployed modules to your device, you can view all of them with the following command:

   ```bash
   az iot hub module-identity list --device-id [device id] --hub-name [hub name]
   ```

   The device id parameter is case-sensitive. ![az iot hub module-identity list output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/list-modules.png)

More information about az and IoT Edge can be found [here](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli).

### Deploy OPC Publisher using the Azure Portal

To deploy OPC Puublisher to the IoT Edge Gateway using the Azure Portal...

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to the IoT Hub deployed earlier.

1. Select **IoT Edge** from the left-hand menu.

1. Click on the ID of the target device from the list of devices.

1. Select **Set Modules**.

1. In the **Deployment modules** section of the page, select **Add** and **IoT Edge Module.**

1. In the **IoT Edge Custom Module** dialog use `publisher` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-publisher:2.9
   ```

   On Linux use the following *create options* if you intend to use the network scanning capabilities of the module:

   ```json
   {"NetworkingConfig":{"EndpointsConfig":{"host":{}}},"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"],
   "CapDrop":["CHOWN", "SETUID"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/azure/iot-edge/module-composition#define-or-update-desired-properties).

1. Select **Save** and then **Next** to continue to the routes section.

1. In the routes tab, paste the following

    ```json
    {
      "routes": {
        "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
        "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
      }
    }
    ```

    and select **Next**

1. Review your deployment information and manifest.  It should look like the deployment manifest found in the [previous section](#deploy-opc-publisher-using-azure-cli).  Select **Submit**.

1. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

1. Add your own or other modules from the Azure Marketplace using the steps above.

For more in depth information check out [the Azure IoT Edge Portal documentation](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal).

## How OPC Publisher works

Publishing OPC UA telemetry from an OPC UA server works as follows:

1. An OPC UA server exposes variable nodes (also sometimes called "tags") which make sensor readings accessible, or nodes that allow a client to subscribe to events.

1. The OPC Publisher can be configured to connect to one or more selected OPC UA server endpoints. Based on the configuration the OPC Publisher OPC UA client creates subscriptions requesting to be notified when the value of the specified nodes change or an event occurs.

1. The publisher groups nodes in the configuration into groups of `Dataset Writers` which are akin to OPC UA subscriptions. These subscriptions refer to node ids (in OPC UA also called monitored items). Nodes can be configured with `SamplingInterval`, `PublishingInterval`, `DataSetWriterId`, `DataSetWriterGroup` and `Heartbeat` (keep-alive key frames)

   - `DataSetWriterId`: A logical name of a subscription to an endpoint on a OPC UA server. A writer can only have 1 publishing interval andin case of event subscription, 1 event node. Should multiple be specified then the writer is broken into smaller writers. A data set writer writes data sets, which are a set of OPC UA data values or events inside a OPC UA PubSub network message.

   - `DataSetWriterGroup`: A logical group of data set writers. These define the content of a OPC UA PubSub network message.

   - `SamplingInterval`: The cyclic time in milliseconds, in which a node in a writer is sampled for updates. This is not applicable for events.

   - `PublishingInterval`: The cyclic time in milliseconds, in which changes to a set of nodes (notifications) are sent to the subscriber (OPC Publisher). A small interval minimizes latency at the cost of network traffic and server load. For low latency it should be set to the smallest sampling interval and appropriate queue size values should be configured to avoid message loss.

   - `Heartbeat`: Cyclic time in seconds, in which to send keep-alive messages to indicate that the connection is still being used, in case no notifications are available

1. Data change notifications or event notifications are published by the OPC UA server to OPC Publisher. OPC UA only sends value changes, that means, if a value has not changed in the publishing cycle it is not send. If you need all values in a message you can use the `KeyFrameCount` or `HeartbeatInterval` settings.

1. The OPC Publisher can be configured to send notifications as soon as they arrive or batch them before sending which saves bandwidth and increases throughput. Sending a batch is triggered by exceeding the threshold of a specified number of messages or by exceeding a specified time interval.

1. OPC Publisher groups and encodes the telemetry events using the specified messaging mode and message encoding format. More information can be found [here](./messageformats.md).

1. The encoded telemetry events are added as the network message, which cannot exceed 256kB, the maximum size of an IoT Hub message. The publisher will try to split messages to avoid loosing data, but has a runtime cost.

1. OPC Publisher also emits Metadata messages in case of PubSub encoding which can be used to learn more about the message content and support decoding in some cases.

1. The network and metadata messages are sent to the northbound destination chosen. By default IoT Hub stores them for the configured retention time (default: 1 day, max: 7 days, dependent on the size of the ingested messages as well, see [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-read-builtin) for more details).

1. Messages can be consumed by applications or other services at the northbound destination.

## Configuring OPC Publisher

OPC Publisher has several interfaces that can be used to configure it.  

- [Configuration via configuration file](#configuration-via-configuration-file)
- [Command Line options configuration](./commandline.md)
- [Direct method runtime configuration](./directmethods.md)
- [How to migrate from previous versions of OPC Publisher](./migrationpath.md)

### Configuration via Configuration File

The simplest way to configure OPC Publisher is via a configuration file. A basic configuration file looks like this:

``` json
[
  {
    "EndpointUrl": "opc.tcp://testserver:62541/Quickstarts/ReferenceServer",
    "UseSecurity": true,
    "OpcNodes": [
      {
        "Id": "i=2258",
        "OpcSamplingInterval": 2000,
        "OpcPublishingInterval": 5000,
        "DisplayName": "Current time"
      }
    ]
  }
]
```

Other example configuration files are provided via [`publishednodes_2.5.json`](publishednodes_2.5.json?raw=1) and [`publishednodes_2.8.json`](publishednodes_2.8.json?raw=1).

The configuration file syntax has been enhanced over time. OPC Publisher read old formats and converts them into the current format when persisting the configuration. OPC Publisher regularly persists the configuration file.

OPC UA optimizes network bandwidth by only sending changes to OPC Publisher when the data item's value has changed. Some use cases require to publish data values in constant intervals. OPC Publisher supports a "heartbeat" for every configured telemetry event that can be enabled by specifying the `HeartbeatInterval` key in the data item's configuration. The interval is specified in seconds:

``` json
 "HeartbeatInterval": 3600,
```

OPC UA sends the current data value when OPC Publisher connects to the OPC UA server. To prevent publishing this telemetry on startup to IoT Hub, the `SkipFirst` key can be additionally specified in the data item's configuration:

``` json
 "SkipFirst": true,
```

#### Configuring Security

IoT Edge automatically provides OPC Publisher with a secure configuration to access IoT Hub. OPC UA does use X.509 certificates for:

- mutual authentication of clients and server systems
- encrypted communication between the systems
- optionally for user authentication

OPC Publisher can be configured to store these certificates in a file system based certificate store. During startup, OPC Publisher checks if there's already a private certificate it can use. If not, a self-signed certificate is created. Self-signed certificates don't provide any trust value and we don't recommend using them in production.

Encrypted communication can be enabled per endpoint via the `"UseSecurity": true,` flag. By default OPC Publisher will connect to an endpoint using the least secure mode OPC Publisher and the OPC UA server support.

By default OPC Publisher does use no user authentication (anonymous). However, OPC Publisher supports user authentication using username and password. These credentials can be specified using the configuration file as follows:

``` json
"OpcAuthenticationMode": "UsernamePassword",
"OpcAuthenticationUsername": "usr",
"OpcAuthenticationPassword": "pwd",
```

OPC Publisher version 2.5 and below encrypts the username and password in the configuration file. Version 2.6 and above stores them in plain text.

#### Configuring event subscriptions

> Starting from version 2.9

OPC Publisher supports two types of event filter configurations you can specify:

- [Simple event filter](#simple-event-filter) configuration mode, where you specify the source node and the event type you want to filter on and then the OPC Publisher constructs the select and where clauses for you.
- [Advanced event filter](#advanced-event-filter-configuration) configuration mode where you explicitly specify the select and where clauses.

In the configuration file you can specify how many event configurations as you like and you can also combine events and data nodes for a single endpoint.

In addition you can configure optional [Condition](#condition-handling-options) reporting where OPC Publisher reports retaind conditions at a configured time periodic rate in seconds.

##### Simple event filter

As highlighted in the example above you can specify namespaces both by using the index or the full name for the namespace. Also look at how the BrowsePath can be configured.

Here is an example of a configuration file in simple mode:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
        "OpcNodes": [
            {
                "Id": "i=2253",
                "DisplayName": "SimpleEventServerEvents",
                "EventFilter": {
                    "TypeDefinitionId": "ns=2;i=235"
                }
            }
        ]
    }
]
```

To subscribe to an event you specify the source node (in this case the server node which has node id `i=2253`) and the event type to monitor (in this case `ns=2;i=235`).

When you use the simple configuration option above, the OPC Publisher does two things:

- It looks at the TypeDefinitionId of the event type to monitor and traverses the inheritance tree for that event type, collecting all fields. Then it constructs a select clause with all the fields it finds.
- It creates a where clause that is OfType(TypeDefinitionId) to filter the events to just the selected event type.

##### Advanced event filter configuration

To configure an advanced event filter you have to specify a full event filter which at minimum consists of three things:

- The source node you want to receive events for (in the example below again the server node which has node id `i=2253`).
- A select clause specifying which fields should be in the reported event. This can include a data set class field id that is then used as identifier in the dataset metadata for the dataset class.
- A where clause specifying the filter AST.

Here is an example of a configuration file that selects events using an advanced event filter:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
        "OpcNodes": [
            {
                "Id": "i=2253",
                "DisplayName": "SimpleEventServerEvents",
                "EventFilter": {
                    "SelectClauses": [
                        {
                            "TypeDefinitionId": "i=2041",
                            "DataSetClassFieldId ": "D3EB3722-E956-4E5E-925B-FB727B737520",
                            "BrowsePath": [
                                "EventId"
                            ]
                        },
                        {
                            "TypeDefinitionId": "i=2041",
                            "DataSetClassFieldId ": "A435F616-CE1E-4FBD-A819-03175EB49231",
                            "BrowsePath": [
                                "Message"
                            ]
                        },
                        {
                            "TypeDefinitionId": "ns=2;i=235",
                            "DataSetClassFieldId ": "BD236A98-8DA3-40A1-B8E8-00AB23A6B5E9",
                            "BrowsePath": [
                                "/2:CycleId"
                            ]
                        },
                        {
                            "TypeDefinitionId": "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            "DataSetClassFieldId ": "9F9A420B-509E-488B-A7A4-F320F8223E9E",
                            "BrowsePath": [
                                "/http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep"
                            ]
                        }
                    ],
                    "WhereClause": {
                        "Elements": [
                            {
                                "FilterOperator": "OfType",
                                "FilterOperands": [
                                    {
                                        "Value": "ns=2;i=235"
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
        ]
    }
]
```

The exact syntax allowed can be found in the OPC UA reference documentation. Note that not all servers support all filter capabilities. You can troubleshoot issues using the OPC Publisher logs.

##### Condition handling options

In addition to event subscription, you can also configure events to enable condition handling.

When configured, OPC Publisher listens to ConditionType derived events, records unique occurrences of them and periodically sends out all condition events that have the Retain property set to True. This enables you to continuously get a snapshot view of all active alarms and conditions which can be very useful for dashboard-like scenarios.

Here is an example of a configuration for condition handling:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/AlarmConditionServer",
        "OpcNodes": [
            {
                "DisplayName": "AlarmConditions",
                "Id": "i=2253",
                "EventFilter": {
                    "TypeDefinitionId": "i=2915"
                },
                "ConditionHandling": {
                    "UpdateInterval": 10,
                    "SnapshotInterval": 20
                }
            }
        ]
    }
]
```

The `ConditionHandling` section consists of the following properties:

- `UpdateInterval` - the interval, in seconds, which a message is sent if anything has been updated during this interval.
- `SnapshotInterval` - the interval, in seconds, that triggers a message to be sent regardless of if there has been an update or not.

One or both of these must be set for condition handling to be in effect. You can use the condition handling configuration regardless if you are using advanced or simple event filters. If you specify the`ConditionHandling` option property without an `EventFilter` property it is ignored, as condition handling has no effect for data change subscriptions.

Conditions are sent as `ua-condition` data set messages. This is a message type not part of the official standard but allows seperating condition snapshots from regular `ua-event` data set messages.

### Persisting OPC Publisher Configuration

To ensure operation of OPC Publisher over restarts, it's required to map configuration files to the host file system. The mapping can be achieved via the "Container Create Option" in the Azure portal. The configuration files are:

- the file system based directory store
- the telemetry configuration file

In version 2.6 and above, username and password are stored in plain text in the configuration file. It must be ensured that the configuration file is protected by the file system access control of the host file system. The same must be ensured for the file system based certificate store, since it contains the private certificate and private key of OPC Publisher.

## Discovering OPC UA servers with OPC Publisher

> Starting from version 2.9

OPC Publisher provides discovery services (formerly [OPC Discovery](https://github.com/Azure/Industrial-IoT/tree/release/2.8.6)) to find assets (OPC UA servers) on the local shop floor network where the IoT Edge device is deployed. This can be programmatically controlled using API calls documented [here](./api.md). The optional [Web service](../web-api/readme.md) subscribes to the events and registers the discovered assets in Azure IoT Hub as device identities.

Example use cases:

- An industrial solution wants to detect assets which are unknown by its asset management system.
- A customer wants to access an asset without looking up the connectivity information in his asset management database or Excel spreadsheet printout from 10 years ago!
- A customer wants to onboard an asset which was recently added to a production line without causing additional network load.

Discovery supports two modes of operation:

- Active Scan mode: The local network is actively scanned by the Discovery module.
- Targeted discovery mode: A list of asset addresses can be specified to be checked.

Discovery is based on native OPC UA server functionality as specified in the OPC UA specification, which allows discovery of endpoint information including security profile information without establishing an OPC UA authenticated and encrypted OPC UA session.

The results of the discovery process are sent to cloud via the IoT Edge Hub’s and IoT Hub’s telemetry path. The optional cloud web service processes the results and onboards the discovered entities as IoT Hub identities.

The Discovery can be configured via the OPC Registry REST API and allows a fine-grained configuration of the discovery process for recurring as well as one-time scans.

### Discovery Configuration

The Discovery capability of OPC Publisher can be configured to do active network and port scanning. The following parameters can be configured for active scanning:

- address ranges (needed when hosted in a Docker context where the host interfaces are not visible)
- port ranges (to narrow or widen scanning to a list of known ports)
- number of workers and time between scans (Advanced)

> Active scanning should be used with care since it causes load on the local network and might be identified by network security software as a threat.

For a targeted discovery, the configuration requires a specific list of discovery URLs. Please note that targeted discovery disables the use of address and port ranges as only the specific list of discovery URLs are checked.

### One-time discovery

One-time discovery is supported by the OPC Publisher module and can be initiated through a API call over IoT Hub direct methods or MQTT/HTTPS (Preview).  The API is documented [here](./api.md).

A discovery configuration is part of the API request payload. All one-time discovery requests are serialized in the Discovery module at the edge, i.e. will be performed one by one.

Using the targeted discovery mode, servers can be registered using a well-known discovery URL without active scanning.

### Discovery Progress

The discovery progress as well as current request queue size is reported via the telemetry path and available in the cloud for applications by the Registry services REST interface.

## OPC UA Client (OPC Twin)

> Starting from version 2.9

The control services (formerly [OPC Twin services](https://github.com/Azure/Industrial-IoT/tree/release/2.8.6)) are provided using IoT Hub device method API as well as Web API and MQTT based request response API (Preview).

The API enables you to write applications that invoke OPC UA server functionality on OPC server endpoints. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service. This includes [Variant](../json.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response. This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint so the OPC UA server is not overburdened with 100’s of simultaneous requests.

The API is documented [here](./api.md).

Example use cases:

- A customer wants to gather the configuration of an asset by reading configuration parameters of the asset.
- A customer wants to browse an OPC UA server’s information model/address space for telemetry selection.
- An industrial solution wants to react on a condition detected in an asset by changing a configuration parameter in the asset.

## OPC Publisher API

OPC Publisher supports remote configuration through Azure IoT Hub [direct methods](./directmethods.md).

> Starting from version 2.9

In addition to the configuraton API, OPC Publisher 2.9 also supports additional [APIs](./api.md) that can be called via

- Azure IoT Hub direct methods. The method name is the operaton name and request payload as documented in the API documentation. Using the provided SDK project it is possible to also transmit and receive payloads that are larger than the 256 KB payload limitation of Azure IoT Hub.

- The same API can also be called via the HTTP Server built into OPC Publisher (Preview). The API supports browse and histrian access streaming, which the other transports do not provide. All calls must be authenticated through an API Key which must be provided as a bearer token. The API key can be read from the OPC Publisher module's module twin.

- API can also be invoked through MQTT v5 RPC calls (Preview). The API is mounted on top of tje method template (configured using the `--mtt` [command line argument](./commandline.md)). The method name follows the topic. The caller provides the topic that receives the response in the topic specified in the corresonding MQTTv5 PUBLISH packet property.

## OPC Publisher Telemetry Formats

OPC Publisher version 2.6 and above supports standardized OPC UA PubSub network messages in JSON format as specified in [part 14 of the OPC UA specification](https://opcfoundation.org/developer-tools/specifications-unified-architecture/part-14-pubsub/).

An example OPC UA PubSub message emitted by OPC Publisher version 2.9 and higher looks as follows:

``` json
{
  "MessageId": "18",
  "MessageType": "ua-data",
  "PublisherId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "DataSetClassId": "78c4e91c-82cb-444e-a8e0-6bbacc9a946d",
  "Messages": [
    {
      "DataSetWriterId": 2,
      "SequenceNumber": 18,
      "MetaDataVersion": {
        "MajorVersion": 452345324,
        "MinorVersion": 234523542
      },
      "Timestamp": "2020-03-24T23:30:56.9597112Z",
      "Status": 0,
      "Payload": {
        "Temperature": {
          "Value": 99,
          "SourceTimestamp": "2020-03-24T23:30:55.9891469Z",
          "ServerTimestamp": "2020-03-24T23:30:55.9891469Z"
        },
        "Counter": {
          "Value": 251,
          "SourceTimestamp": "2020-03-24T23:30:55.9891469Z",
          "ServerTimestamp": "2020-03-24T23:30:55.9891469Z"
        }
      },
      "DataSetWriterName": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f"
    }
  ]
}
```

OPC Publisher 2.9 and above supports strict adherence to Part 6 and Part 14 of the OPC UA specification when it comes to network message encoding. To enable strict mode use the `-c` or `--strict` command line option. For backwards compatibilty this option is off by default.

> It is highly recommended to always run OPC Publisher with strict adherence turned on.

All versions of OPC Publisher support a non-standard, simple JSON telemetry format (typically referred to as "Samples" format and which is the default setting). Samples mode is compatible with [Azure Time Series Insights](https://azure.microsoft.com/services/time-series-insights/):

``` json
[
   {
      "EndpointUrl": "opc.tcp://192.168.178.3:49320/",
      "NodeId": "ns=2;s=Pump\\234754a-c63-b9601",
      "MonitoredItem": {
         "ApplicationUri": "urn:myfirstOPCServer"
      },
      "Value": {
         "Value": 973,
         "SourceTimestamp": "2020-11-30T07:21:31.2604024Z",
         "StatusCode": 0,
         "Status": "Good"
      }
  },
  {
      "EndpointUrl": "opc.tcp://192.168.178.4:49320/",
      "NodeId": "ns=2;s=Boiler\\234754a-c63-b9601",
      "MonitoredItem": {
         "ApplicationUri": "urn:mySecondOPCServer"
      },
      "Value": {
         "Value": 974,
         "SourceTimestamp": "2020-11-30T07:21:32.2625062Z",
         "StatusCode": 0,
         "Status": "Good"
      }
   }
]
```

**Warning: The `Samples` format changed over time**

More detailed information about the supported message formats can be found [here](./messageformats.md)

## OPC UA Certificates management

OPC Publisher connects to OPC UA servers built into machines or industrial systems via OPC UA client/server. There is an OPC UA client built into the OPC Publisher Edge module. OPC UA Client/server uses an OPC UA Secure Channel to secure this connection. The OPC UA Secure Channel in turn uses X.509 certificates to establish *trust* between the client and the server. This is done through *mutual* authentication, i.e. the certificates must be "accepted" (or trusted) by both the client and the server.

To simplify setup, the OPC Publisher Edge module has a setting to automatically trust all *untrusted* server certificates ("--aa").
Please note that this does not mean OPC Publisher will accept any certificate presented. If Certificates are malformed or if certificates chains cannot be validated the certificate is considered broken (and not untrusted) and will be rejected as per OPC Foundation Security guidelines. In particular if a server does not provide a full chain it should be configured to do so, or the entire chain must be pre-provisioned in the OPC Publishers `PKI` folder structure.

By default, the OPC Publisher module will create a self signed x509 certificate with a 1 year expiration. This default, self signed cert includes the Subject Microsoft.Azure.IIoT. This certificate is fine as a demonstration, but for real applications customers may want to [use their own certificate](#use-custom-opc-ua-application-instance-certificate-in-opc-publisher).

The biggest hurdle most OT admins need to overcome when deploying OPC Publisher is to configure the OPC UA server (equipment) to accept this OPC Publisher X.509 certificate (the other side of mutual trust). There is usually a configuration tool that comes with the built-in OPC UA server where certificates can be trusted. For example for KepServerEx, configure the trusted Client certificate as discussed [here]( https://www.kepware.com/getattachment/ccefc1a5-9b13-41e6-99d9-2b00cc85373e/opc-ua-client-server-easy-guide.pdf).

To use the [OPC PLC Server Simulator](https://docs.microsoft.com/en-us/samples/azure-samples/iot-edge-opc-plc/azure-iot-sample-opc-ua-server/), be sure to include the `–-aa` switch or copy the server.der to the pki.
The pki path can be configured using the `PkiRootPath` command line argument.

### Use custom OPC UA application instance certificate in OPC Publisher

By default, the OPC Publisher module will create a self signed x509 certificate with a 1 year expiration. This default, self signed cert includes the Subject Microsoft.Azure.IIoT. This certificate is fine as a demonstration, but for real applications customers may want to use their own certificate.
One can enable use of CA-signed app certs for OPC Publisher using env variables in both orchestrated and standalone modes.

Besides the `ApplicationCertificateSubjectName`, the `ApplicationName` should be provided as well and needs to be the same value as we have in CN field of the `ApplicationCertificateSubjectName` like in the example below.

`ApplicationCertificateSubjectName="CN=TEST-PUBLISHER,OU=Windows2019,OU=\"Test OU\",DC=microsoft,DC=com"`

`ApplicationName ="TEST-PUBLISHER"`

## OPC UA stack

The OPC UA .NET Standard reference stack of the OPC Foundation (contributed by Microsoft) is used for OPC UA secure communications by the Industrial IoT platform. Modules and services consume the NuGet package redistributable licensed by the OPC Foundation. The open source for the reference implementation is provided by the OPC Foundation on GitHub in [this public repository](https://github.com/OPCFoundation/UA-.NETStandard).

## Performance and Memory Tuning OPC Publisher

In production setups, network performance requirements (throughput and latency) and memory resources must be considered. OPC Publisher exposes the following command line parameters to help meet these requirements:

- Message queue capacity (`om` since version 2.7)
- IoT Hub send interval (`si`)

The `om` parameter controls the upper limit of the capacity of the internal message queue. This queue buffers all messages before they're sent to IoT Hub. The default size of the queue is 4000 IoT Hub messages (for example: if the setting for the IoT Hub message size is 256 KB, the size of the queue will be up to 1 GB). If OPC Publisher isn't able to send messages to IoT Hub fast enough, the number of items in this queue increases. In this case, one or both of the following can be done to mitigate:

- Decrease the IoT Hub send interval (`si`)
- Use latest OPC Publisher in standalone mode
  - Use PubSub format (`--mm=PubSub`).
    - Choose the smallest message providing the information you need. E.g., instead of `--mm=PubSub` use `--mm=DataSetMessages`, or event `--mm=RawDataSets`. You can find sample messages [here](./messageformats.md).
    - If you are able to decompress messages back to json at the receiver side, use `--me=JsonGzip` or `--me=JsonReversibleGzip` encoding.
    - If you are able to decode binary network messages at the receiver side, choose `--me=Uadp` instead of `--me=Json`, `--me=JsonReversible` or a compressed form of Json
  - When Samples format (`--mm=Samples`) is required
    - Don't use FullFeaturedMessage (`--mm=FullSamples` or `--mm=Samples` with `--fm=false`). You can find a sample of full featured telemetry message [here](messageformats.md).
  - Use batching (`--bs=600`) in combination with batch publishing interval (`--si=20`).
  - Increase Monitored Items Queue capacity (e.g., `--mq=10`)
  - Don't use "fetch display name" (`--fd=false`)
- General recommendations
  - Try to use less different publishing intervals, rather aim to use the same for all nodes.
  - Experiment with the command line and configuration. E.g., depending on the IoT Hub connectivity it seems to be better to have fewer messages with more OPC UA value changes in it (check OPC Publisher logs) but it could also be better to have more messages with fewer OPC UA value changes, this is specific to every application.

If the queue keeps growing even though the parameters have been adjusted, eventually the maximum queue capacity will be reached and messages will be lost. This is because all parameters have physical limits and the Internet connection between OPC Publisher and IoT Hub isn't fast enough for the number of messages that must be sent in a given scenario. In that case, only setting up several, parallel OPC Publishers will help. The `om` parameter also has the biggest impact on the memory consumption by OPC Publisher.

It must be noted that IoT Hub also has limits in terms of how many messages it will accept, that is, there are quotas for a given IoT Hub SKU defined [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling). If this quota is exceeded, OPC Publisher will generate an error trying to send the message to IoT Hub and the message will be lost.

The `si` or `bi` parameter forces OPC Publisher to send messages to IoT Hub at the specified interval. A message is sent either when the maximum IoT Hub message size of 256 KB of data is available (triggering the send interval to reset) or when the specified interval time has passed.

The `bs` parameter enables batching of incoming OPC UA data change messages. When used without batching interval (`bi`), a message is sent to IoT Hub only once OPC Publisher receives specified number of incoming messages. That is why it is recommended to use batching together with batching interval to achieve consistent message delivery cadence to IoT Hub.

The `ms` parameter enables batching of messages sent to IoT Hub. In most network setups, the latency of sending a single message to IoT Hub is high, compared to the time it takes to transmit the payload. This is due to Quality of Service (QoS) requirements, since messages are acknowledged only once they've been processed by IoT Hub). Therefore, if a delay for the data to arrive at IoT Hub is acceptable, OPC Publisher should be configured to use the maximal message size of 256 KB by setting the `ms` parameter to 0. It's also the most cost-effective way to use OPC Publisher.

The metric `monitored item notifications enqueue failure`  in OPC Publisher version 2.5 and below and `messages lost` in OPC Publisher version 2.7 shows how many messages were lost.

When both `si` and `ms` parameters are set to 0, OPC Publisher sends a message to IoT Hub as soon as data is available. This results in an average IoT Hub message size of just over 200 bytes. However, the advantage of this configuration is that OPC Publisher sends the data from the connected asset without delay. The number of lost messages will be high for use cases where a large amount of data must be published and hence this isn't recommended for these scenarios.
