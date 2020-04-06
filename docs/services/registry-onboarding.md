# Onboarding Service

[Home](readme.md)

Namespace: Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding

## Overview

The Onboarding service is used to process discovery events from the OPC Twin module (supervisor) as a result of a scan.  The service API is called by the event processor host to create IoT Hub Device Twins for each server and server endpoint using the IoT Hub Device Twin Registry.  

The API performs the following tasks:

* Add new applications and their endpoints to the registry if they do not yet exist
* Update existing applications and endpoints to reflect what the server reported and re-enable them if they are disabled.
* Add the "Supervisor ID" to any endpoint to claim the endpoint for the supervisor that found it (unless it is already activated).
* Mark any applications and endpoints found or registered through the supervisor at an earlier point in time and that were not found this time around.  

Applications and their endpoints that have not been found for a while can be purged using the [OPC Registry API](../api/registry/readme.md).

## Next steps

* [Learn about OPC Twin module discovery](../modules/twin.md)
* [Learn about OPC Registry](registry.md)
