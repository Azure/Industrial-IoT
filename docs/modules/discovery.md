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

## Parametrization command line interface options & environment variables

The following list of module parametrization settings can be provided either as CLI options or as  environment variables. When both environment variable and cli argument is provided,the latest will overrule the env variable.

            site=VALUE
                                      The site discovery service is assigned to
                                      Alternative: --s, --site
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: <not set>

            EdgeHubConnectionString=VALUE
                                      An IoT Edge Device or IoT Edge module connection string to use.
                                      When deployed in the iotedge module context, the environment variable
                                      is already set as part of the container deployment
                                      Type: connection string
                                      Default: <not set> <preset by iotedge runtime>

            Transport=VALUE
                                      Upstream communication setting for edgeHub respectively IoTHub protocol
                                      Type: string enum: Any, Amqp, Mqtt, AmqpOverTcp, AmqpOverWebsocket,
                                      MqttOverTcp, MqttOverWebsocket, Tcp, Websocket.
                                      Default: MqttOverTcp

            BypassCertVerification=VALUE
                                      Upstream communication setting for bypassing certificate verification
                                      Type: boolean
                                      Default: false

            EnableMetrics=VALUE
                                      Enable/Disable upstream metrics propagation 
                                      Type: boolean
                                      Default: true

            ApplicationName=VALUE
                                      OPC UA Client Application Config - Application name as per
                                      OPC UA definition. This is used for authentication during communication
                                      init handshake and as part of own certificate validation.
                                      Type: string
                                      Default: "Microsoft.Azure.IIoT"

            ApplicationUri=VALUE
                                      OPC UA Client Application Config - Application URI as per
                                      OPC UA definition
                                      Type: string
                                      Default: $"urn:localhost:{ApplicationName}:microsoft:")

            ProductUri=VALUE
                                      OPC UA Client Application Config - Product URI as per
                                      OPC UA definition
                                      Type: string
                                      Default: "https://www.github.com/Azure/Industrial-IoT"

            DefaultSessionTimeout=VALUE
                                      OPC UA Client Application Config - Session timeout
                                      as per OPC UA definition
                                      Type: integer
                                      Default: 0, meaning <not set>

            MinSubscriptionLifetime=VALUE
                                      OPC UA Client Application Config - Minimum subscription lifetime
                                      as per OPC UA definition
                                      Type: integer
                                      Default: 0, <not set>

            KeepAliveInterval=VALUE
                                      OPC UA Client Application Config - Keep alive interval
                                      as per OPC UA definition
                                      Type: integer milliseconds
                                      Default: 10,000 (10s)

            MaxKeepAliveCount=VALUE
                                      OPC UA Client Application Config - Maximum count of keep alive events
                                      as per OPC UA definition
                                      Type: integer
                                      Default: 50

            PkiRootPath=VALUE
                                      OPC UA Client Security Config - PKI certificate store root path
                                      Type: string
                                      Default: "pki"

            ApplicationCertificateStorePath=VALUE
                                      OPC UA Client Security Config - application's
                                      own certificate store path
                                      Type: string
                                      Default: $"{PkiRootPath}/own"

            ApplicationCertificateStoreType=VALUE
                                      OPC UA Client Security Config - application's
                                      own certificate store type
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            ApplicationCertificateSubjectName=VALUE
                                      OPC UA Client Security Config - the subject name
                                      in the application's own certificate
                                      Type: string
                                      Default: "CN=Microsoft.Azure.IIoT, C=DE, S=Bav, O=Microsoft, DC=localhost"

            TrustedIssuerCertificatesPath=VALUE
                                      OPC UA Client Security Config - trusted certificate issuer
                                      store path
                                      Type: string
                                      Default: $"{PkiRootPath}/issuers"

            TrustedIssuerCertificatesType=VALUE
                                      OPC UA Client Security Config - trusted issuer certificates
                                      store type
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            TrustedPeerCertificatesPath=VALUE
                                      OPC UA Client Security Config - trusted peer certificates
                                      store path
                                      Type: string
                                      Default: $"{PkiRootPath}/trusted"

            TrustedPeerCertificatesType=VALUE
                                      OPC UA Client Security Config - trusted peer certificates
                                      store type
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            RejectedCertificateStorePath=VALUE
                                      OPC UA Client Security Config - rejected certificates
                                      store path
                                      Type: string
                                      Default: $"{PkiRootPath}/rejected"

            RejectedCertificateStoreType=VALUE
                                      OPC UA Client Security Config - rejected certificates
                                      store type
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            AutoAcceptUntrustedCertificates=VALUE
                                      OPC UA Client Security Config - auto accept untrusted 
                                      peer certificates
                                      Type: boolean
                                      Default: false

            RejectSha1SignedCertificates=VALUE
                                      OPC UA Client Security Config - reject deprecated Sha1 
                                      signed certificates
                                      Type: boolean
                                      Default: false

            MinimumCertificateKeySize=VALUE
                                      OPC UA Client Security Config - minimum accepted
                                      certificates key size
                                      Type: integer
                                      Default: 1024

            AddAppCertToTrustedStore=VALUE
                                      OPC UA Client Security Config - automatically copy own 
                                      certificate's public key to the trusted certificate store
                                      Type: boolean
                                      Default: true

            RejectUnknownRevocationStatus=VALUE
                                      OPC UA Client Security Config - reject chain validation 
                                      with CA certs with unknown revocation status, e.g. when the
                                      CRL is not available or the OCSP provider is offline.
                                      Type: boolean
                                      Default: true

            SecurityTokenLifetime=VALUE
                                      OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds
                                      Type: integer (milliseconds)
                                      Default: 3,600,000 (1h)

            ChannelLifetime=VALUE
                                      OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds
                                      Type: integer (milliseconds)
                                      Default: 300,000 (5 min)

            MaxBufferSize=VALUE
                                      OPC UA Stack Transport Secure Channel - Max buffer size
                                      Type: integer
                                      Default: 65,535 (64KB -1)

            MaxMessageSize=VALUE
                                      OPC UA Stack Transport Secure Channel - Max message size
                                      Type: integer
                                      Default: 4,194,304 (4 MB)

            MaxArrayLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Max array length
                                      Type: integer
                                      Default: 65,535 (64KB - 1)

            MaxByteStringLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Max byte string length
                                      Type: integer
                                      Default: 1,048,576 (1MB);

            OperationTimeout=VALUE
                                      OPC UA Stack Transport Secure Channel - OPC UA Service call
                                      operation timeout
                                      Type: integer (milliseconds)
                                      Default: 120,000 (2 min)

            MaxStringLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Maximum length of a string
                                      that can be send/received over the OPC UA Secure channel
                                      Type: integer
                                      Default: 130,816 (128KB - 256)


## Next steps

* [Learn how to deploy Discovery Module](../deploy/howto-install-iot-edge.md)
* [Learn about the Registry Microservice](../services/registry.md)
* [Learn about Registry onboarding](../services/processor-onboarding.md)
