[Home](../../readme.md)

# Microsoft OPC Publisher - Standalone Mode

OPC Publisher is a module that runs on [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) and bridges the gap between industrial assets and the Microsoft Azure cloud. It connects to OPC UA server systems and publishes telemetry data to [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub/) in various formats, including IEC62541 OPC UA PubSub standard format (*not supported in versions < 2.7.x*).

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
        "PkiRootPath=/mount/pki",
        "--pf=/mount/published_nodes.json",
        "--lf=/mount/publisher.log",
        "--mm=PubSub",
        "--me=Json",
        "--fm=true",
        "--fd=false",
        "--bs=100",
        "--di=20",
        "--sc=1",
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

With these options specified, OPC Publisher will read the configuration file `./published_nodes.json`. The OPC Publisher's working directory is set to `/mount` at startup and thus OPC Publisher will read the file `/mount/publishednodes.json` inside its container.
OPC Publisher's log file will be written to `/mount` and the `CertificateStores` directory (used for OPC UA certificates) will also be created in this directory. To make these files available in the IoT Edge host file system, the container configuration requires a bind mount volume. The **Mounts** section will  map the directory `/mount` to the host directory `/opcpublisher` (which will be created by the IoT Edge runtime if it doesn't exist).
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

### Telemetry event Configuration via Configuration File

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

Another example configuration files are provided via [`publishednodes_2.5.json`](publishednodes_2.5.json?raw=1)
and [`publishednodes_2.8.json`](publishednodes_2.8.json?raw=1) files.

The configuration file syntax has been enhanced over time. OPC Publisher read old formats and converts them into the current format when persisting the configuration. OPC Publisher regularly persists the configuration file.
When OPC Publisher reads the file, it's validated against the [reference schema](https://raw.githubusercontent.com/Azure/Industrial-IoT/main/modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src/Schemas/publishednodesschema.json). Refer to the [OPC Publisher manual](https://github.com/Azure/Industrial-IoT/blob/main/docs/manual/readme.md) for schema validation details.

OPC UA optimizes network bandwidth by only sending changes to OPC Publisher when the data item's value has changed. Some use cases require to publish data values in constant intervals. OPC Publisher supports a "heartbeat" for every configured telemetry event that can be enabled by specifying the `HeartbeatInterval` key in the data item's configuration. The interval is specified in seconds:

``` json
 "HeartbeatInterval": 3600,
```

<!--- ToDo: Bring back once SkipFirst mechanism is implemented.

OPC UA sends the current data value when OPC Publisher connects to the OPC UA server. To prevent publishing this telemetry on startup to IoT Hub, the `SkipFirst` key can be additionally specified in the data item's configuration:

``` json
 "SkipFirst": true,
```

-->

### Configuration via Command Line Arguments

There are several command line arguments that can be used to configure global settings for OPC Publisher. They're described [in a separate document](publisher-commandline.md).

### Configuration via IoT Hub Direct methods

Up to OPC Publisher 2.5.x configuration of telemetry events was possible via direct methods. Those direct methods have been removed in higher versions.
Starting with version 2.8.2 direct methods are available again.

These direct methods are documented in a separate [document](publisher-directmethods.md).

Migration of applications, which used direct methods from version 2.5.x to versions 2.8.2 or above, check the [migration path](publisher-migrationpath.md) documentation.

### Configuration via the built-in OPC UA Server Interface

**Please note: This feature right now is only available in version 2.5 and below.**

OPC Publisher has a built-in OPC UA server, running on port 62222. It implements three OPC UA methods:

- PublishNode
- UnpublishNode
- GetPublishedNodes

