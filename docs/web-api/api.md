
<a name="paths"></a>
## Resources

<a name="applications_resource"></a>
### Applications
CRUD and Query application resources


<a name="registerserver"></a>
#### Register new server
```
POST /registry/v2/applications
```


##### Description
Registers a server solely using a discovery url. Requires that the onboarding agent service is running and the server can be located by a supervisor in its network using the discovery url.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Server registration request|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="getlistofapplications"></a>
#### Get list of applications
```
GET /registry/v2/applications
```


##### Description
Get all registered applications in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationInfoListModel](definitions.md#applicationinfolistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="createapplication"></a>
#### Create new application
```
PUT /registry/v2/applications
```


##### Description
The application is registered using the provided information, but it is not associated with a publisher. This is useful for when you need to register clients or you want to register a server that is located in a network not reachable through a publisher module.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Application registration request|[ApplicationRegistrationRequestModel](definitions.md#applicationregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationRegistrationResponseModel](definitions.md#applicationregistrationresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="deletealldisabledapplications"></a>
#### Purge applications
```
DELETE /registry/v2/applications
```


##### Description
Purges all applications that have not been seen for a specified amount of time.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**notSeenFor**  <br>*optional*|A duration in milliseconds|string (date-span)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="discoverserver"></a>
#### Discover servers
```
POST /registry/v2/applications/discover
```


##### Description
Registers servers by running a discovery scan in a supervisor's network. Requires that the onboarding agent service is running.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Discovery request|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
DELETE /registry/v2/applications/discover/{requestId}
```


##### Description
Cancels a discovery request using the request identifier.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|Discovery request|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="queryapplications"></a>
#### Query applications
```
POST /registry/v2/applications/query
```


##### Description
List applications that match a query model. The returned model can contain a continuation token if more results are available. Call the GetListOfApplications operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Application query|[ApplicationRegistrationQueryModel](definitions.md#applicationregistrationquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationInfoListModel](definitions.md#applicationinfolistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofapplications"></a>
#### Get filtered list of applications
```
GET /registry/v2/applications/query
```


##### Description
Get a list of applications filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfApplications operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Applications Query model|[ApplicationRegistrationQueryModel](definitions.md#applicationregistrationquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationInfoListModel](definitions.md#applicationinfolistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getlistofsites"></a>
#### Get list of sites
```
GET /registry/v2/applications/sites
```


##### Description
List all sites applications are registered in.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationSiteListModel](definitions.md#applicationsitelistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getapplicationregistration"></a>
#### Get application registration
```
GET /registry/v2/applications/{applicationId}
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|Application id for the server|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="deleteapplication"></a>
#### Unregister application
```
DELETE /registry/v2/applications/{applicationId}
```


##### Description
Unregisters and deletes application and all its associated endpoints.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|The identifier of the application|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="updateapplicationregistration"></a>
#### Update application registration
```
PATCH /registry/v2/applications/{applicationId}
```


##### Description
The application information is updated with new properties. Note that this information might be overridden if the application is re-discovered during a discovery run (recurring or one-time).


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|The identifier of the application|string|
|**Body**|**body**  <br>*required*|Application update request|[ApplicationRegistrationUpdateModel](definitions.md#applicationregistrationupdatemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="disableapplication"></a>
#### Disable an enabled application.
```
POST /registry/v2/applications/{applicationId}/disable
```


##### Description
A manager can disable an application.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|The application id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="enableapplication"></a>
#### Re-enable a disabled application.
```
POST /registry/v2/applications/{applicationId}/enable
```


##### Description
A manager can enable an application.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|The application id|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="discovery_resource"></a>
### Discovery
Discovery


<a name="getlistofdiscoverers"></a>
#### Get list of discoverers
```
GET /registry/v2/discovery
```


##### Description
Get all registered discoverers and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[DiscovererListModel](definitions.md#discovererlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="querydiscoverers"></a>
#### Query discoverers
```
POST /registry/v2/discovery/query
```


##### Description
Get all discoverers that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Discoverers query model|[DiscovererQueryModel](definitions.md#discovererquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[DiscovererListModel](definitions.md#discovererlistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofdiscoverers"></a>
#### Get filtered list of discoverers
```
GET /registry/v2/discovery/query
```


##### Description
Get a list of discoverers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**discovery**  <br>*optional*|Discovery mode of discoverer|enum (Off, Local, Network, Fast, Scan)|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site of the discoverer|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[DiscovererListModel](definitions.md#discovererlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="setdiscoverymode"></a>
#### Enable server discovery
```
POST /registry/v2/discovery/{discovererId}
```


##### Description
Allows a caller to configure recurring discovery runs on the discovery module identified by the module id.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|discoverer identifier|string|
|**Query**|**mode**  <br>*required*|Discovery mode|enum (Off, Local, Network, Fast, Scan)|
|**Body**|**body**  <br>*optional*|Discovery configuration|[DiscoveryConfigModel](definitions.md#discoveryconfigmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="getdiscoverer"></a>
#### Get discoverer registration information
```
GET /registry/v2/discovery/{discovererId}
```


##### Description
Returns a discoverer's registration and connectivity information. A discoverer id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|Discoverer identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[DiscovererModel](definitions.md#discoverermodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="updatediscoverer"></a>
#### Update discoverer information
```
PATCH /registry/v2/discovery/{discovererId}
```


##### Description
Allows a caller to configure recurring discovery runs on the twin module identified by the discoverer id or update site information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|discoverer identifier|string|
|**Body**|**body**  <br>*required*|Patch request|[DiscovererUpdateModel](definitions.md#discovererupdatemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="endpoints_resource"></a>
### Endpoints
Activate, Deactivate and Query endpoint resources


<a name="getlistofendpoints"></a>
#### Get list of endpoints
```
GET /registry/v2/endpoints
```


##### Description
Get all registered endpoints in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[EndpointInfoListModel](definitions.md#endpointinfolistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="registerendpoint"></a>
#### Register endpoint
```
PUT /registry/v2/endpoints
```


##### Description
Adds an endpoint. This will onboard the endpoint and the associated application but no other endpoints. This call is synchronous and will return successful if endpoint is found. Otherwise the call will fail with error not found.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|Query for the endpoint to register. This must have at least the discovery url. If more information is specified it is used to validate that the application has such endpoint and if not the call will fail.|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|string|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="queryendpoints"></a>
#### Query endpoints
```
POST /registry/v2/endpoints/query
```


##### Description
Return endpoints that match the specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfEndpoints operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Query to match|[EndpointRegistrationQueryModel](definitions.md#endpointregistrationquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[EndpointInfoListModel](definitions.md#endpointinfolistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofendpoints"></a>
#### Get filtered list of endpoints
```
GET /registry/v2/endpoints/query
```


##### Description
Get a list of endpoints filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfEndpoints operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**applicationId**  <br>*optional*|Application id to filter|string|
|**Query**|**certificate**  <br>*optional*|Certificate thumbprint of the endpoint|string|
|**Query**|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**Query**|**endpointState**  <br>*optional*|The last state of the activated endpoint|enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error, Disconnected, Unauthorized)|
|**Query**|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Query**|**securityMode**  <br>*optional*|Security mode to use for communication - null = Best|enum (Best, Sign, SignAndEncrypt, None, NotNone)|
|**Query**|**securityPolicy**  <br>*optional*|Endpoint security policy to use - null = Best.|string|
|**Query**|**siteOrGatewayId**  <br>*optional*|Site or gateway id to filter with|string|
|**Query**|**url**  <br>*optional*|Endoint url for direct server access|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[EndpointInfoListModel](definitions.md#endpointinfolistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpoint"></a>
#### Get endpoint information
```
GET /registry/v2/endpoints/{endpointId}
```


##### Description
Gets information about an endpoint.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[EndpointInfoModel](definitions.md#endpointinfomodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate chain
```
GET /registry/v2/endpoints/{endpointId}/certificate
```


##### Description
Gets current certificate of the endpoint.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test endpoint is accessible
```
POST /registry/v2/endpoints/{endpointId}/test
```


##### Description
Test an endpoint can be connected to. Returns error information if connecting fails.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|
|**Body**|**body**  <br>*required*||[TestConnectionRequestModel](definitions.md#testconnectionrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="events_resource"></a>
### Events
Configure discovery events


<a name="subscribebyrequestid"></a>
#### Subscribe to discovery progress for a request
```
PUT /events/v2/discovery/requests/{requestId}/events
```


##### Description
Register a client to receive discovery progress events through SignalR for a particular request.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The request to monitor|string|
|**Body**|**body**  <br>*optional*|The connection that will receive discovery events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="unsubscribebyrequestid"></a>
#### Unsubscribe from discovery progress for a request.
```
DELETE /events/v2/discovery/requests/{requestId}/events/{connectionId}
```


##### Description
Unregister a client and stop it from receiving discovery events for a particular request.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**connectionId**  <br>*required*|The connection that will not receive any more discovery progress|string|
|**Path**|**requestId**  <br>*required*|The request to unsubscribe from|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="subscribebydiscovererid"></a>
#### Subscribe to discovery progress from discoverer
```
PUT /events/v2/discovery/{discovererId}/events
```


##### Description
Register a client to receive discovery progress events through SignalR from a particular discoverer.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|The discoverer to subscribe to|string|
|**Body**|**body**  <br>*optional*|The connection that will receive discovery events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="unsubscribebydiscovererid"></a>
#### Unsubscribe from discovery progress from discoverer.
```
DELETE /events/v2/discovery/{discovererId}/events/{connectionId}
```


##### Description
Unregister a client and stop it from receiving discovery events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**connectionId**  <br>*required*|The connection that will not receive any more discovery progress|string|
|**Path**|**discovererId**  <br>*required*|The discoverer to unsubscribe from|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="gateways_resource"></a>
### Gateways
Read, Update and Query Gateway resources


<a name="getlistofgateway"></a>
#### Get list of Gateways
```
GET /registry/v2/gateways
```


##### Description
Get all registered Gateways and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[GatewayListModel](definitions.md#gatewaylistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="querygateway"></a>
#### Query Gateways
```
POST /registry/v2/gateways/query
```


##### Description
Get all Gateways that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfGateway operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Gateway query model|[GatewayQueryModel](definitions.md#gatewayquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[GatewayListModel](definitions.md#gatewaylistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofgateway"></a>
#### Get filtered list of Gateways
```
GET /registry/v2/gateways/query
```


##### Description
Get a list of Gateways filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfGateway operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site of the Gateway|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[GatewayListModel](definitions.md#gatewaylistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getgateway"></a>
#### Get Gateway registration information
```
GET /registry/v2/gateways/{GatewayId}
```


##### Description
Returns a Gateway's registration and connectivity information. A Gateway id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**GatewayId**  <br>*required*|Gateway identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[GatewayInfoModel](definitions.md#gatewayinfomodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="updategateway"></a>
#### Update Gateway configuration
```
PATCH /registry/v2/gateways/{GatewayId}
```


##### Description
Allows a caller to configure operations on the Gateway module identified by the Gateway id.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**GatewayId**  <br>*required*|Gateway identifier|string|
|**Body**|**body**  <br>*required*|Patch request|[GatewayUpdateModel](definitions.md#gatewayupdatemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="history_resource"></a>
### History
History raw access services


<a name="gethistoryservercapabilities"></a>
#### Get the history server capabilities
```
GET /history/v2/capabilities/{endpointId}
```


##### Description
Gets the capabilities of the connected historian server. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**namespaceFormat**  <br>*optional*||enum (Uri, Index, Expanded)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historydeleteevents"></a>
#### Delete historic events
```
POST /history/v2/delete/{endpointId}/events
```


##### Description
Delete historic events using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteEventsDetailsModelHistoryUpdateRequestModel](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Delete historic values
```
POST /history/v2/delete/{endpointId}/values
```


##### Description
Delete historic values using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteValuesDetailsModelHistoryUpdateRequestModel](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Delete historic values
```
POST /history/v2/delete/{endpointId}/values/modified
```


##### Description
Delete historic values using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteValuesDetailsModelHistoryUpdateRequestModel](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Delete value history at specified times
```
POST /history/v2/delete/{endpointId}/values/pick
```


##### Description
Delete value history using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModel](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadraw"></a>
#### Read history using json details
```
POST /history/v2/history/read/{endpointId}
```


##### Description
Read node history if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[VariantValueHistoryReadRequestModel](definitions.md#variantvaluehistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadrawnext"></a>
#### Read next batch of history as json
```
POST /history/v2/history/read/{endpointId}/next
```


##### Description
Read next batch of node history values using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestModel](definitions.md#historyreadnextrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdateraw"></a>
#### Update node history using raw json
```
POST /history/v2/history/update/{endpointId}
```


##### Description
Update node history using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[VariantValueHistoryUpdateRequestModel](definitions.md#variantvaluehistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Insert historic events
```
POST /history/v2/insert/{endpointId}/events
```


##### Description
Insert historic events using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history insert request|[UpdateEventsDetailsModelHistoryUpdateRequestModel](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Insert historic values
```
POST /history/v2/insert/{endpointId}/values
```


##### Description
Insert historic values using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history insert request|[UpdateValuesDetailsModelHistoryUpdateRequestModel](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Get history node configuration
```
POST /history/v2/read/{endpointId}/configuration
```


##### Description
Read history node configuration if available. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*optional*|The history configuration read request|[HistoryConfigurationRequestModel](definitions.md#historyconfigurationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read historic events
```
POST /history/v2/read/{endpointId}/events
```


##### Description
Read historic events of a node if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadEventsDetailsModelHistoryReadRequestModel](definitions.md#readeventsdetailsmodelhistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read next batch of historic events
```
POST /history/v2/read/{endpointId}/events/next
```


##### Description
Read next batch of historic events of a node using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestModel](definitions.md#historyreadnextrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read historic processed values at specified times
```
POST /history/v2/read/{endpointId}/values
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadValuesDetailsModelHistoryReadRequestModel](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read historic modified values at specified times
```
POST /history/v2/read/{endpointId}/values/modified
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadModifiedValuesDetailsModelHistoryReadRequestModel](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluenext"></a>
#### Read next batch of historic values
```
POST /history/v2/read/{endpointId}/values/next
```


##### Description
Read next batch of historic values of a node using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestModel](definitions.md#historyreadnextrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read historic values at specified times
```
POST /history/v2/read/{endpointId}/values/pick
```


##### Description
Read historic values of a node if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadValuesAtTimesDetailsModelHistoryReadRequestModel](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Read historic processed values at specified times
```
POST /history/v2/read/{endpointId}/values/processed
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadProcessedValuesDetailsModelHistoryReadRequestModel](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Replace historic events
```
POST /history/v2/replace/{endpointId}/events
```


##### Description
Replace historic events using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history replace request|[UpdateEventsDetailsModelHistoryUpdateRequestModel](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Replace historic values
```
POST /history/v2/replace/{endpointId}/values
```


##### Description
Replace historic values using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history replace request|[UpdateValuesDetailsModelHistoryUpdateRequestModel](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Upsert historic events
```
POST /history/v2/upsert/{endpointId}/events
```


##### Description
Upsert historic events using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history upsert request|[UpdateEventsDetailsModelHistoryUpdateRequestModel](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Upsert historic values
```
POST /history/v2/upsert/{endpointId}/values
```


##### Description
Upsert historic values using historic access. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history upsert request|[UpdateValuesDetailsModelHistoryUpdateRequestModel](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publish_resource"></a>
### Publish
Value and Event publishing services


<a name="getfirstlistofpublishednodes"></a>
#### Get currently published nodes
```
POST /publisher/v2/publish/{endpointId}
```


##### Description
Returns currently published node ids for an endpoint. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The list request|[PublishedItemListRequestModel](definitions.md#publisheditemlistrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getnextlistofpublishednodes"></a>
#### Get next set of published nodes
```
GET /publisher/v2/publish/{endpointId}
```


##### Description
Returns next set of currently published node ids for an endpoint. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|The continuation token to continue with|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="bulkpublishvalues"></a>
#### Bulk publish node values
```
POST /publisher/v2/publish/{endpointId}/bulk
```


##### Description
Adds or removes in bulk values that should be published from a particular endpoint.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of a registered endpoint.|string|
|**Body**|**body**  <br>*required*|The bulk publish request|[PublishBulkRequestModel](definitions.md#publishbulkrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="startpublishingvalues"></a>
#### Start publishing node values
```
POST /publisher/v2/publish/{endpointId}/start
```


##### Description
Start publishing variable node values to IoT Hub. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The publish request|[PublishStartRequestModel](definitions.md#publishstartrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="stoppublishingvalues"></a>
#### Stop publishing node values
```
POST /publisher/v2/publish/{endpointId}/stop
```


##### Description
Stop publishing variable node values to IoT Hub. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The unpublish request|[PublishStopRequestModel](definitions.md#publishstoprequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishers_resource"></a>
### Publishers
Read, Update and Query publisher resources


<a name="getlistofpublisher"></a>
#### Get list of publishers
```
GET /registry/v2/publishers
```


##### Description
Get all registered publishers and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublisherListModel](definitions.md#publisherlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="querypublisher"></a>
#### Query publishers
```
POST /registry/v2/publishers/query
```


##### Description
Get all publishers that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfPublisher operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Publisher query model|[PublisherQueryModel](definitions.md#publisherquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublisherListModel](definitions.md#publisherlistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofpublisher"></a>
#### Get filtered list of publishers
```
GET /registry/v2/publishers/query
```


##### Description
Get a list of publishers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfPublisher operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site for the supervisors|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublisherListModel](definitions.md#publisherlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getpublisher"></a>
#### Get publisher registration information
```
GET /registry/v2/publishers/{publisherId}
```


##### Description
Returns a publisher's registration and connectivity information. A publisher id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**publisherId**  <br>*required*|Publisher identifier|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublisherModel](definitions.md#publishermodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="updatepublisher"></a>
#### Update publisher configuration
```
PATCH /registry/v2/publishers/{publisherId}
```


##### Description
Allows a caller to configure operations on the publisher module identified by the publisher id.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**publisherId**  <br>*required*|Publisher identifier|string|
|**Body**|**body**  <br>*required*|Patch request|[PublisherUpdateModel](definitions.md#publisherupdatemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="getconfiguredendpoints"></a>
#### Get configured endpoints
```
GET /registry/v2/publishers/{publisherId}/endpoints
```


##### Description
Get all configured endpoints on the publisher. These are the ones configured in the local storage of the publisher.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**publisherId**  <br>*required*||string|
|**Query**|**IncludeNodes**  <br>*optional*|Include nodes that make up the configuration|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[PublishedNodesEntryModelIAsyncEnumerable](definitions.md#publishednodesentrymodeliasyncenumerable)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="setconfiguredendpoints"></a>
#### Set configured endpoints
```
PUT /registry/v2/publishers/{publisherId}/endpoints
```


##### Description
Set all configured endpoints on the publisher. These are the ones that will be written to local storage of the publisher.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**publisherId**  <br>*required*|string|
|**Body**|**body**  <br>*required*|[SetConfiguredEndpointsRequestModel](definitions.md#setconfiguredendpointsrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="supervisors_resource"></a>
### Supervisors
Read, Update and Query publisher resources


<a name="getlistofsupervisors"></a>
#### Get list of supervisors
```
GET /registry/v2/supervisors
```


##### Description
Get all registered supervisors and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[SupervisorListModel](definitions.md#supervisorlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="querysupervisors"></a>
#### Query supervisors
```
POST /registry/v2/supervisors/query
```


##### Description
Get all supervisors that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfSupervisors operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**body**  <br>*required*|Supervisors query model|[SupervisorQueryModel](definitions.md#supervisorquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[SupervisorListModel](definitions.md#supervisorlistmodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getfilteredlistofsupervisors"></a>
#### Get filtered list of supervisors
```
GET /registry/v2/supervisors/query
```


##### Description
Get a list of supervisors filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfSupervisors operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**endpointId**  <br>*optional*|Managing provided endpoint twin|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site for the supervisors|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[SupervisorListModel](definitions.md#supervisorlistmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getsupervisor"></a>
#### Get supervisor registration information
```
GET /registry/v2/supervisors/{supervisorId}
```


##### Description
Returns a supervisor's registration and connectivity information. A supervisor id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|Supervisor identifier|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[SupervisorModel](definitions.md#supervisormodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="updatesupervisor"></a>
#### Update supervisor information
```
PATCH /registry/v2/supervisors/{supervisorId}
```


##### Description
Allows a caller to configure recurring discovery runs on the twin module identified by the supervisor id or update site information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|supervisor identifier|string|
|**Body**|**body**  <br>*required*|Patch request|[SupervisorUpdateModel](definitions.md#supervisorupdatemodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="telemetry_resource"></a>
### Telemetry
Value and Event monitoring services


<a name="subscribe"></a>
#### Subscribe to receive samples
```
PUT /events/v2/telemetry/{endpointId}/samples
```


##### Description
Register a client to receive publisher samples through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The endpoint to subscribe to|string|
|**Body**|**body**  <br>*optional*|The connection that will receive publisher samples.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="unsubscribe"></a>
#### Unsubscribe from receiving samples.
```
DELETE /events/v2/telemetry/{endpointId}/samples/{connectionId}
```


##### Description
Unregister a client and stop it from receiving samples.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**connectionId**  <br>*required*|The connection that will not receive any more published samples|string|
|**Path**|**endpointId**  <br>*required*|The endpoint to unsubscribe from|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|No Content|


<a name="twin_resource"></a>
### Twin
Node access read services


<a name="browse"></a>
#### Browse node references
```
POST /twin/v2/browse/{endpointId}
```


##### Description
Browse a node on the specified endpoint. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The browse request|[BrowseFirstRequestModel](definitions.md#browsefirstrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getsetofuniquenodes"></a>
#### Browse set of unique target nodes
```
GET /twin/v2/browse/{endpointId}
```


##### Description
Browse the set of unique hierarchically referenced target nodes on the endpoint. The endpoint must be in the registry and the server accessible. The root node id to browse from can be provided as part of the query parameters. If it is not provided, the RootFolder node is browsed. Note that this is the same as the POST method with the model containing the node id and the targetNodesOnly flag set to true.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**nodeId**  <br>*optional*|The node to browse or omit to browse the root node (i=84)|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next set of references
```
POST /twin/v2/browse/{endpointId}/next
```


##### Description
Browse next set of references on the endpoint. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The request body with continuation token.|[BrowseNextRequestModel](definitions.md#browsenextrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getnextsetofuniquenodes"></a>
#### Browse next set of unique target nodes
```
GET /twin/v2/browse/{endpointId}/next
```


##### Description
Browse the next set of unique hierarchically referenced target nodes on the endpoint. The endpoint must be in the registry and the server accessible. Note that this is the same as the POST method with the model containing the continuation token and the targetNodesOnly flag set to true.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|Continuation token from GetSetOfUniqueNodes operation|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browseusingpath"></a>
#### Browse using a browse path
```
POST /twin/v2/browse/{endpointId}/path
```


##### Description
Browse using a path from the specified node id. This call uses TranslateBrowsePathsToNodeIds service under the hood. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The browse path request|[BrowsePathRequestModel](definitions.md#browsepathrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="callmethod"></a>
#### Call a method
```
POST /twin/v2/call/{endpointId}
```


##### Description
Invoke method node with specified input arguments. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The method call request|[MethodCallRequestModel](definitions.md#methodcallrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getcallmetadata"></a>
#### Get method meta data
```
POST /twin/v2/call/{endpointId}/metadata
```


##### Description
(Obsolete - use GetMetadata API) Return method meta data to support a user interface displaying forms to input and output arguments. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The method metadata request|[MethodMetadataRequestModel](definitions.md#methodmetadatarequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

* `application/json-patch+json`
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
#### Get the server capabilities
```
GET /twin/v2/capabilities/{endpointId}
```


##### Description
Gets the capabilities of the connected server. The endpoint must be in the registry and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**namespaceFormat**  <br>*optional*||enum (Uri, Index, Expanded)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get metadata of a node
```
POST /twin/v2/metadata/{endpointId}/node
```


##### Description
Get the node metadata which includes the fields and meta data of the type and can be used when constructing event filters or calling methods to pass the correct arguments. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The metadata request|[NodeMetadataRequestModel](definitions.md#nodemetadatarequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="readvalue"></a>
#### Read variable value
```
POST /twin/v2/read/{endpointId}
```


##### Description
Read a variable node's value. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The read value request|[ValueReadRequestModel](definitions.md#valuereadrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getvalue"></a>
#### Get variable value
```
GET /twin/v2/read/{endpointId}
```


##### Description
Get a variable node's value using its node id. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**nodeId**  <br>*required*|The node to read|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="readattributes"></a>
#### Read node attributes
```
POST /twin/v2/read/{endpointId}/attributes
```


##### Description
Read attributes of a node. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The read request|[ReadRequestModel](definitions.md#readrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="writevalue"></a>
#### Write variable value
```
POST /twin/v2/write/{endpointId}
```


##### Description
Write variable node's value. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The write value request|[ValueWriteRequestModel](definitions.md#valuewriterequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="writeattributes"></a>
#### Write node attributes
```
POST /twin/v2/write/{endpointId}/attributes
```


##### Description
Write any attribute of a node. The endpoint must be in the registry and the server accessible.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The batch write request|[WriteRequestModel](definitions.md#writerequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|OK|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`



