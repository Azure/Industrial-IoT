# Azure Industrial IoT Platform Components

[Home](../readme.md)

The platform is made up of several cloud components that divide into Microservices providing REST API and Agent services that can provide processing and daemon like functionality.  Check out the [architecture](../architecture.md) for how these components relate.

## Microservices

Azure Industrial IoT Microservices use Azure IoT Edge and IoT Hub to connect the cloud and factory networks.

The following Microservices are part of the platform:

* [Registry Microservice](registry.md) (GA)
* [Registry Onboarding Microservice](onboarding.md) (GA)
* [OPC Twin Microservice](twin.md) (GA)
* [OPC Historic Data Access Microservice](history.md) (Preview)
* [OPC Publisher service](publisher.md) (Preview)
* [Job Service and Edge Service](jobs.md) (Preview)
* [Configuration Service](configuration.md) (Preview)
* [Edge Management Service](edgemanager.md) (Experimental)
* [OPC Vault Microservice](vault.md) (Experimental)
* [OPC Gateway Service](gateway.md) (Experimental)

These microservices use business logic and components included in this repository to provide discovery, registration, and remote control of industrial devices through REST APIs that can be implemented in any programming language and framework that can call an HTTP endpoint.

All REST calls use JSON as mime type.  OPC UA types are encoded as per Part 6 [with some exceptions](../api/json.md). 

## Agent services

The following Agents are part of the platform:

* [Edge Telemetry processor](telemetry.md) (GA)
* [Edge Event Processor](events.md) (COMING SOON)
* [Registry Discovery Multiplexer](discovery.md) (GA)
* [Identity Service](identity.md) (Preview)
* [Registry Security Alerting Service](security.md) (Preview)
* [Registry Eventing Service](registryevents.md) (Preview)
* [File upload Handler](fileupload.md) (Experimental)
* [OPC Model Importer](graph.md) (Experimental)

## Next steps

* [Deploy Microservices to Azure](../howto-deploy-microservices.md)
* [Register a server and browse its address space](../howto-use-cli.md)
* [Explore the REST API](../api/readme.md)
