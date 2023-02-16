# Migration from 2.8 to 2.9

## API changes

* OPC Twin capabilities are integrated now in OPC Publisher. The following differences between 2.8.4 OPC Twin and OPC Publisher 2.9 exist:
  * OPC Publisher does not support activation and deactivation of Endpoint Twins, which allowed OPC Twin endpoints to be addressed with a IoT Hub device id. Instead all API's must be invoked with a `ConnectionModel` parameter (`connection`) and the original request model.
  * The concept of supervisor (the OPC Twin module instance) and discoverer (the OPC Discovery module instance) are completely equivalent to the publisher concept in 2.9. The supervisor, discovery, and publisher REST APIs have been retained for backwards compatibility and return the same information which is the twin of the Publisher module.
  * Activation and deactivation, and the endpoint connectivity concept have been removed.  The current activation and deactivation API can be used to connect and disconnect clients in the OPC Publisher that are not actively managing subscriptions.
    * The GetSupervisorStatus and ResetSupervisor API has been removed without replacement.
  * GetEndpointCertificate API now returns a `X509CertificateChainModel` instead of byte array in 2.8.
* OPC Discovery capabiltiies are integrated into OPC Publisher. 
