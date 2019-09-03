# OPC Twin Edge Module

The OPC Twin module runs inside IoT Edge.  

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## Server Discovery

The supervisor provides services inside the OPC Twin module which include OPC UA server discovery.  If discovery is configured and enabled, the OPC Twin Module will send the results of a scan probe via the IoT Edge and IoT Hub telemetry path to the Onboarding agent.  The agent processes the results and updates the Identities in the [OPC Registry](../services/registry.md) and thus IoT Hub Device Registry.

The discovery interface allows for **recurring** as well as **one-time** scans.  

### Configuration

The Discovery process can involve active network and port scanning.  Scanning can be finely configured, e.g. specifying

* address ranges (needed when hosted in a docker context where the host interfaces are not visible)
* port ranges (to narrow or widen scanning to a list of known ports)
* number of workers and time between scans (Advanced)

If no active scanning is desired the configuration can also specify

* a list of discovery URIs (which if provided disable the use of any address and port ranges in the configuration)

### Recurring discovery

Recurring discovery can be enabled in the Supervisor identity's model using the OPC Registry API.   A specific “mode” must be specified in addition to an optional [configuration](#configuration):

* **`Off`** – no scanning takes place (default)
* **`Local`** – only scan the address of each interface, i.e. localhost.
* **`Fast`** – only scan the first 255 addresses of each interface's subnet using a set of well-known OPC UA server ports.
* **`Scan`** – Do a full scan of all interfaces using all IANA unassigned ports as well as the OPC UA LDS ports 4840-4841.

If any discovery URL's are part of the supervisor's discovery configuration, no active scanning is performed, therefore any mode other than **Off** will cause the probing of the provided URLs.

### One time discovery

One-time only discovery can be initiated by an operator through the OPC registry’s REST API.  A discovery [configuration](#configuration) is part of the request payload.  One time discovery is serialized at the edge, i.e. will be performed one by one.

Using the configuration's discovery URLs, servers can be registered using a well known discovery URL without active scanning.  

## OPC UA Client Services

Another service provided by the Module is the Azure IoT Hub device method OPC UA client API.  The API allows the OPC Twin Microservice to invoke OPC UA server functionality on an activated endpoint. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service.  This includes [Variant](../services/twin.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response.  This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint (same as in the existing OPC Publisher codebase) the actual OPC UA server is not overburdened with 100’s of simultaneous requests.  

## OPC Publisher Module Integration

The [OPC Publisher](publisher.md) module is responsible for maintaining durable Subscriptions to Variables and Events on an endpoint.  On startup, the twin module locates the OPC Publisher module in its Edge environment.  It then forwards requests to it that it receives by way of the OPC Twin Microservice REST API.  This includes requests to

* **`Publish`** a variable on an endpoint to IoT Hub.
* Disable publishing (**`Unpublish`**)
* **`List`** all currently published nodes

If no OPC Publisher module is deployed alongside OPC Twin module, OPC Twin publishing REST calls will fail.

For more information about OPC Publisher, see [here](publisher.md).

## Command line options

The command line options of aplicable for OPC Twin Module are as follows:
        
        Usage: dotnet Microsoft.Azure.IIoT.Modules.OpcUa.Twin.dll
        
        The configuration of the twin can be made either by command line options or environment variables. 
        The command line option will overule the environment variable
        
        Options: 
            EdgeHubConnectionString=VALUE 
                mandatory option
                default: <empty>
                when deployed in the iotedge module context, the environment variuable is already set 
                as part of the container deployment
            
            AppCertStoreType=VALUE 
                defines the Twin's OPC UA security management certificate PKI store type
                allowed values: `X509Store` and `Directory`
                default: `X509Store` for Windows and `Directory` for Linux
                Note: on Windows, due to a platform limitation, only X509Store is possible
            
            PkiRootPath=VALUE 
                Twin's OPC UA own certificate directory root path
                default: `pki`
            
            OwnCertPath=VALUE 
                path to store the Twin's own certificate, both public and private keys
                default: `<PkiRootPath>/own`

            TrustedCertPath=VALUE 
                path to store the Twin's trusted peer certificate list. Path contains the
                public keys of the OPC UA server applicatins allowed by the OPC Twin to establish a secure connection
                default: `<PkiRootPath>/trusted`
                
                Note: since in OPC UA the application's trust is symetrical, the OPC UA Servers must trust the 
                    Twins's certificate as well before establishing a secure connection. 
                For convenience, the OPC Twin will copy the it's own certificate public key to this location
                
            IssuerCertPath=VALUE 
                the path of the trusted issuer (Certificate Authority) certificate store
                default: `<PkiRootPath>/issuer`
            
            RejectedCertPath=VALUE 
                the path to store the certificates of applications attempted to communicate that were rejected 
                default: `<PkiRootPath>/rejected`
            
            AutoAccept=VALUE
                flag to instruct the OPC Twin to automatically trust the new OPC UA Servers that were not connected
                are not yet trusted. The certificates of the new servers will be automatically copied in the Trusted 
                store so that will be trusted from now on                
                allowed values: `true`, `false`
                default: `false`
                Note: setting AutoAccept to true is a security risk and should not be used in production.
                
            OwnCertX509StorePathDefault=VALUE                
                Defines the Windows Certificate Store location where the Own certificate is placed
                Advanced setting applicable only when certificate store type is X509Store under Windows.
                default: CurrentUser\\UA_MachineDefault

## Next steps

* [Learn how to deploy OPC Twin Module](../howto-deploy-modules.md)
* [Learn about the OPC Twin Microservice](../services/twin.md)
* [Learn about OPC Registry Onboarding](../services/onboarding.md)

