[Home](../../readme.md)

# Migration path

The latest version 2.8.2 adds support for configuration via direct methods. OPC Publisher implements multiple [IoT Hub Direct Methods](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) which can be called from an application leveraging the [IoT Hub Device SDK](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks). This document provides the migration path from 2.5.x to 2.8.2.

The direct methods' request payload of version 2.8.2 is backwards compatible with the 2.5.x direct methods. The payload schema allows configuration of additional extensions introduced in the `pn.json` in the publisher 2.6.x and newer e.g. DataSetWriterGroup, DataSetWriterId, QueueSize per node, etc.

For this set of methods, the encoding is JSON and no compression or payload chunking mechanism is applied in order to ensure the backwards compatibility with the 2.5.x version of OPC Publisher module.



## Direct Methods of version 2.5.x

The following  table describes the direct methods which were available in OPC Publisher 2.5.x with request and response. The direct methods which are removed in version 2.8.2 are `GetDiagnosticLog`, `GetDiagnosticStartupLog`, `ExitApplication` and `GetInfo`.

| 2.5.x                            |                                                              |                                                              |                          |
| -------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------ |
| **MethodName**                   | **Request**                                                  | **Response**                                                 | **in 2.8.2** |
| **PublishNodes**                 | EndpointUrl, List<OpcNodes>,  UseSecurity, UserName, Password | Status, List<StatusResponse>                                 | Yes                        |
| **UnpublishNodes**               | EndpointUrl, List<OpcNodes>                                  | Status, List<StatusResponse>                                 | Yes                        |
| **UnpublishAllNodes**            | EndpointUrl                                                  | Status, List<StatusResponse>                                 | Yes                       |
| **GetConfiguredEndpoints**       | -                                                            | List<EndpointUrl>                                            | Yes                        |
| **GetConfiguredNodesOnEndpoint** | EndpointUrl                                                  | EndpointUrl, List< OpcNodeOnEndpointModel >    where OpcNodeOnEndpointModel contains:    Id ExpandedNodeId OpcSamplingInterval OpcPublishingInterval DisplayName HeartbeatInterval SkipFirst | Yes                       |
| **GetDiagnosticInfo**            | -                                                            | DiagnosticInfoMethodResponseModel                            | Yes                        |
| **GetDiagnosticLog**             | -                                                            | MissedMessageCount, LogMessageCount, List<Log>               | No                      |
| **GetDiagnosticStartupLog**      | -                                                            | MissedMessageCount, LogMessageCount, List<Log>               | No                      |
| **ExitApplication**              | SecondsTillExit (optional)                                   | StatusCode, List<StatusResponse>                             | No                      |
| **GetInfo**                      | -                                                            | GetInfoMethodResponseModel >>    VersionMajor  VersionMinor  VersionPatch  SemanticVersion  InformationalVersion OS  OSArchitecture  FrameworkDescription | No                      |

