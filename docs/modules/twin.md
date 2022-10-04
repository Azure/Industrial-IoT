# OPC Twin Edge Module

[Home](readme.md)

The OPC Twin module runs inside IoT Edge.  

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## OPC UA Client Services

The OPC Twin provides a IoT Hub device method API allowing the OPC Twin Microservice to invoke OPC UA server functionality on an activated endpoint. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service.  This includes [Variant](../api/json.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response.  This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint so the OPC UA server is not overburdened with 100’s of simultaneous requests.  

## Parametrization command line interface options & environment variables

The following list of module parametrization settings can be provided either as CLI options or as  environment variables. When both environment variable and cli argument is provided,the latest will overrule the env variable.

            site=VALUE
                                      The site OPC Twin is assigned to
                                      Type: string
                                      Default: <not set>

            EdgeHubConnectionString=VALUE
                                      An IoT Edge Device or IoT Edge module connection string to use
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


## Licensing

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## Next steps

* [Learn how to deploy OPC Twin Module](../deploy/howto-install-iot-edge.md)
* [Learn about the OPC Twin Microservice](../services/twin.md)
* [Learn about OPC Registry Onboarding](../services/processor-onboarding.md)
