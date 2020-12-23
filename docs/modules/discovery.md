# Discovery Services Edge Module

[Home](readme.md)

The Discovery services module runs inside IoT Edge.  

## Server Discovery

The discovery module, represented by the discoverer identity, provides discovery services on the edge which include OPC UA server discovery.  If discovery is configured and enabled, the module will send the results of a scan probe via the IoT Edge and IoT Hub telemetry path to the Onboarding service.  The service processes the results and updates all related Identities in the [Registry](../services/registry.md).

The discovery interface allows for **recurring** as well as **one-time** scans.  

### Configuration

The Discovery process can involve active network and port scanning.  Scanning can be finely configured, e.g. specifying:

* address ranges (needed when hosted in a docker context where the host interfaces are not visible)
* port ranges (to narrow or widen scanning to a list of known ports)
* number of workers and time between scans (Advanced)

If no active scanning is desired the configuration can also specify:

* a list of discovery URIs (which if provided disable the use of any address and port ranges in the configuration)

### Recurring discovery

Recurring discovery can be enabled in the Discoverer identity's twin model using the Registry API.   A specific “mode” must be specified in addition to an optional [configuration](#Configuration):

* **`Off`** – no scanning takes place (default)
* **`Local`** – only scan the address of each interface, i.e. localhost.
* **`Fast`** – only scan the first 255 addresses of each interface's subnet using a set of well-known OPC UA server ports.
* **`Scan`** – Do a full scan of all interfaces using all IANA unassigned ports as well as the OPC UA LDS ports 4840-4841.

If any discovery URL's are part of the supervisor's discovery configuration, no active scanning is performed, therefore any mode other than **Off** will cause the probing of the provided URLs.

### One time discovery

One-time only discovery can be initiated by an operator through the registry’s REST API.  A discovery [configuration](#Configuration) is part of the request payload.  One time discovery is serialized at the edge, i.e. will be performed one by one.

Using the configuration's discovery URLs, servers can be registered using a well known discovery URL without active scanning.  

### Progress

The discovery progress as well as current request queue size is reported via the telemetry path and forwarded to interested listeners.   One of these listeners is the SignalR publisher which any client can subscribe to and retrieve the progress reporting.   This is shown in the console and UI samples included in this repository.

## Next steps

* [Learn how to deploy Discovery Module](../deploy/howto-install-iot-edge.md)
* [Learn about the Registry Microservice](../services/registry.md)
* [Learn about Onboarding Microservice](../services/onboarding.md)
