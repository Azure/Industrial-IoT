# Onboarding Service

[Home](readme.md)

## Overview

The Onboarding processor service is used to process discovery events from the OPC Discovery module resulting from a discovery scan.  The onboarding service is an event processor host that consumes messages from the `onboarding` constumer group and creates a IoT Hub Device Twins for each server and server endpoint using the IoT Hub Device Twin Registry.  

This involves the following tasks:

* Add new applications and their endpoints to the registry if they do not yet exist
* Update existing applications and endpoints to reflect what the server reported and re-enable them if they are disabled.
* Add the "Supervisor ID" to any endpoint to claim the endpoint for the supervisor that found it (unless it is already activated).
* Mark any applications and endpoints found or registered through the supervisor at an earlier point in time and that were not found this time around.  

Applications and their endpoints that have not been found for a while can be purged using the [OPC Registry API](../api/registry/readme.md).

## Docker image

`docker pull mcr.microsoft.com/iot/opc-onboarding-service:latest`

## Next steps

* [Learn about OPC Twin module discovery](../modules/twin.md)
* [Learn about OPC Registry](registry.md)
