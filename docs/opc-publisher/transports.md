# OPC Publisher Transports <!-- omit in toc -->

[Home](./readme.md)

OPC Publisher translates these northbound transport protocols to and from OPC UA with different levels of fidelity.

> Except for the fully supported Azure IoT transport, other northbound transports here are currently provided in preview or experimental form and are by default disabled.

## Table Of Contents <!-- omit in toc -->

- [Azure IoT Hub and Azure IoT Edge](#azure-iot-hub-and-azure-iot-edge)
- [MQTT](#mqtt)
- [Azure EventHub](#azure-eventhub)
- [Dapr](#dapr)
- [HTTP](#http)
  - [Built-in HTTP API server](#built-in-http-api-server)
  - [Publishing to a Webhook](#publishing-to-a-webhook)
- [Other transports](#other-transports)
  - [File System](#file-system)

## Azure IoT Hub and Azure IoT Edge

When deployed into Azure IoT Edge OPC Publisher is automatically configured and connects securely to the Edge Hub component. This is the officially supported configuration of OPC Publisher.

In addition to the automatic configuration during deployment in IoT Edge it is also possible to start the OPC Publisher with an Azure IoT Hub connection using the [command line](./commandline.md) argument `--ec` or by setting the `EdgeHubConnectionString` environment variable to the Azure IoT connection string.

> Providing Azure IoT connection strings is supported for debugging only. Azure IoT connection strings contain access keys. You must ensure they are properly secured in your development environment.

The following table shows the supported features of the Azure IoT Edge and IoT Hub transport implementations inside OPC Publisher:

| Feature                  | Supported  | Notes |
| ------------------------ | ---------- | ----- |
| State store              | Yes        | Using Device Twin desired and reported properties |
| Configuration API        | Yes        | |
| Twin and Discovery       | Yes        | |
| Streaming (Browse/HDA)   | No         | |
| Payload size limits      | 256 kb (*) | Chunked transfer is supported using the companion web-api service. |
| Telemetry publishing     | Yes        | |
| Discovery Events         | Yes        | |
| Operational Events       | Yes        | |
| Message size limits      | 256 kb     | |
| Integrated with Web-Api  | Yes        | |

IoT Edge is used as the default transport unless the Azure IoT related configuration cannot be found at startup time. By default all discovery and runtime events are sent through IoT Edge. Using the `-t` command line argument a different transport can be chosen to publish network messages with. The writer group (network message) transport can also be individually overridden for a writer group using the OPC Publisher [configuration schema's](./readme.md#configuration-schema) `WriterGroupTransport` attribute.

Direct methods can be used to interact with the OPC Publisher configuration, discovery, and Twin API. Direct method calls have a payload limit of 256 KB. The [Web API companion service](../web-api/readme.md) uses chunking to overcome this limitation. Still, high frequency interaction such as browsing through direct methods is naturally slow.

## MQTT

> This transport is in preview

OPC Publisher 2.9 can be connected to an MQTT broker using MQTT 3.11 and MQTT 5 protocols. The connection information is provided in the form of a connection string through the [command line](./commandline.md) argument `--mqc`.

> The MQTT broker connection string typically contains the user name and password to access the MQTT broker. Ensure that you properly secure this secret in your production environment.

MQTT v5 supports RPC which allows a client to call the OPC Publisher [API](./api.md) over MQTT. Like in the case of [IoT Edge](#azure-iot-hub-and-azure-iot-edge), streaming HDA and browse results are not supported.

OPC Publisher supports rich configuration of topic templates that are used when publishing data to MQTT broker, or to specify the root of the RPC server endpoint on which API requests are received. For more information how to configure MQTT topic trees see the [command line](./commandline.md) documentation for the `--rtt`, `--mtt`, `--ett`, `--ttt` and `--mdt` arguments.

OPC Publisher will subscribe to the configured RPC server topic and handle method calls. The sub topic name is the name of the method. The response topic is specified as part of the MQTTv5 message header. Upon handling the method call the response payload will be published to the response topic. The request and response payload as well as the method name are defined in the API documentation.

The following table shows the supported features of the MQTT transport implementation inside OPC Publisher:

| Feature                  | Supported  | Notes |
| ------------------------ | ---------- | ----- |
| State store              | No         | |
| Configuration API        | Yes        | |
| Twin and Discovery       | Yes        | |
| Streaming (Browse/HDA)   | No         | |
| Payload size limits      | 512 MB     | Actual limits depend on MQTT broker and can be configured using the connection string. |
| Telemetry publishing     | Yes        | |
| Discovery Events         | Yes        | Only if IoT Hub transport was not configured |
| Operational Events       | Yes        | Only if IoT Hub transport was not configured |
| Message size limits      | 512 MB     | Actual limits depend on MQTT broker and can be configured using the connection string. |
| Integrated with Web-Api  | No         | |

To configure MQTT as the default transport for telemetry publishing specify the `-t=Mqtt` command line argument. This default choice can be individually overridden for a writer group in the OPC Publisher [configuration](./readme.md#configuration-schema) using the `WriterGroupTransport` attribute.

The message schema can optionally be published to a schema topic, a schema topic template can be configured using the `--stt` [command line options](./commandline.md).  This feature is currently experimental.

## Azure EventHub

> This transport is in preview

OPC Publisher can be configured to publish to [Azure EventHubs](https://dapr.io/). The event hub connectivity information can be configured in OPC Publisher using a connection string using the `--eh, --eventhubnamespaceconnectionstring` [command line options](./commandline.md). The connection string can be obtained from the Azure portal.

The following table shows the supported features of the Azure EventHub transport implementation inside OPC Publisher:

| Feature                  | Supported  | Notes |
| ------------------------ | ---------- | ----- |
| State store              | No         | |
| Configuration API        | No         | |
| Twin and Discovery       | No         | |
| Streaming (Browse/HDA)   | No         | |
| Payload size limits      | No         | |
| Telemetry publishing     | Yes        | |
| Discovery Events         | Yes        | |
| Operational Events       | Yes        | |
| Message size limits      | Yes        | |
| Integrated with Web-Api  | No         | |

In addition, OPC Publisher can publish message schemas to the Azure schema registry (Experimental). The schema group can be selected using the `--sg, --schemagroup` [command line options](./commandline.md). Publishing to the schema registry requires OPC Publisher to authenticate using an Azure identity e.g., a managed service identity, user or service principal.

## Dapr

> This transport is experimental

OPC Publisher can be configured to use [Dapr](https://dapr.io/) to publish to a supported [Dapr Pub Sub component](https://docs.dapr.io/reference/components-reference/supported-pubsub/) via a side car. To get started with Dapr follow the documentation on the [Dapr web site](https://docs.dapr.io/getting-started/).

The following table shows the supported features of the Dapr transport implementation inside OPC Publisher:

| Feature                  | Supported  | Notes |
| ------------------------ | ---------- | ----- |
| State store              | Yes        | Only if IoT Hub transport was not configured |
| Configuration API        | Yes        | Via [Dapr Service invoke](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/howto-invoke-discover-services/) of the [REST API](#http) |
| Twin and Discovery       | Yes        | Via [Dapr Service invoke](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/howto-invoke-discover-services/) of the [REST API](#http) |
| Streaming (Browse/HDA)   | Yes        | Via [Dapr Service invoke](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/howto-invoke-discover-services/) of the [REST API](#http) |
| Payload size limits      | No         | |
| Telemetry publishing     | Yes        | |
| Discovery Events         | Yes        | Only if IoT Hub and MQTT transport were not configured |
| Operational Events       | Yes        | Only if IoT Hub and MQTT transport were not configured |
| Message size limits      | No (*)     | Actual limits depend on the chosen Pub Sub component. Limits can be configured using the connection string |
| Integrated with Web-Api  | No         | |

Whether the Dapr transport is enabled is determined by whether the Dapr Pub Sub component and Api Token are configured. The Pub sub component name and Api token can be provided through the `-d` connection string using the [command line](./commandline.md). The Api Token is automatically provided by the Dapr runtime through the `DAPR_API_TOKEN` environment variable. Whether the component was configured in your Dapr environment is not validated.

The Dapr transport will add all user properties into the meta data of the published event. For retained messages the `retain` metadata variable is set to `true`.

To configure Dapr as the default transport for telemetry publishing specify the `-t=Dapr` command line argument. This default choice can be individually overridden for a writer group in the OPC Publisher [configuration](./readme.md#configuration-schema) using the `WriterGroupTransport` attribute.

## HTTP

The HTTP transports is split into two parts: The [built in HTTP server](#built-in-http-api-server) and the [Http event publisher](#publishing-to-a-webhook) that can be configured to publish to another HTTP server (Web Hook). The following features are supported:

| Feature                  | Supported  | Notes |
| ------------------------ | ---------- | ----- |
| State store              | No         | |
| Configuration API        | Yes        | |
| Twin and Discovery       | Yes        | |
| Streaming (Browse/HDA)   | Yes        | |
| Payload size limits      | No         | |
| Telemetry publishing     | Yes        | |
| Discovery Events         | Yes        | Only if IoT Hub, MQTT, and Dapr transports were not configured |
| Operational Events       | Yes        | Only if IoT Hub, MQTT, and Dapr transports were not configured |
| Message size limits      | No         | |
| Integrated with Web-Api  | No         | |

### Built-in HTTP API server

> This transport is in preview

The built in HTTP server eposes the OPC Publisher [API](./api.md). The API is secured by an API Key. The API key is generated at startup if it is not found inside the state store. The default state store is the IoT Hub device twin. Therefore the API key can be read from the device registry in IoT Hub. The API key must be provided in every request in the HTTP `Authorization` header using the `ApiKey <ApiKey>` scheme.

The HTTP server is exposed at port 80 (no encryption) and 443 (with encryption). The SSL channel is secured using a IoT Edge generated server certificate, just like Edge Hub's endpoints. The API can be further secured using a [Dapr](#dapr) side car providing mtls based security on top of the generic API surface.

The built in HTTP server can be disabled using the `--dh` [command line](./commandline.md) argument.

> Never expose the OPC Publisher HTTP port outside of the Edge Gateway's docker network. The HTTP server should only be accessible by other docker containers.

### Publishing to a Webhook

> This transport is experimental

The other part of the HTTP support in OPC Publisher is the ability to publish network messages to a HTTP server (Web Hook). The web server information is provided on the [command line](./commandline.md) using the `-r` argument in the form of a connection string.

> The HTTP Connection string can contain an API Key. Ensure that you properly secure the connection string.

## Other transports

### File System

> This transport is experimental

Sometimes it is important for troubleshooting and debugging to dump the network messages to files. The File system transport can be used to reflect the network messages in their topic structure inside the file system. The file system Transport is intended for debugging only and not for production scenarios. You can configure a root folder on the bind mount of OPC Publisher using the `-o` [command line](./commandline.md) argument and inspect the results using a file viewer of choice.
