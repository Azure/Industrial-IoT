# OPC Gateway Microservice

[Home](readme.md)

> **This service is not intended for production use at this point in time !**

## Overview

The OPC Gateway Microservice is an OPC UA server in the "Gateway" role.  It allows accessing the OPC Registry and OPC Twin Microservices using OPC UA protocol.  

The OPC Gateway Microservice exposes HTTP and OPC UA Secure Channel endpoints that expose all **activated** endpoint resources in the OPC Registry.  

A client, such as UA Expert or the OPC UA Reference Client can be used to connect to the endpoint and use the OPC Twin supported OPC UA services, e.g. browsing, reading and writing node values and attributes.  

OPC Gateway enables existing UA applications to use your OPC Twin infrastructure.

## Current Limitations

* Many UA clients first browse the server tree to learn about the capabilities of a server.  This is an expensive operation as it involves many calls to the OPC Twin module.  
  * This will be addressed in a future version.
* The gateway currently only supports un-authenticated access.
  * This will be addressed in a future version.  The Gateway will then support **JWT token validation** (as per OPC UA version 1.04), note however that all known UA clients today do not support this authentication mode yet.
* OPC UA Subscription services (i.e. monitored items) are not supported.

## Next steps

* [Learn more about the OPC Registry](registry.md)
* [Learn more about the OPC Twin](twin.md)
* [Learn more about the overall Architecture](../architecture.md)
