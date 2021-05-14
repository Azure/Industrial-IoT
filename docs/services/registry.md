# Registry Microservice

[Home](readme.md)

## Overview

Namespace: Microsoft.Azure.IIoT.Services.OpcUa.Registry

The role of the Registry Microservice is to manage entities and identities in IoT Hub. These include:

* **Applications**. In OPC parlance, an "Application" can be a server or a client or both.  It is also a grouping mechanism for Endpoints, i.e. Applications have Endpoints.  An Application contains all server provided information, such as Discovery URLs, Application and Product URIs.

* **Endpoints**. Each endpoint represents the twin of an OPC UA Server application's endpoint.  A server can have multiple endpoints with different configurations, including security properties.  Endpoint identities are used to invoke OPC UA services or retrieve endpoint specific information, such as certificates.

* **Gateways.** The gateway is an IoT Edge runtime that hosts discovery modules, OPC Twin and OPC publisher modules, all of which have their IoT Edge Module identities managed in the registry as well:

  * **Discoverers**.  The discoverer identity is the Discovery modules's identity.  A discoverer provides discovery, such as OPC UA and network scanning services.
  * **Supervisors**. The supervisor identity is a OPC Twin module's identity. A supervisor manages "activated" endpoint identities.
  * **Publishers**. The publisher identity is the OPC Publisher module's identity.  A publisher is the host of multiple publish job workers.

You can update, read as well as **query** all of these identities' models in the Registry.

The following diagram shows the registry service in relationship to the other components.

![architecture](../media/architecture.png)

## Create and Delete Items in the registry

You can also Create and Delete Application identities ("resources").  There are 2 ways applications are created:

* By **POST**ing of an Application Model.  In this case the application will not have endpoints associated.  This is useful to register client applications or reflect servers registered in another system.

* By the processing of discovery events emitted by the discovery edge module and processing of these events by the Onboarding Microservice.  This typically happens either

  * as a result of a "Discover" API call (One time), or
  * a continuous (persistent) discovery job (configured in the discoverer identity twin), or
  * when registering applications via their OPC UA discovery Url.

A **DELETE** of an application will deactivate and delete all associated endpoints.

## Update Entities

The discoverer module's information comes in the form of a DiscovererModel document.  For example, the DiscovererModel contains the configuration for "recurring" discovery mode (i.e. constantly monitor and find added and removed servers on the network).  You can update the discovery mode using this mechanism API.

To update an item, you must send a HTTP `PATCH` request containing the item's model (e.g. a `DiscovererModel`).

Values in the model that are set to *null* are not updated.  However, missing values in the incoming payload are de-serialized as *null*.  This means, to remove a value, you must explicitly set the value in the model to its *default*, e.g. an empty string, or 0, etc.

## Activate and Deactivate Endpoints

After initial registration in the Registry (and thus Azure IoT Hub) server endpoint twins are by default deactivated.

An operator must inspect an endpoint certificate and actively activate endpoints using the provided Activation API before they can be used.

The Registry Microservice provides REST APIs to activate and deactivate endpoints.  Once activated you can interact with endpoint identities using the OPC Twin REST API.

## Docker image

`docker pull mcr.microsoft.com/iot/opc-registry-service:latest`

## Next steps

* [Learn about Registry Onboarding](onboarding.md)
* [Learn about the OPC Twin module](../modules/twin.md)
* [Explore the Registry REST API](../api/registry/readme.md)
