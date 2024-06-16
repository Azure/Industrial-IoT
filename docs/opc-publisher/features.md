# Features

[Home](./readme.md)

The following table shows the supported features of OPC Publisher and planned feature additions. Preview features are supported through GitHub issues only, experimental features will become preview or fully supported features if you request so through GitHub issues or by contacting us. If you would like to see additional features added, please open a feature request.

| Feature | Sub Feature | 2.8 | 2.9 | Feature state |
| ------- | ----------- |---- | --- | ------------- |
| Uses latest .net reference stack ||X|X||
| .net Version || .net 6 | .net 8 ||
| Secure channel transport and configuration ||X|X||
| OPC UA HTTP transport and configuration ||-|-|#1997|
| Secure channel over web socket transport and configuration ||-|-|#1997|
| Secure channel certificate management API |||||
| | Client Cert |-|X||
| | Using EST |-|-||
| | GDS Push |-|-||
| Session-reconnect handling across connection loss ||X|X||
| | Using official .net stack implementation |-|X||
| [Reverse Connect](./readme.md#using-opc-ua-reverse-connect) ||-|X|Preview|
| User authentication |||||
| | Username / Password authentication |X|X||
| | X509 based user authentication ||X||
| | Token based user authentication |-|-||
| Get Endpoint and Server information [API](./api.md#getservercapabilities) ||-|X|Preview|
| Connect and Disconnect [API](./api.md) ||-|X|Preview|
| Test connection [API](./api.md#testconnection) ||-|X|Preview|
| Browse [API](./api.md#browse) ||-|X||
| | Browse first/next |-|X||
| | RegEx Browse filter |-|-||
| | Streaming browse “Fast browsing” / Partial node set export |-|X|Preview|
| | Publish model change feed change events |-|X|Experimental|
| Translate browse path [API](./api.md) ||-|X||
| Read [API](./api.md#valueread) |||||
| | Read Value |-|X||
| | Read other attributes of nodes |-|X||
| | Get instance metadata |-|X||
| Write [API](./api.md#valuewrite)|||||
| | Write Value |-|X||
| | Write other attributes of nodes |-|X||
| Method Call [API](./api.md#methodcall) ||-|X||
| HDA [API](./api.md#history) for processed, modified, at-times, events time series data |||||
| | Read |-|X|Preview|
| | Streaming read |-|X|Experimental|
| | Update |-|X|Preview|
| | Upsert |-|X|Preview|
| | Delete |-|X|Preview|
| Subscribe to [value changes](./readme.md#configuration-schema) |||||
| | Value change subscriptions |X|X||
| | Data change filter support |-|X||
| | Using browse path to node |-|X||
| | Deadband |-|X||
| | Status trigger |-|X||
| | Set server queue size per value|-|X||
| | Set server queue LIFO/FIFO behavior per value|-|X||
| | Periodic read ([cyclic read](./readme.md#sampling-and-publishing-interval-configuration))|-|X|Preview|
| | Heartbeat (Periodic resending of last known value) |X|X||
| | Configurable heartbeat behavior (LKG, LKV) ||X||
| | Heartbeat message timestamp source configuration ||X||
| | Subscribe to all nodes under an Object node |-|-|#1320|
| Subscribe to [events](./readme.md#configuring-event-subscriptions) |||||
| | Using browse path to event notifier |-|X||
| | Simple (get all events of a type from event notifier)|-|X||
| | Event filter (filter events on server before sending)|-|X||
| | Condition handling / Condition snapshots|-|X|Preview|
| Triggering |||||
| | Using Server side triggering service (SetTriggering) |-|-||
| | Client side sampling of values on event |-|-||
| Re-evaluate subscriptions periodically |||||
| | Periodically |-|X||
| | On data model change events |-|-|#1209|
| Registered Nodes |||||
| | For periodic reads (registered read) |-|X|Preview|
| | For monitored items |-|X|Preview|
| | Register API call |-|-||
| | Unregister API call |-|-||
| Client-side transport queue configuration |||||
| | Batch size and publishing interval publisher wide |X|X||
| | Batch size and publishing interval per group |-|X||
| | Load shedding |X|X||
| | Queue jumping / Priority messages|-|-||
| | Advanced overflow handling strategies|-|-||
| IIoT Platform 2.8 Orchestrated mode support ||X|-||
| 0 message loss||-|-||
| Transfer subscription|||||
| | On reconnect |-|X||
| | On startup |-|-||
| Re-activate session on startup (Transfer session)||||Backlog|
| Deferred Notification Acknowledgement||-|X|Experimental|
| Back pressure to server||-|-|Backlog|
| Published nodes JSON [schema](./readme.md#configuration-schema) support |||||
| | v2.5 |X|X||
| | v2.8 |X|X||
| | v2.9 |-|X||
| | JSON schema validation |X|-||
| OPC UA Pub/Sub configuration API (Part 14)||-|-||
| Data contextualization |||||
| | Add Endpoint/Dataset name to message header (Routing) |X|X||
| | [Enrichment](./readme.md#key-frames-delta-frames-and-extension-fields) |-|X||
| | Transformation |-|-||
| | Normalization |-|-||
| Running as docker outside IoT Edge or K8s ||-|X|Experimental|
| [IoT Edge](./readme.md#install-iot-edge) deployment support ||X|X||
| | Fully functional in nested (ISA95) setup |-|X||
| | IoT Hub direct method-based configuration|X|X||
| | IoT Hub direct method-based API calls|-|X||
| | DTDL interface for API |-|-||
| [MQTT](./transports.md#mqtt) request response-based API and configuration |||||
| | v5 request response |-|X|Preview|
| | v3.11 using IoT Hub like &rid= correlation |-|X|Experimental|
| [HTTP](./transports.md#built-in-http-api-server) REST command/control and configuration API ||-|X|Preview|
| Kafka request response-based API and configuration ||-|-||
| Configuration via OPC UA endpoint ||-|-||
| Prometheus [Metrics](./observability.md) |||||
| | For module metrics |X|X||
| | Endpoint metrics |X|X||
| | Process data |-|-|Backlog|
| Periodic diagnostic output |||||
| | To Console |X|X||
| | To Diagnostics Topic/Output |-|X||
| | As OPC UA PubSub Message |-|-|Backlog|
| Health and liveness probe / watchdog ||-|-|Backlog|
| Message and event publishing [transports](./transports.md) |||||
| | IoT Hub |X|X||
| | MQTT topics |-|X|Preview|
| | Publishing to [Azure EventHub](./transports.md#azure-eventhub) |-|X|Preview|
| | Dapr Pub/Sub (Kafka, Redis, etc.) |-|X|Experimental|
| | Publishing to a Web hook|-|X|Experimental|
| | Dump messages and schemas to zip files in file system |-|X|Experimental|
| | Null sink|-|X|Experimental|
| Multiple cloud transports enabled in parallel ||-|X|Preview|
| Select desired transport per writer group ||-|X|Preview|
| Cloud Events support ||-|-|via Dapr|
| OPC UA Pub Sub [message content profiles](./messageformats.md) |||||
| | (Full and simple) data set messages |X|X||
| | (Full and simple) Network messages |X|X||
| | Raw message format |-|X||
| | Single data set format |-|X||
| | Custom configuration using content flags |-|-||
| | Configurable per writer group |-|X||
| OPC UA Pub Sub message [encoding](./messageformats.md) |||||
| | JSON Encoding |X|X||
| | JSON Encoding per standard |-|X||
| | GZIP JSON Encoding |-|X||
| | JSON Schema publishing for JSON encoding |-|X|Experimental|
| | UADP Binary encoding |-|X|Preview|
| | Avro and Avro+Gzip encoding with Schema publishing |-|X|Experimental|
| | [Reversible Encoding](./messageformats.md#reversible-encoding) |-|X|Preview|
| | [Samples JSON encoding](./messageformats.md#samples-mode-encoding-legacy) – Legacy |X|X|Deprecated|
| | Samples Binary encoding – Legacy |X|-||
| | Configurable per writer group |-|X||
| OPC UA Part 14 Pub Sub Message types |||||
| | [Delta frame messages](./messageformats.md#data-value-change-messages) |-|X||
| | [Key frame messages](./readme.md#key-frames-delta-frames-and-extension-fields) / Key frame count |-|X||
| | [Event messages](./messageformats.md#event-messages) |-|X||
| | Keep alive messages |-|X||
| | Data Set Metadata messages (on change and periodic) |-|X||
| | Discovery messages |-|-||
| | Publisher status messages |-|-||
| Unified Namespace |||||
| | Topic templates at writer group and dataset writer level |-|X|Preview|
| | Automatic topic routing using OPC UA browse paths |-|X|Experimental|
