# OPC Historian Access Microservice

[Home](readme.md)

## Overview

The Historian Microservice provides a REST API to interact with the HDA (Historical Data Access) services in an OPC UA server.  It leverages the OPC [Twin](twin.md) API, and invokes these services on the server through the OPC Twin module running on the edge.

The Historian Microservice API is more accessible by surfacing the `ExtensionObject` encoded `HistoryRead`/`HistoryUpdate` payload as first class API parameters.

## Docker image

`docker pull mcr.microsoft.com/iot/opc-history-service:latest`
