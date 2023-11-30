
<a name="paths"></a>
## Resources

<a name="certificates_resource"></a>
### Certificates
<br>
            This section lists the certificate APi provided by OPC Publisher providing
            all public and private key infrastructure (PKI) related API methods.
            
<br>
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
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
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
|**Path**|**store**  <br>*required*|The store to add the certificate to|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**Path**|**store**  <br>*required*|The store to enumerate|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [X509CertificateModel](definitions.md#x509certificatemodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**Path**|**store**  <br>*required*|The store to add the certificate to|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|
|**Query**|**password**  <br>*optional*|The optional password of the pfx|string|
|**Body**|**body**  <br>*required*|The pfx encoded certificate.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
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
|**Path**|**store**  <br>*required*|The store to add the certificate to|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|
|**Path**|**thumbprint**  <br>*required*|The thumbprint of the certificate to delete.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**Path**|**store**  <br>*required*|The store to enumerate|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< string (byte) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**Path**|**store**  <br>*required*|The store to add the certificate to|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**Path**|**store**  <br>*required*|The store to add the certificate to|enum (Application, Rejected, Trusted, Https, User, Issuer, HttpsIssuer, UserIssuer)|
|**Body**|**body**  <br>*required*|The pfx encoded certificate.|string (byte)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="configuration_resource"></a>
### Configuration
<br>
            This section contains the API to configure OPC Publisher.
            
<br>
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
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
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
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishbulk"></a>
#### PublishBulk
```
POST /v2/configuration/bulk
```


##### Description
Configure node values to publish and unpublish in bulk. The group field in the Connection Model can be used to specify a writer group identifier that will be used in the configuration entry that is created from it inside the OPC Publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The nodes to publish or unpublish.|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### PublishStart
```
POST /v2/configuration/start
```


##### Description
Start publishing values from a node on a server. The group field in the Connection Model can be used to specify a writer group identifier that will be used in the configuration entry that is created from it inside the OPC Publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The server and node to publish.|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="discovery_resource"></a>
### Discovery
<br>OPC UA and network discovery related API.
<br>
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
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="general_resource"></a>
### General
<br>
            This section lists the general APi provided by OPC Publisher providing
            all connection, endpoint and address space related API methods.
            
<br>
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
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Description
Connect to a server using the provided connection request information. The connection is established and held active until cancelled or the publisher process is restarted.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Description
Can be used to disconnect a previously established connection.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|The request payload and connection information identifying the server to connect to perform the operation on.|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
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
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="history_resource"></a>
### History
<br>
            This section lists all OPC UA HDA or Historian related API provided by
            OPC Publisher.
            
<br>
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
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
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`



