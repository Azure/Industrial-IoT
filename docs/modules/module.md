# OPC Twin Edge Module

The OPC Twin module runs inside IoT Edge.  

## Server Discovery

The supervisor provides services inside the OPC Twin module which include OPC UA server discovery.  If discovery is configured and enabled, the OPC Twin Module will send the results of a scan probe via the IoT Edge and IoT Hub telemetry path to the Onboarding agent.  The agent processes the results and updates the Identities in the [OPC Registry](registry.md) and thus IoT Hub Device Registry.

The discovery interface allows for **recurring** as well as **one-time** scans.  

### Configuration

The Discovery process can involve active network and port scanning.  Scanning can be finely configured, e.g. specifying

- address ranges (needed when hosted in a docker context where the host interfaces are not visible)
- port ranges (to narrow or widen scanning to a list of known ports)
- number of workers and time between scans (Advanced)

If no active scanning is desired the configuration can also specify

- a list of discovery URIs (which if provided disable the use of any address and port ranges in the configuration)

### Recurring discovery

Recurring discovery can be enabled in the Supervisor identity's model using the OPC Registry API.   A specific “mode” must be specified in addition to an optional [configuration](#configuration):

- **`Off`** – no scanning takes place (default)
- **`Local`** – only scan the address of each interface, i.e. localhost.
- **`Fast`** – only scan the first 255 addresses of each interface's subnet using a set of well-known OPC UA server ports.
- **`Scan`** – Do a full scan of all interfaces using all IANA unassigned ports as well as the OPC UA LDS ports 4840-4841.

If any discovery URL's are part of the supervisor's discovery configuration, no active scanning is performed, therefore any mode other than **Off** will cause the probing of the provided URLs.

### One time discovery

One-time only discovery can be initiated by an operator through the OPC registry’s REST API.  A discovery [configuration](#configuration) is part of the request payload.  One time discovery is serialized at the edge, i.e. will be performed one by one.

Using the configuration's discovery URLs, servers can be registered using a well known discovery URL without active scanning.  

## OPC UA Client Services

Another service provided by the Module is the Azure IoT Hub device method OPC UA client API.  The API allows the OPC Twin Microservice to invoke OPC UA server functionality on an activated endpoint. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service.  This includes [Variant](twin.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response.  This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint (same as in the existing OPC Publisher codebase) the actual OPC UA server is not overburdened with 100’s of simultaneous requests.  

## OPC Publisher Module Integration

The OPC Publisher module is responsible for maintaining durable Subscriptions to Variables and Events on an endpoint.  On startup, the twin module locates the OPC Publisher module in its Edge environment.  It then forwards requests to it that it receives by way of the OPC Twin Microservice REST API.  This includes requests to

- **`Publish`** a variable on an endpoint to IoT Hub.
- Disable publishing (**`Unpublish`**)
- **`List`** all currently published nodes

If no OPC Publisher module is deployed alongside OPC Twin module, OPC Twin publishing REST calls will fail.

For more information about OPC Publisher, see the [OPC Publisher](https://github.com/Azure/iot-edge-opc-publisher) repository on GitHub.

## Next steps

- [Learn how to deploy OPC Twin Module](howto-modules.md)
- [Explore the OPC Twin Module repository](https://github.com/Azure/azure-iiot-opc-twin-module)
- [Learn about the OPC Twin Microservice](twin.md)
- [Learn about OPC Registry Onboarding](onboarding.md)
