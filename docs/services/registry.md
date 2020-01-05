# Registry Microservice

[Home](../readme.md)

## Overview

The role of the Registry Microservice is to manage identities in IoT Hub.  These items include:

* **Applications**. In OPC parlance, an “Application” can be a server or a client or both.  It is also a grouping mechanism for Endpoints, i.e. Applications have Endpoints.  An Application contains all server provided information, such as Discovery URLs, Application and Product URIs.
* **Endpoints**.  Each endpoint represents the twin of an OPC UA Server application’s endpoint.  A server can have multiple endpoints with different configurations, including security properties.
* **Supervisors**.  The supervisor identity is a OPC Twin module’s identity.  A supervisor manages "activated" endpoint identities.  It also performs discovery and has a couple of other Microservices up its sleeve.  
* **Publishers**.  The publisher identity is the OPC Publisher module's identity.  A publisher is the host of multiple publish job workers.
* **Gateways.**  The gateway is the host of supervisor (OPC Twin) and publisher (OPC Publisher) modules.

You can update, read as well as **query** all of these identities' models in the OPC Registry.  

The following diagram shows the registry service in relationship to the other components.

![](../media/architecture.PNG)

## Create and Delete Items in the registry

You can also Create and Delete Application identities ("resources").  There are 3 ways applications are created:

* By **POST**ing of an Application Model.  In this case the application will not have endpoints associated.  This is useful to register client applications or reflect servers registered in another system.
* By the onboarding Microservice as a result of a "Discover" API call (One time)
* By the onboarding Microservice as a result of a continuous discovery (configured in the supervisor identity)

A **DELETE** of an application will deactivate and delete all associated endpoints.  

## Update Items

The supervisor model for example contains the configuration for "recurring" discovery mode (i.e. constantly monitor and find added and removed servers on the network).  You can update the discovery mode using the API.

To update an item, you must send a HTTP `PATCH` request containing the item's model (e.g. a `SupervisorModel`).  

Values in the model that are set to *null* <u>are not updated</u>.  However, missing values in the incoming payload are de-serialized as *null*.  This means, to remove a value, you must explicitly set the value in the model to its *default*, e.g. an empty string, or 0, etc.

## Activate and Deactivate Endpoints

When registered in OPC Registry (and thus Azure IoT Hub) Server endpoints are by default deactivated.  An operator must  inspect the endpoint certificate and actively activate an endpoint using the API before it can be used.  The OPC Registry provides REST APIs to activate and deactivate endpoints.  Once activated you can interact with endpoint identities using the OPC Twin REST API.  

## Next steps

* [Learn about Registry Onboarding](onboarding.md)
* [Learn about the OPC Twin module](../modules/twin.md)
* [Explore the Registry REST API](../api/registry/readme.md)