A [sample configuration application](https://github.com/Azure-Samples/iot-edge-opc-publisher-nodeconfiguration) as well as an [sample application for reading diagnostic information](https://github.com/Azure-Samples/iot-edge-opc-publisher-diagnostics) are provided from OPC Publisher open-source, leveraging this interface.

_Hint: The samples applications are not actively maintained and may be outdated._

## Direct Methods of version 2.8.2

There are some direct methods which are inherited from 2.5.x in addition to new ones.
Inherited direct methods from 2.5.x:
  - PublishNodes_V1
  - UnpublishNodes_V1
  - UnpublishAllNodes_V1
  - GetConfiguredEndpoints_V1
  - GetConfiguredNodesOnEndpoints_V1
  - GetDiagnosticInfo_V1

New direct methods:
  - AddOrUpdateEndpoints_V1

Note: `_V1` suffix is not required.

### Terminologies

The definitions of the important terms used are described below:

- **DataSet** is a group of NodeIds within an OPC UA Server to be published with the same publishing interval.

- **DataSetWriter** has one DataSet and contains the elements required to successfully establish a connection to the OPC UA Server.

- **DatSetGroup** is used to group several DataSetWriters for a specific OPC UA server.

### Payload Schema

The ` _V1` methods  uses the  payload schema as described below:

``` json
{ 
  "EndpointUrl": "string",
  "UseSecurity": "boolean",
  "OpcAuthenticationMode": "string",
  "UserName": "string", 
  "Password": "string",
  "DataSetWriterGroup": "string",
  "DataSetWriterId": "string",
  "DataSetPublishingInterval": "integer",
  "Tag": "string",
  "OpcNodes": 
  [
    {
      "Id": "string",
      "ExpandedNodeIdId": "string",
      "OpcSamplingInterval": "integer",
      "OpcSamplingIntervalTimespan": "string",
      "OpcPublishingInterval": "integer",
      "OpcPublishingIntervalTimespan": "string",
      "DataSetFieldId ": "string",
      "DisplayName": "string",
      "HeartbeatInterval": "integer",
      "HeartbeatIntervalTimespan": "string",
      "SkipFirst": "boolean",
      "QueueSize": "integer"
    }
  ]
}
```



Method call's request attributes are as follows:

| Attribute                        | Mandatory | Type    | Default                   | Description                                                                                  |
|----------------------------------|-----------|---------|---------------------------|----------------------------------------------------------------------------------------------|
| `EndpointUrl`                    | yes       | String  | N/A                       | The OPC UA Server’s endpoint URL                                                             |
| `UseSecurity`                    | no        | Boolean | false                     | Desired opc session security mode                                                            |
| `OpcAuthenticationMode`          | no        | Enum    | Anonymous                 | Enum to specify the session authentication.<br>Options: Anonymous, UserName                  |
| `UserName`                       | no        | String  | null                      | The username for the session authentication<br>Mandatory if OpcAuthentication mode is UserName|
| `Password`                       | no        | String  | null                      | The password for the session authentication<br>Mandatory if OpcAuthentication mode is UserName|
| `DataSetWriterGroup`             | no        | String  | EndpointUrl           | The writer group collecting datasets defined for a certain <br>endpoint uniquely identified by the above attributes. <br>This is used to identify the session opened into the <br>server. The default value consists of the EndpointUrl string, <br>followed by a deterministic hash composed of the <br>EndpointUrl, security and authentication attributes.|
| `DataSetWriterId`                | no        | String  | DataSetPublishingInterval | The unique identifier for a data set writer used to collect <br>opc nodes to be semantically grouped and published with <br>a same publishing interval. <br>When not specified a string representing the common <br>publishing interval of the nodes in the data set collection. <br>This the DataSetWriterId  uniquely identifies a data set <br>within a DataSetGroup. The unicity is determined <br>using the provided DataSetWriterId and the publishing <br>interval of the grouped OpcNodes.  An individual <br>subscription is created for each DataSetWriterId|
| `DataSetPublishingInterval`      | no        | Integer | false                     | The publishing interval used for a grouped set of nodes <br>under a certain DataSetWriter. When defined it <br>overrides the OpcPublishingInterval value in the OpcNodes <br>if grouped underneath a DataSetWriter. |
| `Tag`                            | no        | String  | empty                     | TODO                                                                                         |
| `OpcNodes`                       | yes      | OpcNode | empty                     | The DataSet collection grouping the nodes to be published for <br>the specific DataSetWriter defined above. |



*Note*: **OpcNodes** field is mandatory for PublishNodes_V1, UnpublishNodes_V1 and AddOrUpdateEndpoints_V1. It should not be specified for the rest of the direct methods.

OpcNode attributes are as follows:

| Attribute                        | Mandatory | Type    | Default                   | Description                                                                                  |
|----------------------------------|-----------|---------|---------------------------|----------------------------------------------------------------------------------------------|
| `Id`                             | Yes     | String  | N/A                       | The node Id to be published in the opc ua server. <br>Can be specified as NodeId or Expanded NodeId <br>in as per opc ua spec, or as ExpandedNodeId IIoT format <br>{NamespaceUi}#{NodeIdentifier}.   |
| `ExpandedNodeIdId`               | No        | String  | null                      | Backwards compatibility form for Id attribute. Must be <br>specified as expanded node Id as per OPC UA Spec.    |
| `OpcSamplingInterval`            | No        | Integer | 1000                      | The sampling interval for the monitored item to be <br>published. Value expressed in milliseconds.     |
| `OpcSamplingIntervalTimespan`    | No        | String  | null                      | The sampling interval for the monitored item to be <br>published. Value expressed in Timespan <br>string({d.hh:mm:dd.fff}). <br>Ignored when OpcSamplingInterval is present.  |
| `OpcPublishingInterval`          | No        | Integer | 1000                      | The publishing interval for the monitored item to be <br>published. Value expressed in milliseconds. <br>This value will be overwritten when a publishing interval <br>is explicitly defined in the DataSetWriter owning this OpcNode.  |
| `OpcPublishingIntervalTimespan`  | No        | String  | null                      | The publishing interval for the monitored item to be <br>published. Value expressed in Timespan <br>string({d.hh:mm:dd.fff}). <br>This value will be overwritten when a publishing interval <br>is explicitly defined in the DataSetWriter owning this OpcNode. <br>Ignored when OpcPublishingInterval is present  |
| `DataSetFieldId`                 | No        | String  | null                      | A user defined tag used to identify the Field in the <br>DataSet telemetry message when publisher runs in <br>PubSub message mode.   |
| `DisplayName`                    | No        | String  | null                      | A user defined tag to be added to the telemetry message <br>when publisher runs in Samples message mode.    |
| `HeartbeatInterval`              | No        | Integer |    0                      | The interval used for the node to publish a value (a publisher <br>cached one) even if the value has not been <br>changed at the source. This value is represented in seconds. <br>0 means the heartbeat mechanism is disabled. <br>This value is ignored when HeartbeatIntervalTimespan is present      |
| `HeartbeatIntervalTimespan`      | No        | String  | null                      | The interval used for the node to publish a value (a publisher <br>cached one) even if the value has not been <br>changed at the source. This value is represented in seconds. <br>Value expressed in Timespan string({d.hh:mm:dd.fff}).     |
| `SkipFirst`                      | No        | Boolean | false                     | Instructs the publisher not to add to telemetry the<br> Initial DataChange (after subscription activation) for this OpcNode.     |
| `QueueSize`                      | No        | Integer | 1                         | The desired QueueSize for the monitored item to be published.     |

*Note*: **Id** field may be omitted when ExpandedNodeIdId is present.

The direct methods definitions of version 2.8.2 with examples and exceptions are provided in [this](publisher-directmethods.md) document.

## Next steps

* [Learn how to deploy OPC Publisher and Twin Modules](../deploy/howto-install-iot-edge.md)
* [Learn about the OPC Publisher Microservice](../services/publisher.md)
