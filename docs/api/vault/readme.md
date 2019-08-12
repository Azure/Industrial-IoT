# Opc-Vault-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Vault Service


### Version information
*Version* : v2


### URI scheme
*Schemes* : HTTPS, HTTP


### Tags

* Certificates : Certificate services.
* Distribution : Certificate CRL Distribution Point and Authority
Information Access
services.
* Requests : Certificate request services.
* Status : Status checks
* TrustGroups : Trust group services.
* TrustLists : Trust lists services.




<a name="paths"></a>
## Resources

<a name="certificates_resource"></a>
### Certificates
Certificate services.


<a name="getissuercertificatechain"></a>
#### Get Issuer CA Certificate chain.
```
GET /v2/certificates/{serialNumber}
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**serialNumber**  <br>*required*|the serial number of the<br>            Issuer CA Certificate|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainApiModel](definitions.md#x509certificatechainapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getissuercrlchain"></a>
#### Get Issuer CA CRL chain.
```
GET /v2/certificates/{serialNumber}/crl
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**serialNumber**  <br>*required*|the serial number of the Issuer<br>            CA Certificate|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CrlChainApiModel](definitions.md#x509crlchainapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="distribution_resource"></a>
### Distribution
Certificate CRL Distribution Point and Authority
Information Access
services.


<a name="getissuercrlchain"></a>
#### Get Issuer CRL in CRL Distribution Endpoint.
```
GET /v2/crl/{serialNumber}
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**serialNumber**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/pkix-crl`


<a name="getissuercertificatechain"></a>
#### Get Issuer Certificate for Authority Information Access endpoint.
```
GET /v2/issuer/{serialNumber}
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**serialNumber**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/pkix-cert`


<a name="requests_resource"></a>
### Requests
Certificate request services.


<a name="listrequests"></a>
#### Lists certificate requests.
```
GET /v2/requests
```


##### Description
Get all certificate requests in paged form or continue a current listing or
query.
The returned model can contain a link to the next page if more results are
available.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**nextPageLink**  <br>*optional*|optional, link to next page|string|
|**Query**|**pageSize**  <br>*optional*|optional, the maximum number of result per page|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[CertificateRequestQueryResponseApiModel](definitions.md#certificaterequestqueryresponseapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="startnewkeypairrequest"></a>
#### Create a certificate request with a new key pair.
```
PUT /v2/requests/keypair
```


##### Description
The request is in the 'New' state after this call.
Requires Writer or Manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**newKeyPairRequest**  <br>*required*|The new key pair request parameters|[StartNewKeyPairRequestApiModel](definitions.md#startnewkeypairrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StartNewKeyPairRequestResponseApiModel](definitions.md#startnewkeypairrequestresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="finishnewkeypairrequest"></a>
#### Fetch certificate request result.
```
GET /v2/requests/keypair/{requestId}
```


##### Description
Can be called in any state.
Fetches private key in 'Completed' state.
After a successful fetch in 'Completed' state, the request is
moved into 'Accepted' state.
Requires Writer role.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**requestId**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[FinishNewKeyPairRequestResponseApiModel](definitions.md#finishnewkeypairrequestresponseapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="queryrequests"></a>
#### Query for certificate requests.
```
POST /v2/requests/query
```


##### Description
Get all certificate requests in paged form.
The returned model can contain a link to the next page if more results are
available.  Use ListRequests to continue.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|optional, the maximum number of result per page|integer (int32)|
|**Body**|**query**  <br>*optional*|optional, query filter|[CertificateRequestQueryRequestApiModel](definitions.md#certificaterequestqueryrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[CertificateRequestQueryResponseApiModel](definitions.md#certificaterequestqueryresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="startsigningrequest"></a>
#### Create a certificate request with a certificate signing request (CSR).
```
PUT /v2/requests/sign
```


##### Description
The request is in the 'New' state after this call.
Requires Writer or Manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**signingRequest**  <br>*required*|The signing request parameters|[StartSigningRequestApiModel](definitions.md#startsigningrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StartSigningRequestResponseApiModel](definitions.md#startsigningrequestresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="finishsigningrequest"></a>
#### Fetch signing request results.
```
GET /v2/requests/sign/{requestId}
```


##### Description
Can be called in any state.
After a successful fetch in 'Completed' state, the request is
moved into 'Accepted' state.
Requires Writer role.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**requestId**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[FinishSigningRequestResponseApiModel](definitions.md#finishsigningrequestresponseapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getrequest"></a>
#### Get a specific certificate request.
```
GET /v2/requests/{requestId}
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The certificate request id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[CertificateRequestRecordApiModel](definitions.md#certificaterequestrecordapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="deleterequest"></a>
#### Delete request. Physically delete the request.
```
DELETE /v2/requests/{requestId}
```


##### Description
By purging the request it is actually physically deleted from the
database, including the public key and other information.
Requires Manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The certificate request id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="acceptrequest"></a>
#### Cancel request
```
POST /v2/requests/{requestId}/accept
```


##### Description
The request is in the 'Accepted' state after this call.
Requires Writer role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The certificate request id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="approverequest"></a>
#### Approve the certificate request.
```
POST /v2/requests/{requestId}/approve
```


##### Description
Validates the request with the application database.
- If Approved:
  - New Key Pair request: Creates the new key pair
        in the requested format, signs the certificate and stores the
        private key for later securely in KeyVault.
  - Cert Signing Request: Creates and signs the certificate.
        Deletes the CSR from the database.
 Stores the signed certificate for later use in the Database.
 The request is in the 'Approved' or 'Rejected' state after this call.
 Requires Approver role.
 Approver needs signing rights in KeyVault.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The certificate request id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="rejectrequest"></a>
#### Reject the certificate request.
```
POST /v2/requests/{requestId}/reject
```


##### Description
The request is in the 'Rejected' state after this call.
Requires Approver role.
Approver needs signing rights in KeyVault.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The certificate request id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="status_resource"></a>
### Status
Status checks


<a name="getstatus"></a>
#### Return the service status in the form of the service status api model.
```
GET /v2/status
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StatusResponseApiModel](definitions.md#statusresponseapimodel)|


##### Produces

* `application/json`


<a name="trustgroups_resource"></a>
### TrustGroups
Trust group services.


<a name="listgroups"></a>
#### Get information about all groups.
```
GET /v2/groups
```


##### Description
A trust group has a root certificate which issues certificates
to entities.  Entities can be part of a trust group and thus
trust the root certificate and all entities that the root has
issued certificates for.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**nextPageLink**  <br>*optional*|optional, link to next page|string|
|**Query**|**pageSize**  <br>*optional*|optional, the maximum number of result per page|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TrustGroupRegistrationListApiModel](definitions.md#trustgroupregistrationlistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="creategroup"></a>
#### Create new sub-group of an existing group.
```
PUT /v2/groups
```


##### Description
Requires manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**request**  <br>*required*|The create request|[TrustGroupRegistrationRequestApiModel](definitions.md#trustgroupregistrationrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TrustGroupRegistrationResponseApiModel](definitions.md#trustgroupregistrationresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="createroot"></a>
#### Create new root group.
```
PUT /v2/groups/root
```


##### Description
Requires manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**request**  <br>*required*|The create request|[TrustGroupRootCreateRequestApiModel](definitions.md#trustgrouprootcreaterequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TrustGroupRegistrationResponseApiModel](definitions.md#trustgroupregistrationresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updategroup"></a>
#### Update group registration.
```
POST /v2/groups/{groupId}
```


##### Description
Use this function with care and only if you are aware of
the security implications.
Requires manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**groupId**  <br>*required*|The group id|string|
|**Body**|**request**  <br>*required*|The group configuration|[TrustGroupUpdateRequestApiModel](definitions.md#trustgroupupdaterequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getgroup"></a>
#### Get group information.
```
GET /v2/groups/{groupId}
```


##### Description
A trust group has a root certificate which issues certificates
to entities.  Entities can be part of a trust group and thus
trust the root certificate and all entities that the root has
issued certificates for.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**groupId**  <br>*required*|The group id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TrustGroupRegistrationApiModel](definitions.md#trustgroupregistrationapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="deletegroup"></a>
#### Delete a group.
```
DELETE /v2/groups/{groupId}
```


##### Description
After this operation the Issuer CA, CRLs and keys become inaccessible.
Use this function with extreme caution.
Requires manager role.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**groupId**  <br>*required*|The group id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="renewissuercertificate"></a>
#### Renew a group CA Certificate.
```
POST /v2/groups/{groupId}/renew
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**groupId**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="trustlists_resource"></a>
### TrustLists
Trust lists services.


<a name="listtrustedcertificates"></a>
#### List trusted certificates
```
GET /v2/trustlists/{entityId}
```


##### Description
Returns all certificates the entity should trust based on the
applied trust configuration.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**entityId**  <br>*required*||string|
|**Query**|**nextPageLink**  <br>*optional*|optional, link to next page|string|
|**Query**|**pageSize**  <br>*optional*|optional, the maximum number of result per page|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateListApiModel](definitions.md#x509certificatelistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="addtrustrelationship"></a>
#### Add trust relationship
```
PUT /v2/trustlists/{entityId}/{trustedEntityId}
```


##### Description
Define trust between two entities.  The entities are identifiers
of application, groups, or endpoints.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**entityId**  <br>*required*|The entity identifier, e.g. group, etc.|string|
|**Path**|**trustedEntityId**  <br>*required*|The trusted entity identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="removetrustrelationship"></a>
#### Remove a trust relationship
```
DELETE /v2/trustlists/{entityId}/{untrustedEntityId}
```


##### Description
Removes trust between two entities.  The entities are identifiers
of application, groups, or endpoints.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**entityId**  <br>*required*|The entity identifier, e.g. group, etc.|string|
|**Path**|**untrustedEntityId**  <br>*required*|The trusted entity identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|



