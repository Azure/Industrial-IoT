# OPC Onboarding Agent

The Onboarding agent receives discovery events from the OPC Twin module (supervisor) as a result of a scan.  The events are processed, and the agent creates IoT Hub Device Twins for each OPC UA application’s endpoint using the IoT Hub Device Twin Registry.  The onboarding micro service is an event processor host.  It can be scaled to the number of event hub partitioned of IoT Hub.

When receiving new scan results it performs the following tasks:

- Add new applications and their endpoints to the registry if they do not yet exist
- Update existing applications and endpoints to reflect what the server reported and re-enable them if they are disabled.
- Add the "Supervisor ID" to any endpoint to claim the endpoint for the supervisor that found it (unless it is already activated).
- Disable all applications and endpoints found or registered through the supervisor at an earlier point in time and that were not found this time around.  

All soft deleted applications and their endpoints can be purged using the [OPC Registry API](../api/registry/overview.md).

## Next steps

- [Learn about OPC Twin module discovery](module.md)
- [Learn about OPC Registry](registry.md)
