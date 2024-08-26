
<a name="paths"></a>
## Resources

<a name="certificates_resource"></a>
### Certificates
This section lists the certificate APi provided by OPC Publisher providing
            all public and private key infrastructure (PKI) related API methods.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="addtrustedhttpscertificate"></a>
#### AddTrustedHttpsCertificateAsync
```
POST /v2/pki/https/certs
```


##### Description
Add a certificate chain to the trusted https store. The certificate is provided as a concatenated set of certificates with the first the one to add, and the remainder the issuer chain.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The certificate chain.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="approverejectedcertificate"></a>
#### ApproveRejectedCertificate
```
POST /v2/pki/rejected/certs/{thumbprint}/approve
```


##### Description
Move a rejected certificate from the rejected folder to the trusted folder on the publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**thumbprint**  <br>*required*|The thumbprint of the certificate to trust.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addcertificatechain"></a>
#### AddCertificateChain
```
POST /v2/pki/trusted/certs
```


##### Description
Add a certificate chain to the specified store. The certificate is provided as a concatenated asn encoded set of certificates with the first the one to add, and the remainder the issuer chain.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The certificate chain.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removeall"></a>
#### RemoveAll
```
DELETE /v2/pki/{store}
```


##### Description
Remove all certificates and revocation lists from the specified store.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to add the certificate to|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information such store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|Nothing could be found.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="listcertificates"></a>
#### ListCertificates
```
GET /v2/pki/{store}/certs
```


