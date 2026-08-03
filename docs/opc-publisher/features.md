# Features

[Home](./readme.md)

The following table shows the supported features of OPC Publisher and planned feature additions. Preview features are supported through GitHub issues only, experimental features will become preview or fully supported features if you request so through GitHub issues or by contacting us. If you would like to see additional features added, please open a feature request.

Rows marked `Removed in 3.0` describe features that earlier versions supported and 3.0 no longer does. Where an option controlled such a feature it is accepted and ignored rather than rejected, so an existing command line or published nodes file still starts - the exception is the `Samples` and `FullSamples` messaging modes, which fail with an error naming their replacement because they changed the message format on the wire. [The migration notes](../../MIGRATION-2.0-WIP.md) explain each removal and what to use instead.

| Feature | Sub Feature | 2.8 | 2.9 | 3.0 | Feature state |
| ------- | ----------- |---- | --- | --- | ------------- |
| Uses latest .net reference stack ||X|X|X||
| .net Version || .net 6 | .net 10 | .net 10 ||
| Secure channel transport and configuration ||X|X|X||
| OPC UA HTTP transport and configuration ||-|-|-|#1997|
| Secure channel over web socket transport and configuration ||-|-|-|#1997|
| Secure channel certificate management [API](./api.md#certificates) ||||||
| | Client Cert |-|X|X||
| | Using EST |-|-|-||
| | GDS Pull from GDS server |-|-|-|#2081|
| | GDS Server Push to OPC Publisher |-|-|-||
| Session-reconnect handling across connection loss ||X|X|X||
| | Using official .net stack implementation |-|X|X||
| [Reverse Connect](./readme.md#using-opc-ua-reverse-connect) ||-|X|X|Preview|
| User authentication ||||||
| | Username / Password authentication |X|X|X||
| | X509 based user authentication ||X|X||
| | Token based user authentication |-|-|-||
| Get Endpoint and Server information [API](./api.md#getservercapabilities) ||-|X|X|Preview|
| Connect and Disconnect [API](./api.md) ||-|X|X|Preview|
| Test connection [API](./api.md#testconnection) ||-|X|X|Preview|
| Browse [API](./api.md#browse) ||-|X|X||
| | Browse first/next |-|X|X||
| | RegEx Browse filter |-|-|-||
| | Streaming browse "Fast browsing" / Partial node set export |-|X|X|Preview|
| | Publish model change feed change events |-|X|X|Experimental|
| Translate browse path [API](./api.md) ||-|X|X||
| Read [API](./api.md#valueread) ||||||
| | Read Value |-|X|X||
| | Read other attributes of nodes |-|X|X||
| | Get instance metadata |-|X|X||
| Write [API](./api.md#valuewrite)||||||
| | Write Value |-|X|X||
| | Write other attributes of nodes |-|X|X||
| Method Call [API](./api.md#methodcall) ||-|X|X||
| HDA [API](./api.md#history) for processed, modified, at-times, events time series data ||||||
| | Read |-|X|X|Preview|
| | Streaming read |-|X|X|Experimental|
| | Update |-|X|X|Preview|
| | Upsert |-|X|X|Preview|
| | Delete |-|X|X|Preview|
| File Transfer [API](./api.md#filesystem) ([Part 20](https://reference.opcfoundation.org/Core/Part20/v105/docs/)) ||||||
| | List file systems |-|X|X|Experimental|
| | Browse file systems on server |-|X|X|Experimental|
| | Create files and directories |-|X|X|Experimental|
| | Download files |-|X|X|Experimental|
| | Upload files to directory |-|X|X|Experimental|
| | Delete files and directories |-|X|X|Experimental|
| | Substitutable Close method |-|X|X|Experimental|
| | Temporary file transfer |-|-|-||
| Subscribe to [value changes](./readme.md#configuration-schema) ||||||
| | Value change subscriptions |X|X|X||
| | Data change filter support |-|X|X||
| | Using browse path to node |-|X|X||
| | Deadband |-|X|X||
| | Status trigger |-|X|X||
| | Set server queue size per value|-|X|X||
| | Set server queue LIFO/FIFO behavior per value|-|X|X||
| | Set queue size using publishing interval and sampling interval |-|X|X|Preview|
| | Periodic read ([cyclic read](./readme.md#sampling-and-publishing-interval-configuration))|-|X|X|Preview|
| | Heartbeat (Periodic resending of last known value) |X|X|X||
| | Configurable heartbeat behavior (LKG, LKV) ||X|X||
| | Heartbeat message timestamp source configuration ||X|X||
| Subscribe to [events](./readme.md#configuring-event-subscriptions) ||||||
| | Using browse path to event notifier |-|X|X||
| | Simple (get all events of a type from event notifier)|-|X|X||
| | Event filter (filter events on server before sending)|-|X|X||
| | Condition handling / Condition snapshots|-|X|X|Preview|
| Subscribe to nodes that are not variables or event notifiers ||||||
| | All variables under and object |-|X|X|Preview|
| | All Objects and variables of an object type |-|X|X|Preview|
| | All Variables of a variable type |-|X|X|Preview|
| Triggering ||||||
| | Using Server side triggering service (SetTriggering) |-|-|-||
| | Client side sampling of values on event |-|-|-||
| Re-evaluate subscriptions ||||||
| | Periodically |-|X|X||
| | While monitored items failed to be applied |-|X|X||
| | On data model change events |-|-|-|#1209|
| Subscription watchdog ||||||
| | When all monitored items are not reporting within an interval |-|X|X||
| | When a monitored item is not reporting within an interval |-|X|X||
| | When subscription is deleted on server |-|X|X||
| | Configure whether to reset or terminate |-|X|X||
| Registered Nodes ||||||
| | For periodic reads (registered read) |-|X|X|Preview|
| | For monitored items |-|X|X|Preview|
| | Register API call |-|-|-||
| | Unregister API call |-|-|-||
| Client-side transport queue configuration ||||||
| | Batch size and publishing interval publisher wide |X|X|X&#42;||
| | Batch size and publishing interval per group |-|X|X&#42;||
| | Load shedding |X|X|X||
| | Queue jumping / Priority messages|-|-|-||
| | Advanced overflow handling strategies|-|-|-||
| IIoT Platform 2.8 Orchestrated mode support ||X|-|-||
| 0 message loss||-|-|-||
| Transfer subscription||||||
| | On reconnect |-|X|X||
| | On startup |-|-|-||
| Re-activate session on startup (Transfer session)||||||
| Deferred Notification Acknowledgement||-|X|X|Experimental|
| Back pressure to server||-|-|-||
| Published nodes JSON [schema](./readme.md#configuration-schema) support ||||||
| | v2.5 |X|X|X||
| | v2.8 |X|X|X||
| | v2.9 |-|X|X||
| | JSON schema validation |X|-|-||
| | Bootstrapped from Azure Storage blob |-|-|-|#2284|
| API to configure and subscribe to Objects, Types and Assets ||||||
| | All variables under an object as writers |-|X|X|Experimental|
| | All variables of objects of a certain object type or subtype |-|X|X|Experimental|
| | All variables of a variable type or subtype |-|X|X|Experimental|
| | Asset configuration using Web of Things Description per [Part 10100-1](https://reference.opcfoundation.org/WoT/v100/docs/)|-|X|X|Experimental|
| | Asset admin shell support per [Part 30270](https://reference.opcfoundation.org/I4AAS/v100/docs/)|-|-|-||
| OPC UA Pub/Sub configuration API ([Part 14](https://reference.opcfoundation.org/Core/Part14/v105/docs/))||-|-|-||
| Data contextualization ||||||
| | Add Endpoint/Dataset name to message header (Routing) |X|X|X||
| | [Enrichment](./readme.md#key-frames-delta-frames-and-extension-fields) |-|X|X||
| | Transformation |-|-|-||
| | Normalization |-|-|-||
| Running as docker outside IoT Edge or K8s ||-|X|X|Experimental|
| [IoT Edge](./readme.md#install-iot-edge) deployment support ||X|X|X||
| | Fully functional in nested (ISA95) setup |-|X|X||
| | IoT Hub direct method-based configuration|X|X|X||
| | IoT Hub direct method-based API calls|-|X|X||
| | DTDL interface for API |-|-|-||
| [MQTT](./transports.md#mqtt) request response-based API and configuration ||||||
| | v5 request response |-|X|X|Preview|
| | v3.11 using IoT Hub like &rid= correlation |-|X|X|Experimental|
| [HTTP](./transports.md#built-in-http-api-server) REST command/control and configuration API ||-|X|X|Preview|
| Kafka request response-based API and configuration ||-|-|-||
| Configuration via OPC UA endpoint ||-|-|-||
| Prometheus [Metrics](./observability.md) ||||||
| | For module metrics |X|X|X||
| | Endpoint metrics |X|X|X||
| | Process data |-|-|-|Backlog|
| Periodic diagnostic output ||||||
| | To Console |X|X|X||
| | To Diagnostics Topic/Output |-|X|X||
| | As OPC UA PubSub Message |-|-|-|Backlog|
| Health and liveness probe / watchdog ||-|-|-|Backlog|
| Message and event publishing [transports](./transports.md) ||||||
| | IoT Hub |X|X|X||
| | MQTT topics |-|X|X|Preview|
| | Publishing to [Azure EventHub](./transports.md#azure-eventhub) |-|X|X|Preview|
| | Dapr Pub/Sub (Kafka, Redis, etc.) |-|X|X|Experimental|
| | Publishing to a Web hook|-|X|X|Experimental|
| | Dump messages and schemas to zip files in file system |-|X|X|Experimental|
| | Null sink|-|X|X|Experimental|
| Multiple cloud transports enabled in parallel ||-|X|X|Preview|
| Select desired transport per writer group ||-|X|X|Preview|
| Cloud Events support ||-|-|-|via Dapr|
| OPC UA Pub Sub [message content profiles](./messageformats.md) ||||||
| | (Full and simple) data set messages |X|X|X||
| | (Full and simple) Network messages |X|X|X||
| | Raw message format |-|X|X||
| | Single data set format |-|X|X||
| | Custom configuration using content flags |-|-|-||
| | Configurable per writer group |-|X|X||
| OPC UA Pub Sub message [encoding](./messageformats.md) ||||||
| | JSON Encoding |X|X|X||
| | [Non-Reversible](https://reference.opcfoundation.org/Core/Part6/v105/docs/) |-|X|X||
| | [Reversible](https://reference.opcfoundation.org/Core/Part6/v105/docs/) |-|X|X||
| | [Compact](https://reference.opcfoundation.org/Core/Part6/v105/docs/) |-|-|-||
| | [Verbose](https://reference.opcfoundation.org/Core/Part6/v105/docs/) |-|-|-||
| | GZIP JSON Encoding |-|X|X||
| | JSON Schema publishing for JSON encoding |-|X|X|Experimental|
| | UADP Binary encoding per [Part 14](https://reference.opcfoundation.org/Core/Part14/v105/docs/)|-|X|X|Preview|
| | Avro and Avro+Gzip encoding with Schema publishing |-|X|-|Removed in 3.0|
| | [Samples JSON encoding](./messageformats.md#samples-mode-encoding-legacy) – Legacy |X|X|-|Removed in 3.0|
| | Samples Binary encoding – Legacy |X|-|-||
| | Configurable per writer group |-|X|X||
| OPC UA [Part 14](https://reference.opcfoundation.org/Core/Part14/v105/docs/) Pub Sub Message types ||||||
| | [Delta frame messages](./messageformats.md#data-value-change-messages) |-|X|X||
| | [Key frame messages](./readme.md#key-frames-delta-frames-and-extension-fields) / Key frame count |-|X|X||
| | [Event messages](./messageformats.md#event-messages) |-|X|X||
| | Keep alive messages |-|X|X||
| | Data Set Metadata messages (on change and periodic) |-|X|X||
| | Discovery messages |-|-|-||
| | Publisher status messages |-|-|-||
| Unified Namespace ||||||
| | Topic templates at writer group and dataset writer level |-|X|X|Preview|
| | Automatic topic routing using OPC UA browse paths |-|X|-|Removed in 3.0|
&#42; In 3.0 the publishing interval is retained but the batch size is not. The native OPC UA PubSub runtime emits a message per sample and does not coalesce, so `--bs` (`BatchSize`) and `--wgp` (writer group partitions) are accepted and ignored. `--bi` still controls how often the runtime samples, and `--om` still bounds the send queue.
