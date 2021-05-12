# OPC Twin Edge Module

[Home](readme.md)

The OPC Twin module runs inside IoT Edge.  

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## OPC UA Client Services

The OPC Twin provides a IoT Hub device method API allowing the OPC Twin Microservice to invoke OPC UA server functionality on an activated endpoint. The Payload is transcoded from JSON to OPC UA binary and passed on through the OPC UA stack to the OPC UA server.  The response is reencoded to JSON and passed back to the cloud service.  This includes [Variant](../api/json.md) encoding and decoding in a consistent JSON format.

Payloads that are larger than the Azure IoT Hub supported Device Method payload size are chunked, compressed, sent, then decompressed and reassembled for both request and response.  This allows fast and large value writes and reads, as well as returning large browse results.  

A single session is opened on demand per endpoint so the OPC UA server is not overburdened with 100’s of simultaneous requests.  

## Command line options

The configuration of the twin can be made either by command line options or environment variables.  The command line option will overule the environment variable.

```bash
Options: 
    EdgeHubConnectionString=VALUE 
        mandatory option
        default: <empty>
        when deployed in the iotedge module context, the environment variuable is 
        already set as part of the container deployment
    
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
        public keys of the OPC UA server applicatins allowed by the OPC Twin to 
        establish a secure connection
        default: `<PkiRootPath>/trusted`
        
        Note: since in OPC UA the application's trust is symetrical, the OPC UA 
        Servers must trust the Twins's certificate as well before establishing a 
        secure connection. 
        For convenience, the OPC Twin will copy the it's own certificate public 
        key to this location
        
    IssuerCertPath=VALUE 
        the path of the trusted issuer (Certificate Authority) certificate store
        default: `<PkiRootPath>/issuer`
    
    RejectedCertPath=VALUE 
        the path to store the certificates of applications attempted to communicate
        that were rejected 
        default: `<PkiRootPath>/rejected`
    
    AutoAccept=VALUE
        flag to instruct the OPC Twin to automatically trust the new OPC UA 
        Servers that were not connected are not yet trusted. The certificates of 
        the new servers will be automatically copied in the Trusted store so that 
        will be trusted from now on 
        allowed values: `true`, `false`
        default: `false`
        Note: setting AutoAccept to true is a security risk and should not be used in
        production.
        
    OwnCertX509StorePathDefault=VALUE                
        Defines the Windows Certificate Store location where the Own certificate 
        is placed
        Advanced setting applicable only when certificate store type is X509Store 
        under Windows.
        default: CurrentUser\\UA_MachineDefault
```

## Licensing

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## Next steps

* [Learn how to deploy OPC Twin Module](../deploy/howto-install-iot-edge.md)
* [Learn about the OPC Twin Microservice](../services/twin.md)
* [Learn about OPC Registry Onboarding](../services/processor-onboarding.md)