##### Description
Get the certificates in the specified certificate store


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to enumerate|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|< [X509CertificateModel](definitions.md#x509certificatemodel) > array|
|**400**|The passed in information such as store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|Nothing could be found.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addcertificate"></a>
#### AddCertificate
```
PATCH /v2/pki/{store}/certs
```


##### Description
Add a certificate to the specified store. The certificate is provided as a pfx/pkcs12 optionally password protected blob.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to add the certificate to|string|
|**Query**|**password**  <br>*optional*|The optional password of the pfx|string|
|**Body**|**body**  <br>*required*|The pfx encoded certificate.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information such as store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removecertificate"></a>
#### RemoveCertificate
```
DELETE /v2/pki/{store}/certs/{thumbprint}
```


##### Description
Remove a certificate with the provided thumbprint from the specified store.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to add the certificate to|string|
|**Path**|**thumbprint**  <br>*required*|The thumbprint of the certificate to delete.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information such store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|Nothing could be found.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="listcertificaterevocationlists"></a>
#### ListCertificateRevocationLists
```
GET /v2/pki/{store}/crls
```


##### Description
Get the certificates in the specified certificated store


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to enumerate|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|< string (byte) > array|
|**400**|The passed in information such as store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|Nothing could be found.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removecertificaterevocationlist"></a>
#### RemoveCertificateRevocationList
```
DELETE /v2/pki/{store}/crls
```


##### Description
Remove a certificate revocation list from the specified store.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to add the certificate to|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information such store name is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|Nothing could be found.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addcertificaterevocationlist"></a>
#### AddCertificateRevocationList
```
PATCH /v2/pki/{store}/crls
```


##### Description
Add a certificate revocation list to the specified store. The certificate revocation list is provided as a der encoded blob.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**store**  <br>*required*|The store to add the certificate to|string|
|**Body**|**body**  <br>*required*|The pfx encoded certificate.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An internal error ocurred.|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="configuration_resource"></a>
### Configuration
This section contains the API to configure OPC Publisher.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="getconfiguredendpoints"></a>
#### [GetConfiguredEndpoints](./directmethods.md#getconfiguredendpoints_v1)
```
GET /v2/configuration
```


##### Description
Get a list of nodes under a configured endpoint in the configuration. Further information is provided in the OPC Publisher documentation. configuration.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**IncludeNodes**  <br>*optional*|Include nodes that make up the configuration|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The data was retrieved.|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="setconfiguredendpoints"></a>
#### [SetConfiguredEndpoints](./directmethods.md#setconfiguredendpoints_v1)
```
PUT /v2/configuration
```


##### Description
Enables clients to update the entire published nodes configuration in one call. This includes clearing the existing configuration. Further information is provided in the OPC Publisher documentation. configuration.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The new published nodes configuration|[SetConfiguredEndpointsRequestModel](definitions.md#setconfiguredendpointsrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### [AddOrUpdateEndpoints](./directmethods.md#addorupdateendpoints_v1)
```
PATCH /v2/configuration
```


##### Description
Add or update endpoint configuration and nodes on a server. Further information is provided in the OPC Publisher documentation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The parts of the configuration to add or update.|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|
|**404**|The endpoint was not found to add to|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="publishbulk"></a>
#### PublishBulk
```
POST /v2/configuration/bulk
```


##### Description
Configure node values to publish and unpublish in bulk. The group field in the Connection Model can be used to specify a writer group identifier that will be used in the configuration entry that is created from it inside OPC Publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The nodes to publish or unpublish.|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|
|**404**|The item could not be unpublished|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### [GetDiagnosticInfo](./directmethods.md#getdiagnosticinfo_v1)
```
POST /v2/configuration/diagnostics
```


##### Description
Get the list of diagnostics info for all dataset writers in the OPC Publisher at the point the call is received. Further information is provided in the OPC Publisher documentation.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|
|**405**|Call not supported or functionality disabled.|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### [GetConfiguredNodesOnEndpoint](./directmethods.md#getconfigurednodesonendpoint_v)
```
POST /v2/configuration/endpoints/list/nodes
```


##### Description
Get the nodes of a published nodes entry object returned earlier from a call to GetConfiguredEndpoints. Further information is provided in the OPC Publisher documentation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The entry model from a call to GetConfiguredEndpoints for which to gather the nodes.|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The information was returned.|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry was not found.|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### PublishList
```
POST /v2/configuration/list
```


##### Description
Get all published nodes for a server endpoint. The group field that was used in the Connection Model to start publishing must also be specified in this connection model.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*required*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The items were found and returned.|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### [PublishNodes](./directmethods.md#publishnodes_v1)
```
POST /v2/configuration/nodes
```


##### Description
PublishNodes enables a client to add a set of nodes to be published. Further information is provided in the OPC Publisher documentation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request contains the nodes to publish.|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### [UnpublishNodes](./directmethods.md#unpublishnodes_v1)
```
POST /v2/configuration/nodes/unpublish
```


##### Description
UnpublishNodes method enables a client to remove nodes from a previously configured DataSetWriter. Further information is provided in the OPC Publisher documentation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload specifying the nodes to unpublish.|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|
|**404**|The nodes could not be unpublished|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### [UnpublishAllNodes](./directmethods.md#unpublishallnodes_v1)
```
POST /v2/configuration/nodes/unpublish/all
```


##### Description
Unpublish all specified nodes or all nodes in the publisher configuration. Further information is provided in the OPC Publisher documentation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request contains the parts of the configuration to remove.|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|
|**404**|The nodes could not be unpublished|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### PublishStart
```
POST /v2/configuration/start
```


##### Description
Start publishing values from a node on a server. The group field in the Connection Model can be used to specify a writer group identifier that will be used in the configuration entry that is created from it inside OPC Publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The server and node to publish.|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### PublishStop
```
POST /v2/configuration/stop
```


##### Description
Stop publishing values from a node on the specified server. The group field that was used in the Connection Model to start publishing must also be specified in this connection model.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The node to stop publishing|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|
|**404**|The item could not be unpublished|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="diagnostics_resource"></a>
### Diagnostics
This section lists the diagnostics APi provided by OPC Publisher providing
            connection related diagnostics API methods.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="getactiveconnections"></a>
#### GetActiveConnections
```
GET /v2/connections
```


##### Description
Get all active connections the publisher is currently managing.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|< [ConnectionModel](definitions.md#connectionmodel) > array|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getchanneldiagnostics"></a>
#### GetChannelDiagnostics
```
GET /v2/diagnostics/channels
```


##### Description
Get channel diagnostic information for all connections.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|< [ChannelDiagnosticModel](definitions.md#channeldiagnosticmodel) > array|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="watchchanneldiagnostics"></a>
#### WatchChannelDiagnostics
```
GET /v2/diagnostics/channels/watch
```


##### Description
Get channel diagnostic information for all connections. The first set of diagnostics are the diagnostics active for all connections, continue reading to get updates.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[ChannelDiagnosticModelIAsyncEnumerable](definitions.md#channeldiagnosticmodeliasyncenumerable)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getconnectiondiagnostics"></a>
#### GetConnectionDiagnostics
```
GET /v2/diagnostics/connections
```


##### Description
Get diagnostics for all active clients including server and client session diagnostics.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|[ConnectionDiagnosticsModelIAsyncEnumerable](definitions.md#connectiondiagnosticsmodeliasyncenumerable)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="resetallconnections"></a>
#### ResetAllConnections
```
GET /v2/reset
```


##### Description
Can be used to reset all established connections causing a full reconnect and recreate of all subscriptions.


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful.|No Content|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="discovery_resource"></a>
### Discovery
OPC UA and network discovery related API.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method


<a name="discover"></a>
#### Discover
```
POST /v2/discovery
```


##### Description
Start network discovery using the provided discovery request configuration. The discovery results are published to the configured default event transport.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The discovery configuration to use during the discovery run.|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|boolean|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel
```
POST /v2/discovery/cancel
```


##### Description
Cancel a discovery run that is ongoing using the discovery request token specified in the discover operation.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The information needed to cancel the discovery operation.|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|boolean|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### FindServer
```
POST /v2/discovery/findserver
```


##### Description
Find servers matching the specified endpoint query spec.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The endpoint query specifying the matching criteria for the discovered endpoints.|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="register"></a>
#### Register
```
POST /v2/discovery/register
```


##### Description
Start server registration. The results of the registration are published as events to the default event transport.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Contains all information to perform the registration request including discovery url to use.|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|boolean|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="filesystem_resource"></a>
### FileSystem
This section lists the file transfer API provided by OPC Publisher providing
            access to file transfer services to move files in and out of a server
            using the File transfer specification.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="createdirectory"></a>
#### CreateDirectory
```
POST /v2/filesystem/create/directory/{name}
```


##### Description
Create a new directory in an existing file system or directory on the server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**name**  <br>*required*|The name of the directory to create as child under the parent directory provided|string|
|**Body**|**body**  <br>*required*|The file system or directory object to create the directory in and the connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelServiceResponse](definitions.md#filesystemobjectmodelserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="createfile"></a>
#### CreateFile
```
POST /v2/filesystem/create/file/{name}
```


##### Description
Create a new file in a directory or file system on the server


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**name**  <br>*required*|The name of the file to create as child under the directory or filesystem provided|string|
|**Body**|**body**  <br>*required*|The file system or directory object to create the file in and the connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelServiceResponse](definitions.md#filesystemobjectmodelserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="deletefilesystemobject"></a>
#### DeleteFileSystemObject
```
POST /v2/filesystem/delete
```


##### Description
Delete a file or directory in an existing file system on the server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The file or directory object to delete and the connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="deletefileordirectory"></a>
#### DeleteFileOrDirectory
```
POST /v2/filesystem/delete/{fileOrDirectoryNodeId}
```


##### Description
Delete a file or directory in the specified directory or file system.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**fileOrDirectoryNodeId**  <br>*required*|The node id of the file or directory to delete|string|
|**Body**|**body**  <br>*required*|The filesystem or directory object in which to delete the specified file or directory and the connection to use for the operation.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="download"></a>
#### Download
```
GET /v2/filesystem/download
```


##### Description
Download a file from the server


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Header**|**x-ms-connection**  <br>*required*|The connection information identifying the server to connect to perform the operation on. This is passed as json serialized via the header "x-ms-connection"|string|
|**Header**|**x-ms-target**  <br>*required*|The file object to upload. This is passed as json serialized via the header "x-ms-target"|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getfileinfo"></a>
#### GetFileInfo
```
POST /v2/filesystem/info/file
```


##### Description
Gets the file information for a file on the server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The file object and connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileInfoModelServiceResponse](definitions.md#fileinfomodelserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getfilesystems"></a>
#### GetFileSystems
```
POST /v2/filesystem/list
```


##### Description
Gets all file systems of the server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The connection information identifying the server to connect to perform the operation on.|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelServiceResponseIAsyncEnumerable](definitions.md#filesystemobjectmodelserviceresponseiasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getdirectories"></a>
#### GetDirectories
```
POST /v2/filesystem/list/directories
```


##### Description
Gets all directories in a directory or file system


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The directory or filesystem object and connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelIEnumerableServiceResponse](definitions.md#filesystemobjectmodelienumerableserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getfiles"></a>
#### GetFiles
```
POST /v2/filesystem/list/files
```


##### Description
Get files in a directory or file system on a server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The directory or filesystem object and connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelIEnumerableServiceResponse](definitions.md#filesystemobjectmodelienumerableserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getparent"></a>
#### GetParent
```
POST /v2/filesystem/parent
```


##### Description
Gets the parent directory or filesystem of a file or directory.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The file or directory object and connection information identifying the server to connect to perform the operation on.|[FileSystemObjectModelRequestEnvelope](definitions.md#filesystemobjectmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[FileSystemObjectModelServiceResponse](definitions.md#filesystemobjectmodelserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="upload"></a>
#### Upload
```
POST /v2/filesystem/upload
```


##### Description
Upload a file to the server.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Header**|**x-ms-connection**  <br>*required*|The connection information identifying the server to connect to perform the operation on. This is passed as json serialized via the header "x-ms-connection"|string|
|**Header**|**x-ms-options**  <br>*required*|The file write options to use passed as header "x-ms-mode"|string|
|**Header**|**x-ms-target**  <br>*required*|The file object to upload. This is passed as json serialized via the header "x-ms-target"|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="general_resource"></a>
### General
This section lists the general APi provided by OPC Publisher providing
            all connection, endpoint and address space related API methods.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="browsestream"></a>
#### BrowseStream (only HTTP transport)
```
POST /v2/browse
```


##### Description
Recursively browse a node to discover its references and nodes. The results are returned as a stream of nodes and references. Consult <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.2"> the relevant section of the OPC UA reference specification</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Description
Browse a a node to discover its references. For more information consult <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.2"> the relevant section of the OPC UA reference specification</a>. The operation might return a continuation token. The continuation token can be used in the BrowseNext method call to retrieve the remainder of references or additional continuation tokens.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### BrowseNext
```
POST /v2/browse/next
```


##### Description
Browse next


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### BrowsePath
```
POST /v2/browse/path
```


##### Description
Translate a start node and browse path into 0 or more target nodes. Allows programming aginst types in OPC UA. For more information consult <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.4"> the relevant section of the OPC UA reference specification</a>.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### MethodCall
```
POST /v2/call
```


##### Description
Call a method on the OPC UA server endpoint with the specified input arguments and received the result in the form of the method output arguments. See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.11.2"> the relevant section of the OPC UA reference specification</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### MethodMetadata
```
POST /v2/call/$metadata
```


##### Description
Get the metadata for calling the method. This API is obsolete. Use the more powerful GetMetadata method instead.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getservercapabilities"></a>
#### GetServerCapabilities
```
POST /v2/capabilities
```


##### Description
Get the capabilities of the server. The server capabilities are exposed as a property of the server object and this method provides a convinient way to retrieve them.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[RequestHeaderModelRequestEnvelope](definitions.md#requestheadermodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### GetEndpointCertificate
```
POST /v2/certificate
```


##### Description
Get a server endpoint's certificate and certificate chain if available.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The server endpoint to get the certificate for.|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### HistoryGetServerCapabilities
```
POST /v2/history/capabilities
```


##### Description
Get the historian capabilities exposed as part of the OPC UA server server object.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[RequestHeaderModelRequestEnvelope](definitions.md#requestheadermodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historygetconfiguration"></a>
#### HistoryGetConfiguration
```
POST /v2/history/configuration
```


##### Description
Get the historian configuration of a historizing node in the OPC UA server


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### HistoryRead
```
POST /v2/historyread/first
```


##### Description
Read the history using the respective OPC UA service call. See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> the relevant section of the OPC UA reference specification</a> for more information. If continuation is returned the remaining results of the operation can be read using the HistoryReadNext method.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### HistoryReadNext
```
POST /v2/historyread/next
```


##### Description
Read next history using the respective OPC UA service call. See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> the relevant section of the OPC UA reference specification</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### HistoryUpdate
```
POST /v2/historyupdate
```


##### Description
Update history using the respective OPC UA service call. Consult the <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> relevant section of the OPC UA reference specification</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### GetMetadata
```
POST /v2/metadata
```


##### Description
Get the type metadata for a any node. For data type nodes the response contains the data type metadata including fields. For method nodes the output and input arguments metadata is provided. For objects and object types the instance declaration is returned.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="compilequery"></a>
#### CompileQuery
```
POST /v2/query/compile
```


##### Description
Compile a query string into a query spec that can be used when setting up event filters on monitored items that monitor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The compilation request and connection information.|[QueryCompilationRequestModelRequestEnvelope](definitions.md#querycompilationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### ValueRead
```
POST /v2/read
```


##### Description
Read the value of a variable node. This uses the service detailed in the <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.1"> relevant section of the OPC UA reference specification</a>.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### NodeRead
```
POST /v2/read/attributes
```


##### Description
Read any writeable attribute of a specified node on the server. See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.2"> the relevant section of the OPC UA reference specification</a> for more information. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ReadResponseModel](definitions.md#readresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### TestConnection
```
POST /v2/test
```


##### Description
Test connection to an opc ua server. The call will not establish any persistent connection but will just allow a client to test that the server is available.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### ValueWrite
```
POST /v2/write
```


##### Description
Write the value of a variable node. This uses the service detailed in <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.4"> the relevant section of the OPC UA reference specification</a>.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### NodeWrite
```
POST /v2/write/attributes
```


##### Description
Write any writeable attribute of a specified node on the server. See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.4"> the relevant section of the OPC UA reference specification</a> for more information. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[WriteResponseModel](definitions.md#writeresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="history_resource"></a>
### History
This section lists all OPC UA HDA or Historian related API provided by
            OPC Publisher.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="historydeleteevents"></a>
#### HistoryDeleteEvents
```
POST /v2/history/events/delete
```


##### Description
Delete event entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to delete in the timeseries.|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyinsertevents"></a>
#### HistoryInsertEvents
```
POST /v2/history/events/insert
```


##### Description
Insert event entries into a specified timeseries of the historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to insert into the timeseries.|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### HistoryStreamEvents (only HTTP transport)
```
POST /v2/history/events/read
```


##### Description
Read an entire event timeseries from an OPC UA server historian as stream. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to read in the timeseries.|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadevents"></a>
#### HistoryReadEvents
```
POST /v2/history/events/read/first
```


##### Description
Read an event timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to read in the timeseries.|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadeventsnext"></a>
#### HistoryReadEventsNext
```
POST /v2/history/events/read/next
```


##### Description
Continue reading an event timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The continuation from a previous read request.|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreplaceevents"></a>
#### HistoryReplaceEvents
```
POST /v2/history/events/replace
```


##### Description
Replace events in a timeseries in the historian of the OPC UA server. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to replace with in the timeseries.|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyupsertevents"></a>
#### HistoryUpsertEvents
```
POST /v2/history/events/upsert
```


##### Description
Upsert events into a time series of the opc server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The events to upsert into the timeseries.|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historydeletevalues"></a>
#### HistoryDeleteValues
```
POST /v2/history/values/delete
```


##### Description
Delete value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to delete in the timeseries.|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historydeletevaluesattimes"></a>
#### HistoryDeleteValuesAtTimes
```
POST /v2/history/values/delete/attimes
```


##### Description
Delete value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to delete in the timeseries.|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historydeletemodifiedvalues"></a>
#### HistoryDeleteModifiedValues
```
POST /v2/history/values/delete/modified
```


##### Description
Delete value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to delete in the timeseries.|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyinsertvalues"></a>
#### HistoryInsertValues
```
POST /v2/history/values/insert
```


##### Description
Insert value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to insert into the timeseries.|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### HistoryStreamValues (only HTTP transport)
```
POST /v2/history/values/read
```


##### Description
Read an entire timeseries from an OPC UA server historian as stream. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### HistoryStreamValuesAtTimes (only HTTP transport)
```
POST /v2/history/values/read/attimes
```


##### Description
Read specific timeseries data from an OPC UA server historian as stream. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadvalues"></a>
#### HistoryReadValues
```
POST /v2/history/values/read/first
```


##### Description
Read a data change timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadvaluesattimes"></a>
#### HistoryReadValuesAtTimes
```
POST /v2/history/values/read/first/attimes
```


##### Description
Read parts of a timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadmodifiedvalues"></a>
#### HistoryReadModifiedValues
```
POST /v2/history/values/read/first/modified
```


##### Description
Read modified changes in a timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadprocessedvalues"></a>
#### HistoryReadProcessedValues
```
POST /v2/history/values/read/first/processed
```


##### Description
Read processed timeseries data inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### HistoryStreamModifiedValues (only HTTP transport)
```
POST /v2/history/values/read/modified
```


##### Description
Read an entire modified series from an OPC UA server historian as stream. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### HistoryReadValuesNext
```
POST /v2/history/values/read/next
```


##### Description
Continue reading a timeseries inside the OPC UA server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The continuation token from a previous read operation.|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### HistoryStreamProcessedValues (only HTTP transport)
```
POST /v2/history/values/read/processed
```


##### Description
Read processed timeseries data from an OPC UA server historian as stream. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to read in the timeseries.|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyreplacevalues"></a>
#### HistoryReplaceValues
```
POST /v2/history/values/replace
```


##### Description
Replace value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to replace with in the timeseries.|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="historyupsertvalues"></a>
#### HistoryUpsertValues
```
POST /v2/history/values/upsert
```


##### Description
Upsert value change entries in a timeseries of the server historian. See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/"> the relevant section of the OPC UA reference specification</a> and <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5"> respective service documentation</a> for more information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The values to upsert into the timeseries.|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The operation was successful or the response payload contains relevant error information.|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="writer_resource"></a>
### Writer
This section contains the API to configure data set writers and writer
            groups inside OPC Publisher. It supersedes the configuration API.
            Applications should use one or the other, but not both at the same
            time.
            


            The method name for all transports other than HTTP (which uses the shown
            HTTP methods and resource uris) is the name of the subsection header.
            To use the version specific method append "_V1" or "_V2" to the method
            name.


<a name="expandandcreateorupdatedatasetwriterentries"></a>
#### ExpandAndCreateOrUpdateDataSetWriterEntries
```
POST /v2/writer
```


##### Description
Create a series of published nodes entries using the provided entry as template. The entry is expanded using expansion configuration provided. Expanded entries are returned one by one with error information if any. The configuration is also saved in the local configuration store. The server must be online and accessible for the expansion to work.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The entry to create for the writer and node expansion configuration to use|[PublishedNodeExpansionModelPublishedNodesEntryRequestModel](definitions.md#publishednodeexpansionmodelpublishednodesentryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was created|[PublishedNodesEntryModelServiceResponseIAsyncEnumerable](definitions.md#publishednodesentrymodelserviceresponseiasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to update.|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="createorupdatedatasetwriterentry"></a>
#### CreateOrUpdateDataSetWriterEntry
```
PUT /v2/writer
```


##### Description
Create a published nodes entry for a specific writer group and dataset writer. The entry must specify a unique writer group and dataset writer id. A null value is treated as empty string. If the entry is found it is replaced, if it is not found, it is created. If more than one entry is found with the same writer group and writer id an error is returned. The writer entry provided must include at least one node which will be the initial set. All nodes must specify a unique dataSetFieldId. A null value is treated as empty string. Publishing intervals at node level are also not supported and generate an error. Publishing intervals must be configured at the data set writer level.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The entry to create for the writer|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was created|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to update.|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="createorupdateasset"></a>
#### CreateOrUpdateAsset
```
POST /v2/writer/assets/create
```


##### Description
Creates an asset from the entry in the request and the configuration provided in the Web of Things Asset configuration file. The entry must contain a data set name which will be used as the asset name. The writer can stay empty. It will be set to the asset id on successful return. The server must support the WoT profile per <see href="https://reference.opcfoundation.org/WoT/v100/docs/" />. The asset will be created and the configuration updated to reference it. A wait time can be provided as optional query parameter to wait until the server has settled after uploading the configuration.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The contains the entry and WoT file to configure the server to expose the asset.|[ByteArrayPublishedNodeCreateAssetRequestModel](definitions.md#bytearraypublishednodecreateassetrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The asset was created|[PublishedNodesEntryModelServiceResponse](definitions.md#publishednodesentrymodelserviceresponse)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|Forbidden|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="deleteasset"></a>
#### DeleteAsset
```
POST /v2/writer/assets/delete
```


##### Description
Delete the asset referenced by the entry in the request. The entry must contain the asset id to delete. The asset id is the data set writer id. The entry must also contain the writer group id or deletion of the asset in the configuration will fail before the asset is deleted. The server must support WoT connectivity profile per <see href="https://reference.opcfoundation.org/WoT/v100/docs/" />. First the entry in the configuration will be deleted and then the asset on the server. If deletion of the asset in the configuration fails it will not be deleted in the server. An optional request option force can be used to force the deletion of the asset in the server regardless of the failure to delete the entry in the configuration.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Request that contains the entry of the asset that should be deleted.|[PublishedNodeDeleteAssetRequestModel](definitions.md#publishednodedeleteassetrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The asset was deleted successfully|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|Forbidden|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getallassets"></a>
#### GetAllAssets
```
POST /v2/writer/assets/list
```


##### Description
Get a list of entries representing the assets in the server. This will not touch the configuration, it will obtain the list from the server. If the server does not support <see href="https://reference.opcfoundation.org/WoT/v100/docs/" /> the result will be empty.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The entry to use to list the assets with the optional header information used when invoking services on the server.|[RequestHeaderModelPublishedNodesEntryRequestModel](definitions.md#requestheadermodelpublishednodesentryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Successfully completed the listing|[PublishedNodesEntryModelServiceResponseIAsyncEnumerable](definitions.md#publishednodesentrymodelserviceresponseiasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|Forbidden|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="expandwriter"></a>
#### ExpandWriter
```
POST /v2/writer/expand
```


##### Description
Expands the provided nodes in the entry to a series of published node entries. The provided entry is used template. The entry is expanded using expansion configuration provided. Expanded entries are returned one by one with error information if any. The configuration is not updated but the resulting entries can be modified and later saved in the configuration using the configuration API. The server must be online and accessible for the expansion to work.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The entry to expand and the node expansion configuration to use. If no configuration is provided a default configuration is used which and no error entries are returned.|[PublishedNodeExpansionModelPublishedNodesEntryRequestModel](definitions.md#publishednodeexpansionmodelpublishednodesentryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was created|[PublishedNodesEntryModelServiceResponseIAsyncEnumerable](definitions.md#publishednodesentrymodelserviceresponseiasyncenumerable)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to update.|[ProblemDetails](definitions.md#problemdetails)|
|**408**|The operation timed out.|[ProblemDetails](definitions.md#problemdetails)|
|**500**|An unexpected error occurred|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getdatasetwriterentry"></a>
#### GetDataSetWriterEntry
```
GET /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}
```


##### Description
Get the published nodes entry for a specific writer group and dataset writer. Dedicated errors are returned if no, or no unique entry could be found. The entry does not contain the nodes. Nodes can be retrieved using the GetNodes API.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was found|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|There is no unique item present.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The item was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addorupdatenode"></a>
#### AddOrUpdateNode
```
PUT /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}
```


##### Description
Add a node to a dedicated data set writer in a writer group. A node must have a unique DataSetFieldId. If the field already exists, the node is updated. If a node does not have a dataset field id an error is returned. Publishing intervals at node level are also not supported and generate an error. Publishing intervals must be configured at the data set writer level.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|
|**Query**|**insertAfterFieldId**  <br>*optional*|Field after which to insert the nodes. If not specified, nodes are added at the end of the entry|string|
|**Body**|**body**  <br>*required*|Node to add or update|[OpcNodeModel](definitions.md#opcnodemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was added|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to update.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|An entry was not found to add the node to|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removedatasetwriterentry"></a>
#### RemoveDataSetWriterEntry
```
DELETE /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}
```


##### Description
Remove the published nodes entry for a specific data set writer in a writer group. Dedicated errors are returned if no, or no unique entry could be found.


##### Parameters

|Type|Name|Description|Schema|Default|
|---|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string||
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string||
|**Query**|**force**  <br>*optional*|Force delete all writers even if more than one were found. Does not error when none were found.|boolean|`"false"`|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The entry was removed|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to remove.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry to remove was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="addorupdatenodes"></a>
#### AddOrUpdateNodes
```
POST /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/add
```


##### Description
Add Nodes to a dedicated data set writer in a writer group. Each node must have a unique DataSetFieldId. If the field already exists, the node is updated. If a node does not have a dataset field id an error is returned. Publishing intervals at node level are also not supported and generate an error. Publishing intervals must be configured at the data set writer level.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|
|**Query**|**insertAfterFieldId**  <br>*optional*|Field after which to insert the nodes. If not specified, nodes are added at the end of the entry|string|
|**Body**|**body**  <br>*required*|Nodes to add or update|< [OpcNodeModel](definitions.md#opcnodemodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The items were added|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique entry could not be found to add to.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getnodes"></a>
#### GetNodes
```
GET /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes
```


##### Description
Get Nodes from a data set writer in a writer group. The nodes can optionally be offset from a previous last node identified by the dataSetFieldId and pageanated by the pageSize. If the dataSetFieldId is not found, an empty list is returned. If the dataSetFieldId is not specified, the first page is returned.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|
|**Query**|**lastDataSetFieldId**  <br>*optional*|the field id after which to start the page. If not specified, nodes from the beginning are returned.|string|
|**Query**|**pageSize**  <br>*optional*|Number of nodes to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The items were found|< [OpcNodeModel](definitions.md#opcnodemodel) > array|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to get nodes from.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removenodes"></a>
#### RemoveNodes
```
POST /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/remove
```


##### Description
Remove Nodes that match the provided data set field ids from a data set writer in a writer group. If one of the fields is not found, no error is returned, however, if all fields are not found an error is returned.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|
|**Body**|**body**  <br>*required*|The identifiers of the fields to remove|< string > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Some or all items were removed|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to remove from.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry or all items to remove were not found|[ProblemDetails](definitions.md#problemdetails)|


##### Consumes

* `application/json`
* `application/x-msgpack`


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="getnode"></a>
#### GetNode
```
GET /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}
```


##### Description
Get a node from a dataset in a writer group. Dedicated errors are returned if no, or no unique entry could be found, or the node does not exist.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetFieldId**  <br>*required*|The data set field id of the node to return|string|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was retrieved|[OpcNodeModel](definitions.md#opcnodemodel)|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to get a node from.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry or item was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`


<a name="removenode"></a>
#### RemoveNode
```
DELETE /v2/writer/{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}
```


##### Description
Remove a node with the specified data set field id from a data set writer in a writer group. If the field is not found, an error is returned.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**dataSetFieldId**  <br>*required*|Identifier of the field to remove|string|
|**Path**|**dataSetWriterGroup**  <br>*required*|The writer group name of the entry|string|
|**Path**|**dataSetWriterId**  <br>*required*|The data set writer identifer of the entry|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|The item was removed|No Content|
|**400**|The passed in information is invalid|[ProblemDetails](definitions.md#problemdetails)|
|**403**|A unique item could not be found to remove from.|[ProblemDetails](definitions.md#problemdetails)|
|**404**|The entry or item to remove was not found|[ProblemDetails](definitions.md#problemdetails)|


##### Produces

* `application/json`
* `application/x-msgpack`



