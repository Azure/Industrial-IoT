# OPC Historian Access Microservice (Experimental)

[Home](../readme.md)

> **The OPC Historian Access Microservice is not intended for production use at this point in time !**

The Historian Microservice provides a REST API to interact with the HDA (Historical Data Access) services in an OPC UA server.  It leverages the OPC [Twin](twin.md) API, and invokes these services on the server through the OPC Twin module running on the edge.   

The Historian Microservice API is more accessible by surfacing the `ExtensionObject` encoded `HistoryRead`/`HistoryUpdate` payload as first class API parameters.

