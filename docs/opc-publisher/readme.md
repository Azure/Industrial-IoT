[Home](../readme.md)

# Microsoft OPC Publisher

OPC Publisher is a module that runs on [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) and bridges the gap between industrial assets and the Microsoft Azure cloud. It connects to OPC UA server systems and publishes telemetry data to [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub/) in various formats, including IEC62541 OPC UA PubSub standard format (*not supported in versions < 2.7.x*).

In this document you find information to

* [Getting started](#getting-started)
* [Configue OPC Publisher](#configuring-opc-publisher)
  * [Configure secure access to OPC UA server endpoints](#configuring-security)
  * [Configure Value Change subscriptions](#configuration-via-configuration-file)
  * [Configure Event subscriptions](#configuring-event-subscriptions)
  * [Command Line Options](./ommandline.md) 
  * [Direct method based Configuration](./directmethods.md)
* [Calling services inside an OPC UA servers](#opc-ua-client-opc-twin)
* [Configure mutual trust between OPC Publisher and the OPC UA server](#opc-ua-certificates-management)
* [Understand message formats supported by OPC Publisher](#opc-publisher-telemetry-formats)
  * [More Details](./messageformats.md)
* [Tune OPC Publisher performance](#performance-and-memory-tuning-opc-publisher)
* [Monitor and diagnose OPC Publisher](#diagnostics)

## Getting Started

Use our released containers for OPC Publisher available in the Microsoft Container Registry, rather than building from sources. The easiest way to deploy OPC Publisher is through the [Azure Marketplace](https://azuremarketplace.microsoft.com/marketplace/apps/microsoft_iot.iotedge-opc-publisher).

Select the "Get It Now" button to log into the [Azure portal](https://portal.azure.com) and deploy OPC Publisher. The following steps are required:

1. Pick the Azure subscription to use. If no Azure subscription is available, one must be created.
2. Pick the IoT Hub the OPC Publisher is supposed to send data to. If no IoT Hub is available, one must be created.
3. Pick the IoT Edge device OPC Publisher is supposed to run on. If no IoT Edge device exists, one must be created).
4. Select "Create". The "Set modules on Device" page for the selected IoT Edge device opens.
5. Select on "OPCPublisher" to open the OPC Publisher's "Update IoT Edge Module" page and then select "Container Create Options".
6. Validate "container create options" based on your usage of OPC Publisher. For more information, see next section.

### Specifying Container Create Options in the Azure portal

Container create options can be specified in the "Update IoT Edge Module" page of OPC Publisher. These create options must be in JSON format. The OPC Publisher command line arguments can be specified via the "Cmd" key. Here an example for a configuration on a Linux host system:

``` json
{
    "Hostname": "opcpublisher",
    "Cmd": [
        "-c",
        "--PkiRootPath=/mount/pki",
        "--pf=/mount/published_nodes.json",
        "--lf=/mount/publisher.log",
        "--mm=PubSub",
        "--me=Json",
        "--fd=false",
        "--bs=100",
        "--bi=1000",
        "--di=20"
        "--aa"
    ],
    "HostConfig": {
        "Mounts": [
            {
                "Type": "bind",
                "Source": "/opcpublisher",
                "Target": "/mount"
            }
        ],
        "CapDrop": [
            "CHOWN",
            "SETUID"
        ]
    }
}
```

With these options specified, OPC Publisher will read the configuration file `./published_nodes.json`. The OPC Publisher's working directory is set to `/mount` at startup and thus OPC Publisher will read the file `/mount/published_nodes.json` inside its container.
OPC Publisher's log file will be written to `/mount` and the `CertificateStores` directory (used for OPC UA certificates) will also be created in this directory. To make these files available in the IoT Edge host file system, the container configuration requires a bind mount volume. The **Mounts** section will  map the directory `/mount` to the host directory `/opcpublisher`. Please note that `/opcpublisher` directory should be present on host file system, otherwise OPC Publisher will fail to start.
The `CapDrop` option will drop the CHOWN (user can’t makes arbitrary changes to file UIDs and GIDs) and SETUID (user can’t makes arbitrary manipulations of process UIDs) capabilities for security reason.

**Without this bind mount volume, all OPC Publisher configuration files will be lost when the container is restarted.**

A connection to an OPC UA server using its hostname without a DNS server configured on the network can be achieved by adding an `ExtraHosts` entry to the `HostConfig` section:

``` json
"HostConfig": {
    "ExtraHosts": [
        "opctestsvr:192.168.178.26"
    ]
}
```

## Configuring OPC Publisher

OPC Publisher has several interfaces that can be used to configure it.  

* [Configuration via configuration file](#configuration-via-configuration-file)
* [Command Line options configuration](./ommandline.md)
* [Direct method runtime configuration](./directmethods.md)
* [How to migrate from previous versions of OPC Publisher](./migrationpath.md)

### Configuring Security

IoT Edge provides OPC Publisher with its security configuration for accessing IoT Hub automatically. OPC Publisher can also run as a standalone Docker container by specifying a device connection string for accessing IoT Hub via the `dc` command line parameter. A device for IoT Hub can be created and its connection string retrieved through the Azure portal.

OPC UA does use X.509 certificates for:

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

When OPC Publisher reads the file, it's validated against the [reference schema](https://raw.githubusercontent.com/Azure/Industrial-IoT/main/modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src/Schemas/publishednodesschema.json). Refer to the [OPC Publisher manual](https://github.com/Azure/Industrial-IoT/blob/main/docs/manual/readme.md) for schema validation details.

OPC UA optimizes network bandwidth by only sending changes to OPC Publisher when the data item's value has changed. Some use cases require to publish data values in constant intervals. OPC Publisher supports a "heartbeat" for every configured telemetry event that can be enabled by specifying the `HeartbeatInterval` key in the data item's configuration. The interval is specified in seconds:

``` json
 "HeartbeatInterval": 3600,
```

OPC UA sends the current data value when OPC Publisher connects to the OPC UA server. To prevent publishing this telemetry on startup to IoT Hub, the `SkipFirst` key can be additionally specified in the data item's configuration:

``` json
 "SkipFirst": true,
```

### Configuring event subscriptions

OPC Publisher supports two types of event filter configurations you can specify:

* [Simple event filter](#simple-event-filter) configuration mode, where you specify the source node and the event type you want to filter on and then the OPC Publisher constructs the select and where clauses for you.
* [Advanced event filter](#advanced-event-filter) configuration mode where you explicitly specify the select and where clauses.

In the configuration file you can specify how many event configurations as you like and you can also combine events and data nodes for a single endpoint.

In addition you can configure optional [Condition](#condition-handling-options) reporting where OPC Publisher reports retaind conditions at a configured time periodic rate in seconds.

#### Simple event filter

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

* It looks at the TypeDefinitionId of the event type to monitor and traverses the inheritance tree for that event type, collecting all fields. Then it constructs a select clause with all the fields it finds.
* It creates a where clause that is OfType(TypeDefinitionId) to filter the events to just the selected event type.

#### Advanced event filter configuration

To configure an advanced event filter you have to specify a full event filter which at minimum consists of three things:

* The source node you want to receive events for (in the example below again the server node which has node id `i=2253`).
* A select clause specifying which fields should be in the reported event. This can include a data set class field id that is then used as identifier in the dataset metadata for the dataset class.
* A where clause specifying the filter AST.

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

#### Condition handling options

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

* `UpdateInterval` - the interval, in seconds, which a message is sent if anything has been updated during this interval.
* `SnapshotInterval` - the interval, in seconds, that triggers a message to be sent regardless of if there has been an update or not.

One or both of these must be set for condition handling to be in effect. You can use the condition handling configuration regardless if you are using advanced or simple event filters. If you specify the`ConditionHandling` option property without an `EventFilter` property it is ignored, as condition handling has no effect for data change subscriptions.

Conditions are sent as `ua-condition` data set messages. This is a message type not part of the official standard but allows seperating condition snapshots from regular `ua-event` data set messages.

### Persisting OPC Publisher Configuration

To ensure operation of OPC Publisher over restarts, it's required to map configuration files to the host file system. The mapping can be achieved via the "Container Create Option" in the Azure portal. The configuration files are:

- the file system based directory store
- the telemetry configuration file

In version 2.6 and above, username and password are stored in plain text in the configuration file. It must be ensured that the configuration file is protected by the file system access control of the host file system. The same must be ensured for the file system based certificate store, since it contains the private certificate and private key of OPC Publisher.

## OPC UA Client (OPC Twin)

The control services (formerly OPC Twin services) are provided using IoT Hub device method API (as well as Web API and MQTT based request response API).

The API enables you to write applications taht invoke OPC UA server functionality on OPC server endpoints. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service. This includes [Variant](../api/json.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response.  This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint so the OPC UA server is not overburdened with 100’s of simultaneous requests.  

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

## OPC Publisher Telemetry Formats

OPC Publisher version 2.6 and above supports standardized OPC UA PubSub network messages in JSON format as specified in [part 14 of the OPC UA specification](https://opcfoundation.org/developer-tools/specifications-unified-architecture/part-14-pubsub/).

An example OPC UA PubSub message looks as follows:

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

### Diagnostics

The OPC Publisher emits metrics through its Prometheus endpoint (`/metrics`). To learn more about how to create a local metrics dashboard for OPC Publisher V2.7, refer to the tutorial [here](../tutorials/tut-publisher-local-metrics-dashboard/MetricsDashboard.md).

To measure the performance of OPC Publisher, the `di` parameter can be used to print metrics to the log in the interval specified (in seconds).

The following table describes the actual instruments that are logged per endpoint:

| Log line item name                      | Diagnostic info property name                       | Description |
|-----------------------------------------|-------------------------------------|-------------|
| # Ingestion duration                    | ingestionDuration                   | How long the data flow inside the publisher has been executing after it was created (either from file or API) |
| # Ingress DataChanges (from OPC)        | ingressDataChanges                  | The number of OPC UA subscription notification messages with data value changes that have been received by publisher inside this data flow |
| # Ingress ValueChanges (from OPC)       | ingressValueChanges                 | The number of value changes inside the OPC UA subscription notifications processed by the data flow. |
| # Ingress EventNotifications (from OPC) | ingressEventNotifications           | The number of OPC UA subscription notification messages with events that have been received by publisher so far inside this data flow |
| # Ingress EventChanges (from OPC)       | ingressEvents                       | The number of events that were part of these OPC UA subscription notifications that were so far processed by the data flow. |
| # Ingress BatchBlock buffer size        | ingressBatchBlockBufferSize         | The number of messages awaiting encoding and sending tot he telemetry message destination inside the data flow pipeline. |
| # Encoding Block input / output size    | encodingBlockInputSize              | The number of messages awaiting encoding into the output format. |
| # Encoding Block input / output size    | encodingBlockOutputSize             | The number of messages already encoded and waiting to be sent to the telemetry message destination. |
| # Encoder Notifications processed       | encoderNotificationsProcessed       | The total number of subscription notifications processed by the encoder stage of the data flow pipeline since the pipeline started. |
| # Encoder Notifications dropped         | encoderNotificationsDropped         | The total number of subscription notifications that were dropped because they could not be encoded, e.g., due to their size being to large to fit into the message. |
| # Encoder IoT Messages processed        | encoderIoTMessagesProcessed         | The total number of encoded messages produced by the encoder since the start of the pipeline. |
| # Encoder avg Notifications/Message     | encoderAvgNotificationsMessage      | The average number of subscription notifications that were presssed into a message. |
| # Encoder avg IoT Message body size     | encoderAvgIoTMessageBodySize        | The average size of the message body produced over the course of the pipeline run. |
| # Encoder avg IoT Chunk (4 Kb) usage    | encoderAvgIoTChunkUsage             | The average use of IoT Hub chunks (4k). |
| # Estimated IoT Chunks (4 KB) per day   | estimatedIoTChunksPerDay            | An estimate of how many chunks are used per day by publisher which enables correct sizing of the IoT Hub to avoid data loss due to throttling. |
| # Outgress Batch Block buffer size      | outgressBatchBlockBufferSize        | The number of messages that are waiting to be sent to all configured telemetry message destination via the message sink. |
| # Outgress input bufffer count          | outgressInputBufferCount            | The aggregated number of messages waiting in the input buffer of the configured telemetry message destination sinks. |
| # Outgress input buffer dropped         | outgressInputBufferDropped          | The aggregated number of messages that were dropped in any of the configured telemetry message destination sinks. |
| # Outgress IoT message count            | outgressIoTMessageCount             | The aggregated number of messages that were sent by all configured telemetry message destination sinks. |
|                                         | sentMessagesPerSec                  | Publisher throughput meaning the number of messages sent to the telemetry message destination (e.g., IoT Hub / Edge Hub) per second |
| # Connection retries                    | connectionRetries                   | How many times connections to the OPC UA server broke and needed to be reconnected as it pertains to the data flow. |
| # Opc endpoint connected?               | opcEndpointConnected                | Whether the pipeline is currently connected to the OPC UA server endpoint or in a reconnect attempt. |
| # Montitored Opc nodes succeeded count  | monitoredOpcNodesSucceededCount     | How many of the configured monitored items have been established successfully inside the data flow's OPC UA subscription and should be producing data. |
| # Montitored Opc nodes failed count     | monitoredOpcNodesFailedCount        | How many of the configured monitored items inside the data flow failed to be created in the subscription (the logs will provide more information). |