This interface can be accessed using an OPC UA client application, for example [UA Expert](https://www.unified-automation.com/products/development-tools/uaexpert.html).

### Configuration via Cloud-based, Companion REST Microservice

**Please note: This feature is not available in version 2.5.x.**

A cloud-based, companion microservice with a REST interface is described and available [here](https://github.com/Azure/Industrial-IoT/blob/main/docs/services/publisher.md). It can be used to configure OPC Publisher via an OpenAPI-compatible interface, for example through Swagger.

## OPC Publisher Telemetry Format

OPC Publisher version 2.6 and above supports standardized OPC UA PubSub JSON format as specified in [part 14 of the OPC UA specification](https://opcfoundation.org/developer-tools/specifications-unified-architecture/part-14-pubsub/):

``` json
{
  "MessageId": "18",
  "MessageType": "ua-data",
  "PublisherId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "DataSetClassId": "78c4e91c-82cb-444e-a8e0-6bbacc9a946d",
  "Messages": [
    {
      "DataSetWriterId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
      "SequenceNumber": 18,
      "MetaDataVersion": {
        "MajorVersion": 1,
        "MinorVersion": 1
    },
    "Timestamp": "2020-03-24T23:30:56.9597112Z",
    "Status": null,
    "Payload": {
      "http://test.org/UA/Data/#i=10845": {
        "Value": 99,
          "SourceTimestamp": "2020-03-24T23:30:55.9891469Z",
          "ServerTimestamp": "2020-03-24T23:30:55.9891469Z"
        },
        "http://test.org/UA/Data/#i=10846": {
          "Value": 251,
          "SourceTimestamp": "2020-03-24T23:30:55.9891469Z",
          "ServerTimestamp": "2020-03-24T23:30:55.9891469Z"
        }
      }
    }
  ]
}
```

All versions of OPC Publisher support a non-standardized, simple JSON telemetry format, which is compatible with [Azure Time Series Insights](https://azure.microsoft.com/services/time-series-insights/):

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

### Configuration of the simple JSON telemetry format via Separate Configuration File

**Please note: This feature is only available in version 2.5 and below of OPC Publisher.**

OPC Publisher allows filtering the parts of the non-standardized, simple telemetry format via a separate configuration file, which can be specified via the `tc` command line option. If no configuration file is specified, the full JSON telemetry format is sent to IoT Hub. The format of the separate telemetry configuration file is described [here](publisher-telemetryformat.md).

### Persisting OPC Publisher Configuration

To ensure operation of OPC Publisher over restarts, it's required to map configuration files to the host file system. The mapping can be achieved via the "Container Create Option" in the Azure portal. The configuration files are:

- the file system based directory store
- the telemetry configuration file

In version 2.6 and above, username and password are stored in plain text in the configuration file. It must be ensured that the configuration file is protected by the file system access control of the host file system. The same must be ensured for the file system based certificate store, since it contains the private certificate and private key of OPC Publisher.

### Use custom OPC UA application instance certificate in OPC Publisher

By default, the OPC Publisher module will create a self signed x509 certificate with a 1 year expiration. This default, self signed cert includes the Subject Microsoft.Azure.IIoT. This certificate is fine as a demonstration, but for real applications customers may want to use their own certificate.
One can enable use of CA-signed app certs for OPC Publisher using env variables in both orchestrated and standalone modes.

Besides the `ApplicationCertificateSubjectName`, the `ApplicationName` should be provided as well and needs to be the same value as we have in CN field of the ApplicationCertificateSubjectName like in the example below.
 
`ApplicationCertificateSubjectName="CN=TEST-PUBLISHER,OU=Windows2019,OU=\"Test OU\",DC=microsoft,DC=com"`

`ApplicationName ="TEST-PUBLISHER"`

## Performance and Memory Tuning OPC Publisher

In production setups, network performance requirements (throughput and latency) and memory resources must be considered. OPC Publisher exposes the following command line parameters to help meet these requirements:

- Message queue capacity (`mq` for version 2.5 and below, not available in version 2.6, `om` for version 2.7)
- IoT Hub send interval (`si`)

The `mq/om` parameter controls the upper limit of the capacity of the internal message queue. This queue buffers all messages before they're sent to IoT Hub. The default size of the queue is up to 2 MB for OPC Publisher version 2.5 and below and 4000 IoT Hub messages for version 2.7 (for example: if the setting for the IoT Hub message size is 256 KB, the size of the queue will be up to 1 GB). If OPC Publisher isn't able to send messages to IoT Hub fast enough, the number of items in this queue increases. In this case, one or both of the following can be done to mitigate:

- Decrease the IoT Hub send interval (`si`)
- Use OPC Publisher > 2.6 in standalone mode
  - Use PubSub format (`--mm=PubSub`)
  - When Samples format (`--mm=Samples`) is required
    - Don't use FullFeaturedMessage (`--fm=false`). You can find a sample of full featured telemetry message [here](../dev-guides/telemetry-messages-format.md).
  - Use batching (`--bs=600`) in combination with batch interval (`--si=20`)
    - Batching is also useable with PubSub but current implementation of PubSub batches automatically based on Publishing Interval of OPC UA nodes. When most nodes are using the same publishing interval it isn't necessary.
  - Increase Monitored Items Queue capacity (`--mq=25000`)
  - Don't use "fetch display name" (`--fd=false`)
- General recommendations
  - Try to use less different publishing intervals
  - Experiment with the numbers, depending on the IoT Hub connectivity it seems to be better to have fewer messages with more OPC UA value changes in it (check OPC Publisher logs) but it could also be better to have more messages with fewer OPC UA value changes, this is specific to every factory

If the queue keeps growing even though the parameters have been adjusted, eventually the maximum queue capacity will be reached and messages will be lost. This is because all parameters have physical limits and the Internet connection between OPC Publisher and IoT Hub isn't fast enough for the number of messages that must be sent in a given scenario. In that case, only setting up several, parallel OPC Publishers will help. The `mq/om` parameter also has the biggest impact on the memory consumption by OPC Publisher.

It must be noted that IoT Hub also has limits in terms of how many messages it will accept, that is, there are quotas for a given IoT Hub SKU defined [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling). If this quota is exceeded, OPC Publisher will generate an error trying to send the message to IoT Hub and the message will be lost.

The `si` parameter forces OPC Publisher to send messages to IoT Hub at the specified interval. A message is sent either when the maximum IoT Hub message size of 256 KB of data is available (triggering the send interval to reset) or when the specified interval time has passed.

The `bs` parameter enables batching of incoming OPC UA data change messages. When used without batching interval (`si`), a message is sent to IoT Hub only once OPC Publisher receives specified number of incoming messages. That is why it is recommended to use batching together with batching interval to achieve consistent message delivery cadence to IoT Hub.

The `ms` parameter enables batching of messages sent to IoT Hub. In most network setups, the latency of sending a single message to IoT Hub is high, compared to the time it takes to transmit the payload. This is due to Quality of Service (QoS) requirements, since messages are acknowledged only once they've been processed by IoT Hub). Therefore, if a delay for the data to arrive at IoT Hub is acceptable, OPC Publisher should be configured to use the maximal message size of 256 KB by setting the `ms` parameter to 0. It's also the most cost-effective way to use OPC Publisher.

The metric `monitored item notifications enqueue failure`  in OPC Publisher version 2.5 and below and `messages lost` in OPC Publisher version 2.7 shows how many messages were lost.

When both `si` and `ms` parameters are set to 0, OPC Publisher sends a message to IoT Hub as soon as data is available. This results in an average IoT Hub message size of just over 200 bytes. However, the advantage of this configuration is that OPC Publisher sends the data from the connected asset without delay. The number of lost messages will be high for use cases where a large amount of data must be published and hence this isn't recommended for these scenarios.

To measure the performance of OPC Publisher, the `di` parameter can be used to print performance metrics to the log in the interval specified (in seconds).

## Local metrics dashboard for OPC Publisher

To learn more about how to create a local metrics dashboard for OPC Publisher V2.7, refer to the tutorial [here](../tutorials/tut-publisher-local-metrics-dashboard/MetricsDashboard.md).

## Next steps

- [Learn how to deploy OPC Publisher and Twin Modules](../deploy/howto-install-iot-edge.md)
- [Learn about the OPC Publisher Microservice](../services/publisher.md)
- [Learn how to configure the OPC Publisher in EFLOW (Azure IoT Edge for Linux on Windows)](https://github.com/Azure/iotedge-eflow/blob/main/samples/networking/multiple-nics/README.md)
