# Azure Industrial IoT Platform Web API

[Home](../readme.md)

The OPC Publisher Web API service is an optional component included in this repo and provides cloud side APIs to configure and operate the OPC Publisher module.

## APIs

The optional Web service exposes API with enabling the following functionality:

* Start the publishing of values of an endpoint (a.k.a Publish services)
* Access the OPC UA services exposed by OPC Publisher (a.k.a [Twin](#twin) services)
* Discover OPC UA servers and endpoints (a.k.a [Discovery](#discovery) services)
* Manage the discovered entities (a.k.a [Registry](#registry) services)
* Receive updates through SignalR subscriptions (a.k.a [Event](#events) services)

The Web service has [dependencies](#dependencies) and operational [capabilities](#operations). A sample of using the API is provided in the form of the [Engineering Tool](#engineering-tool).

## Registry

The role of the Registry component is to enable managenment of entities and identities in IoT Hub. These include:

* **Applications**. In OPC parlance, an "Application" can be a server or a client or both. It is also a grouping mechanism for Endpoints, i.e. Applications have Endpoints. An Application contains all server provided information, such as Discovery URLs, Application and Product URIs.

* **Endpoints**. Each endpoint represents the twin of an OPC UA Server application's endpoint. A server can have multiple endpoints with different configurations, including security properties.  Endpoint identities are used to invoke OPC UA services or retrieve endpoint specific information, such as certificates.

* **Gateways.** The gateway is an IoT Edge runtime that hosts discovery modules, OPC Twin and OPC publisher modules, all of which have their IoT Edge Module identities managed in the registry as well:

* **Publishers**. The publisher identity is the OPC Publisher module's identity. A publisher is the host of multiple publish job workers.

> NOTE: In 2.9 the previous concepts of "Supervisor" and "Discoverer" have been subsumed by the "Publisher" concept.

You can Read as well as **query** all of these identities' models in the Registry.
You can also create and delete Application identities ("resources").  There are 2 ways applications can be created:

* By **POST**ing of an Application Model. In this case the application will not have endpoints associated. This is useful to register client applications or reflect servers registered in another system.

* By the processing of discovery events emitted by the discovery edge module and processing of these events by the Onboarding Microservice. This typically happens either

  * as a result of a "Discover" API call (One time), or
  * a continuous (persistent) discovery job (configured in the discoverer identity twin), or
  * when registering applications via their OPC UA discovery Url.

* By calling the **RegisterEndpoint** API to register an endpoint. If successful this will also add the application descriptor of the endpoint to the registry.

A **DELETE** of an application will delete all associated endpoints.

To update items where supported, you must send a HTTP `PATCH` request containing the item's model (e.g. a `PublisherModel`).

Values in the model that are set to *null* are not updated.  However, missing values in the incoming payload are de-serialized as *null*.  This means, to remove a value, you must explicitly set the value in the model to its *default*, e.g. an empty string, or 0, etc.

## Discovery

The discovery component provides access to the OPC Publisher discovery API. The discovery API enables active scanning of networks for OPC UA servers as well as calling the local discovery services of OPC UA. Results of discovery process are sent to IoT Hub and processed by the Web service. The Web service creates a IoT Hub Device Twins for each server and server endpoint using the IoT Hub Device Twin Registry.  

This involves the following tasks:

* Add new applications and their endpoints to the registry if they do not yet exist
* Update existing applications and endpoints to reflect what the server reported and re-enable them if they are disabled.
* Add the "Supervisor ID" to any endpoint to claim the endpoint for the supervisor that found it (unless it is already activated).
* Mark any applications and endpoints found or registered through the supervisor at an earlier point in time and that were not found this time around.  

Applications and their endpoints that have not been found for a while can be purged using the [REST API](../api/readme.md).

## Twin

OPC Twin component of the Web API provides a [REST API](../api/twin/readme.md) to call the following [OPC UA](../opcua.md) services using endpoint identifiers in the registry:

* **Read** and **Write** a “Value” on a Variable node
* **Call** a “Method Node”
* **Read** and **Write** Node “Attributes
* **History Read** and **Update** service calls to interact with Historians
* **Batching** of any of the above calls.
* **Browse** first / next (with and without reading the browsed target nodes)
* Get **meta data** of methods (to display input arguments to a user)

Before invoking any services you must inspect the endpoint certificate using the registry API.

> NOTE: After deleting of an endpoint in the registry access through the twin component is still possible for a while due to connection information caching.

## Events

The SignalR event component of the Web Service forwards ...

* Registry update events
* Discovery Progress
* telemetry samples

over SignalR to subscribed clients and thus provides a reactive UX experience.

## Operations

### Health checks

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
