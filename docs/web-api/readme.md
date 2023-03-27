# Azure Industrial IoT Platform Components

[Home](../readme.md)

The OPC Publisher Web API provides APIs to configure the OPC Publisher module. It configures the OPC Publisher and manages job that an OPC Publisher module can receive to publish data.

## APIs

The Publisher API provides functionality to start the publishing of values of an endpoint. This endpoint also handles the creation of new jobs.
There is also the possibility to bulk publish nodes (i.e. to publish multiple nodes at once).

## Configuration

| Configuration Parameter                                                                  | Modality | Default Value                                          | Description                                                                                                                                                                                                  |
|------------------------------------------------------------------------------------------|----------|--------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **CORS Configuration**                                                                   |          |                                                        |                                                                                                                                                                                                              |
| `Cors:Whitelist`<br>`PCS_CORS_WHITELIST`                                                 | optional | (empty)                                                | When a CORS Whitelist is set, [CORS](https://fetch.spec.whatwg.org/#http-cors-protocol) will be enabled. Otherwise, CORS will be disabled.                                                                   |
| **Service Authentication Configuration**                                                 |          |                                                        |                                                                                                                                                                                                              |
| `Auth:Required`<br>`PCS_AUTH_REQUIRED`                                                   | optional | `true`                                                 | Whether authentication should be added to APIs and OpenAPI UI.                                                                                                                                               |
| **OpenApi Configuration**                                                                |          |                                                        |                                                                                                                                                                                                              |
| `OpenApi:Enabled`<br>`PCS_OPENAPI_ENABLED`                                               | optional | `true` if `OpenApi:AppId` is set,<br>`false` otherwise | Whether OpenApi should be enabled.                                                                                                                                                                           |
| `OpenApi:UseV2`<br>`PCS_OPENAPI_USE_V2`<br>`PCS_SWAGGER_V2`                              | optional | `true`                                                 | Create v2 open api json.                                                                                                                                                                                     |
| `OpenApi:AppId`<br>`PCS_OPENAPI_APPID`<br>`PCS_AAD_CONFIDENTIAL_CLIENT_APPID`            | optional | (empty)                                                | The Application id for the OpenAPI UI client.                                                                                                                                                                |
| `OpenApi:AppSecret`<br>`PCS_OPENAPI_APP_SECRET`<br>`PCS_AAD_CONFIDENTIAL_CLIENT_SECRET`  | optional | (empty)                                                | Application Secret                                                                                                                                                                                           |
| `OpenApi:AuthorizationUrl`                                                               | optional | (empty)                                                | Authorization URL                                                                                                                                                                                            |
| `OpenApi:ServerHost`<br>`PCS_OPENAPI_SERVER_HOST`                                        | optional | (empty)                                                | Server host for OpenAPI.                                                                                                                                                                                     |
| **Forwarded Headers Configuration**                                                      |          |                                                        |                                                                                                                                                                                                              |
| `AspNetCore:ForwardedHeaders:Enabled`<br>`ASPNETCORE_FORWARDEDHEADERS_ENABLED`           | optional | `false`                                                | Determines whether processing of forwarded headers should be enabled or not.                                                                                                                                 |
| `AspNetCore:ForwardedHeaders:ForwardLimit`<br>`ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT` | optional | `0`                                                    | Determines limit on number of entries in the forwarded headers.                                                                                                                                              |
| **Web Host Configuration**                                                               |          |                                                        |                                                                                                                                                                                                              |
| `Auth:HttpsRedirectPort`<br>`PCS_AUTH_HTTPSREDIRECTPORT`                                 | optional | `0`                                                    | `null` value allows http. Should always be set to the https port except for local development. JWT tokens are not encrypted and if not sent over HTTPS will allow an attacker to get the same authorization. |
| `Host:ServicePathBase`<br>`PCS_SERVICE_PATH_BASE`<br>`PCS_PUBLISHER_SERVICE_PATH_BASE`   | optional | (empty)                                                | Determines URL path base that service should be running on.                                                                                                                                                  |
| **IoT Hub Configuration**                                                                |          |                                                        |                                                                                                                                                                                                              |
| `IoTHubConnectionString`<br>`PCS_IOTHUB_CONNSTRING`<br>`_HUB_CS`                         | required | `null`                                                 | IoT hub connection string                                                                                                                                                                                    |
| **Container Registry Configuration**                                                     |          |                                                        |                                                                                                                                                                                                              |
| `Docker:Server`<br>`PCS_DOCKER_SERVER`                                                   | optional | `mcr.microsoft.com`                                    | URL of the server                                                                                                                                                                                            |
| `Docker:User`<br>`PCS_DOCKER_USER`                                                       | optional | `null`                                                 | Username                                                                                                                                                                                                     |
| `Docker:Password`<br>`PCS_DOCKER_PASSWORD`                                               | optional | `null`                                                 | Password                                                                                                                                                                                                     |
| `Docker:ImagesNamespace`<br>`PCS_IMAGES_NAMESPACE`                                       | optional | (empty)                                                | The namespace of the images                                                                                                                                                                                  |
| `Docker:ImagesTag`<br>`PCS_IMAGES_TAG`                                                   | optional | (current microservice version)                         | The tag of the images                                                                                                                                                                                        |

## Registry

The role of the Registry Microservice is to manage entities and identities in IoT Hub. These include:

* **Applications**. In OPC parlance, an "Application" can be a server or a client or both. It is also a grouping mechanism for Endpoints, i.e. Applications have Endpoints. An Application contains all server provided information, such as Discovery URLs, Application and Product URIs.

* **Endpoints**. Each endpoint represents the twin of an OPC UA Server application's endpoint. A server can have multiple endpoints with different configurations, including security properties.  Endpoint identities are used to invoke OPC UA services or retrieve endpoint specific information, such as certificates.

* **Gateways.** The gateway is an IoT Edge runtime that hosts discovery modules, OPC Twin and OPC publisher modules, all of which have their IoT Edge Module identities managed in the registry as well:

  * **Discoverers**. The discoverer identity is the Discovery modules's identity. A discoverer provides discovery, such as OPC UA and network scanning services.
  * **Supervisors**. The supervisor identity is a OPC Twin module's identity. A supervisor manages "activated" endpoint identities.
  * **Publishers**. The publisher identity is the OPC Publisher module's identity. A publisher is the host of multiple publish job workers.

You can update, read as well as **query** all of these identities' models in the Registry.

The following diagram shows the registry service in relationship to the other components.

![architecture](./media/architecture.png)

### Create and Delete Items in the registry

You can also Create and Delete Application identities ("resources").  There are 2 ways applications are created:

* By **POST**ing of an Application Model. In this case the application will not have endpoints associated. This is useful to register client applications or reflect servers registered in another system.

* By the processing of discovery events emitted by the discovery edge module and processing of these events by the Onboarding Microservice. This typically happens either

  * as a result of a "Discover" API call (One time), or
  * a continuous (persistent) discovery job (configured in the discoverer identity twin), or
  * when registering applications via their OPC UA discovery Url.

A **DELETE** of an application will deactivate and delete all associated endpoints.

### Update Entities

The discoverer module's information comes in the form of a DiscovererModel document.  For example, the DiscovererModel contains the configuration for "recurring" discovery mode (i.e. constantly monitor and find added and removed servers on the network).  You can update the discovery mode using this mechanism API.

To update an item, you must send a HTTP `PATCH` request containing the item's model (e.g. a `DiscovererModel`).

Values in the model that are set to *null* are not updated.  However, missing values in the incoming payload are de-serialized as *null*.  This means, to remove a value, you must explicitly set the value in the model to its *default*, e.g. an empty string, or 0, etc.

### Activate and Deactivate Endpoints

After initial registration in the Registry (and thus Azure IoT Hub) server endpoint twins are by default deactivated.

An operator must inspect an endpoint certificate and actively activate endpoints using the provided Activation API before they can be used.

The Registry Microservice provides REST APIs to activate and deactivate endpoints.  Once activated you can interact with endpoint identities using the OPC Twin REST API.

## OPC UA client services

The following diagram shows the twin service in relationship to the other components.

![architecture](../media/architecture.png)

OPC Twin Microservice in cloud exposes a [REST API](../api/twin/readme.md) to call the following [OPC UA](../opcua.md) services on activated endpoints in an OPC Twin edge module.

### Supported OPC UA Services

* **Read** and **Write** a “Value” on a Variable node
* **Call** a “Method Node”
* **Read** and **Write** Node “Attributes
* **History Read** and **Update** service calls to interact with Historians
* **Batching** of any of the above calls.
* **Browse** first / next (with and without reading the browsed target nodes)
* Get **meta data** of methods (to display input arguments to a user)

## Events

The SignalR event service forwards ...

* Registry update events (from ServiceBus Topics)
* Discovery Progress (from ServiceBus Topics)
* telemetry samples from the secondary Telemetry EventHub

over SignalR to clients and thus provides a reactive UX experience.

The secondary Telemetry EventHub receives processed and decoded edge telemetry messages from the OPC Publisher module (PubSub).  This is the same EventHub that Azure Time Series Insights (TSI) can connect to for historian query capability.

The Onboarding processor service is used to process discovery events from the OPC Discovery module resulting from a discovery scan.  The onboarding service is an event processor host that consumes messages from the `onboarding` constumer group and creates a IoT Hub Device Twins for each server and server endpoint using the IoT Hub Device Twin Registry.  

This involves the following tasks:

* Add new applications and their endpoints to the registry if they do not yet exist
* Update existing applications and endpoints to reflect what the server reported and re-enable them if they are disabled.
* Add the "Supervisor ID" to any endpoint to claim the endpoint for the supervisor that found it (unless it is already activated).
* Mark any applications and endpoints found or registered through the supervisor at an earlier point in time and that were not found this time around.  

Applications and their endpoints that have not been found for a while can be purged using the [REST API](../api/readme.md).

## Health checks

The OPC Publisher exposes port `8045` as a health check endpoint. The health checks include a liveness and a readiness probe, you can read more about them [here](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/).

The liveness probe will respond as soon as possible. The readiness probe is continuously evaluated in the existing `EnsureWorkerRunningTimer_ElapsedAsync` event handler ([WorkerSupervisor](C:\Git5\Industrial-IoT\common\src\Microsoft.Azure.IIoT.Agent.Framework\src\Agent\Default\WorkerSupervisor.cs)).

### How to use

The health check endpoint is enabled by default. You can verify that it has started by the logs output by the OPC Publisher.

![Health checks endpoint started as visible in the standard output.](../media/healthchecks.png)

You can use a browser or a command line to probe the health check endpoint:
1. Liveness: `http://localhost:8045/healthz`
1. Readiness: `http://localhost:8045/ready`

### Format

A healthy response from the health check endpoint is an HTTP response with its status code 200, its `Content-Type` header set to "text/plain", and its body set to "OK". An unhealthy probe will not yield a response.

## Dependencies

The deployment tool deploys the Industrial IoT Microservices into a Linux [VM](https://azure.microsoft.com/en-us/services/virtual-machines/) or [Azure Kubernetes Cluster](https://azure.microsoft.com/en-us/services/kubernetes-service/) and creates the following required PaaS services:

### Azure IoT Hub

The [Azure IoT Hub](https://azure.microsoft.com/en-us/services/iot-hub/) is used as cloud broker for Edge to Cloud and Cloud to Edge messaging.   IoT Hub manages device and module identities, including OPC UA endpoints and applications.

### Azure KeyVault

Azure [KeyVault](https://azure.microsoft.com/en-us/services/key-vault/) is used to store configuration secrets such as private keys and passwords securely.

### Azure Storage

A [storage](https://azure.microsoft.com/en-us/services/storage/blobs/) account is used by the service to persist Azure IoT Hub Event Hub Endpoint read offsets and partition information to support partitioned and reliable access from multiple instances.

### Azure Active Directory

All Microservices are registered as Application in [Azure Active Directory](https://azure.microsoft.com/en-us/services/active-directory/) to integrate with Enterprise Authentication and Authorization policies.

## Engineering tool

The Engineering tool is a blazor application that provides a simple frontend for the contained services.  It is a sample and showcases all API's just like the CLI does.

## Next steps

* [Deploy Microservices to Azure](../deploy/readme.md)
* [Register a server and browse its address space](../tutorials/tut-use-cli.md)
* [Explore the REST API](../api/readme.md)
