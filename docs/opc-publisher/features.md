# Features

[Home](./readme.md)

The following table shows the supported features of OPC Publisher and planned feature additions. Preview features are supported through GitHub issues only, experimental features will become preview or fully supported features if you request so through GitHub issues or by contacting us. If you would like to see additional features added, please open a feature request.

| Feature | Sub Feature | 2.8 | 2.9 | Feature state |
| ------- | ----------- |---- | --- | ------------- |
| Uses latest .net reference stack ||X|X||
| .net Version || .net 6 | .net 7 ||
| Secure channel transport and configuration ||X|X||
| OPC UA HTTP transport and configuration ||-|-|#1997|
| Secure channel over web socket transport and configuration ||-|-|#1997|
| Secure channel certificate management API |||||
| | Client Cert |-|-|#1996|
| | Using EST |-|-||
| | GDS Push |-|-||
| Session-reconnect handling across connection loss ||X|X||
| | Using official .net stack implementation |-|X||
| [Reverse Connect](./readme.md#using-opc-ua-reverse-connect) ||-|X|Preview|
| User authentication |||||
| | Username / Password authentication |X|X||
| | X509 based user authentication |-|-|Backlog|
| Local persisted user credential |||||
| | As plain text |X|X||
| | Securely encrypted |-|-||
| | From secret manager API |-|-||
| Get Endpoint and Server information [API](./api.md#getservercapabilities) ||-|X|Preview|
| Connect and Disconnect [API](./api.md) ||-|X|Preview|
| Test connection [API](./api.md#testconnection) ||-|X|Preview|
| Browse [API](./api.md#browse) ||-|X||
| | Browse first/next |-|X||
| | RegEx Browse filter |-|-||
| | Streaming browse “Fast browsing” / Partial node set export |-|X|Preview|
| Translate browse path [API](./api.md) ||-|X||
| Read [API](./api.md#valueread) |||||
| | Read Value |-|X||
| | Read other attributes of nodes |-|X||
| | Get instance metadata |-|X||
| Write [API](./api.md#valuewrite)|||||
| | Write Value |-|X||
| | Write other attributes of nodes |-|X||
| Method Call [API](./api.md#methodcall) ||-|X||
| HDA [API](./api.md#history) for processed, modified, attimes, events time series data |||||
| | Read |-|X|Preview|
| | Streaming read |-|X|Experimental|
| | Update |-|X|Preview|
| | Upsert |-|X|Preview|
| | Delete |-|X|Preview|
| Subscribe to [value changes](./readme.md#configuration-schema) |||||
| | Value change subscriptions |X|X||
| | Data change filter support |-|X||
| | Deadband |-|X||
| | Status trigger |-|X||
| | Set server queue size per value|-|X||
| | Set server queue LIFO/FIFO behavior per value|-|X||
| | Periodic read ([cyclic read](./readme.md#sampling-and-publishing-interval-configuration))|-|X|Preview|
| | Heartbeat (Periodic resending of last known value) |X|X|Deprecated|
| Subscribe to [events](./readme.md#configuring-event-subscriptions) |||||
| | Simple (get all events of a type from event notifier)|-|X||
| | Event filter (filter events on server before sending)|-|X||
| | Condition handling / Condition snapshotting|-|X|Preview|
| Registered Nodes |||||
| | For periodic reads (registered read) |-|X|Preview|
| | For monitored items |-|X|Preview|
| | Register API call |-|-||
| | Unregister API call |-|-||
| Client-side transport queue configuration |||||
| | Batch size and publishing interval publisher wide |X|X||
| | Batch size and publishing interval per group |-|X||
| | Load shedding |-|X||
| | Queue jumping / Priority messages|-|-||
| | Advanced overflow handling strategies|-|-||
| IIoT Platform 2.8 Orchestrated mode support ||X|-||
| 0 message loss||-|-||
| Transfer subscription|||||
| | On reconnect |-|X||
| | On startup |-|-||
| Deferred Acknowledge (Backpressure to server)||-|X|Experimental|
| Published nodes JSON [schema](./readme.md#configuration-schema) support |||||
| | v2.5 |X|X||
| | v2.8 |X|X||
| | v2.9 |-|X||
| | Published nodes JSON schema validation |X|-||
| OPC UA Pub/Sub configuration API (Part 14)||-|-||
| Data contextualization |||||
| | Add Endpoint/Dataset name to message header (Routing) ||X|X||
| | Enrichment |-|-|Backlog|
| | Transformation |-|-||
| | Normalization |-|-||
| Running as docker outside IoT Edge or K8s ||-|X|Experimental|
| [IoT Edge](./readme.md#install-iot-edge) deployment support ||X|X||
| | ISA 95 nested support ||-|X||
| | IoT Hub direct method-based configuration|X|X||
| | IoT Hub direct method-based API calls|-|X||
| | DTDL interface for API |-|-||
| [MQTT](./transports.md#mqtt) request response-based API and configuration |||||
| | v5 request response |-|X|Preview|
| | v3.11 using IoT Hub like &rid= correlation |-|X|Experimental|
| Kafka request response-based API and configuraton ||-|-||
| [HTTP](./transports.md#built-in-http-api-server) REST command/control and configuration API ||-|X|Preview|
| Configuration via OPC UA endpoint ||-|-||
| Prometheus [Metrics](./observability.md) |||||
| | For module metrics |X|X||
| | Endpoint metrics |X|X||
| | Process data |-|-||
| Periodic diagnostic output to Console ||X|X||
| Health and liveness probe / watchdog ||-|-|Backlog|
| Message and event publishing [transports](./transports.md) |||||
| | IoT Hub |X|X||
| | MQTT topics |-|X|Preview|
| | DAPR Pub/Sub (Kafka, Redis, etc.) |-|X|Experimental|
| | Publishing to a Web hook|-|X|Experimental|
| | File system dump|-|X|Experimental|
| Multiple cloud transports enabled in parallel ||-|X|Preview|
| Select desired transport per writer group ||-|X|Preview|
| Cloud Events support ||-|-|via DAPR|
| OPC UA Pub Sub [message content profiles](./messageformats.md) |||||
| | (Full and simple) data set messages |X|X||
| | (Full and simple) Network messages |X|X||
| | Raw message format |-|X||
| | Custom configuration using content flags |-|-||
| | Configurable per writer group |-|X||
| OPC UA Pub Sub message [encoding](./messageformats.md)
| | JSON Encoding |X|X||
| | JSON Encoding per standard |-|X||
| | GZIP JSON Encoding |-|X||
| | UADP Binary encoding |-|X|Preview|
| | [Reversible Encoding](./messageformats.md#reversible-encoding) |-|X|Preview|
| | [Samples JSON encoding](./messageformats.md#samples-mode-encoding-legacy) – Legacy |X|X|Deprecated|
| | Samples Binary encoding – Legacy |X|-||
| | Configurable per writer group |-|X||
| OPC UA Pub Sub Message types
| | [Delta frame messages](./messageformats.md#data-value-change-messages) |-|X||
| | Key frame messages / Key frame count |-|X||
| | [Event messages](./messageformats.md#event-messages) |-|X||
| | Keep alive messages |-|X||
| | Data Set Metadata messages (on change and periodic) |-|X||
| | Discovery messages |-|-|Backlog|
| | Publisher status messages |-|-|Backlog|
