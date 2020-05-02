# Opc-Registry-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Registry Service


### Version information
*Version* : v2


### License information
*License* : MIT LICENSE  
*License URL* : https://opensource.org/licenses/MIT  
*Terms of service* : null


### URI scheme
*Host* : localhost:9080  
*Schemes* : HTTP, HTTPS


### Tags

* Applications : CRUD and Query application resources
* Discovery : Configure discovery
* Endpoints : Activate, Deactivate and Query endpoint resources
* Gateways : Read, Update and Query Gateway resources
* Publishers : Read, Update and Query publisher resources
* Supervisors : Read, Update and Query supervisor resources




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
|**Body**|**body**  <br>*required*|Server registration request|[ServerRegistrationRequestApiModel](definitions.md#serverregistrationrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|[ApplicationInfoListApiModel](definitions.md#applicationinfolistapimodel)|


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
The application is registered using the provided information, but it is not associated with a supervisor. This is useful for when you need to register clients or you want to register a server that is located in a network not reachable through a Twin module.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*required*|Application registration request|[ApplicationRegistrationRequestApiModel](definitions.md#applicationregistrationrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationResponseApiModel](definitions.md#applicationregistrationresponseapimodel)|


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
|**200**|Success|No Content|


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
|**Body**|**body**  <br>*required*|Discovery request|[DiscoveryRequestApiModel](definitions.md#discoveryrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**Body**|**body**  <br>*required*|Application query|[ApplicationRegistrationQueryApiModel](definitions.md#applicationregistrationqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationInfoListApiModel](definitions.md#applicationinfolistapimodel)|


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
|**Body**|**body**  <br>*required*|Applications Query model|[ApplicationRegistrationQueryApiModel](definitions.md#applicationregistrationqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationInfoListApiModel](definitions.md#applicationinfolistapimodel)|


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
|**200**|Success|[ApplicationSiteListApiModel](definitions.md#applicationsitelistapimodel)|


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
|**200**|Success|[ApplicationRegistrationApiModel](definitions.md#applicationregistrationapimodel)|


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
|**200**|Success|No Content|


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
|**Body**|**body**  <br>*required*|Application update request|[ApplicationRegistrationUpdateApiModel](definitions.md#applicationregistrationupdateapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


<a name="discovery_resource"></a>
### Discovery
Configure discovery


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
|**200**|Success|[DiscovererListApiModel](definitions.md#discovererlistapimodel)|


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
|**Body**|**body**  <br>*required*|Discoverers query model|[DiscovererQueryApiModel](definitions.md#discovererqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[DiscovererListApiModel](definitions.md#discovererlistapimodel)|


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
|**200**|Success|[DiscovererListApiModel](definitions.md#discovererlistapimodel)|


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
|**Body**|**body**  <br>*optional*|Discovery configuration|[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|[DiscovererApiModel](definitions.md#discovererapimodel)|


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
|**Body**|**body**  <br>*required*|Patch request|[DiscovererUpdateApiModel](definitions.md#discovererupdateapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|[EndpointInfoListApiModel](definitions.md#endpointinfolistapimodel)|


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
|**Body**|**body**  <br>*required*|Query to match|[EndpointRegistrationQueryApiModel](definitions.md#endpointregistrationqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[EndpointInfoListApiModel](definitions.md#endpointinfolistapimodel)|


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
|**Query**|**activated**  <br>*optional*|Whether the endpoint was activated|boolean|
|**Query**|**applicationId**  <br>*optional*|Application id to filter|string|
|**Query**|**certificate**  <br>*optional*|Endpoint certificate thumbprint|string|
|**Query**|**connected**  <br>*optional*|Whether the endpoint is connected on supervisor.|boolean|
|**Query**|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**Query**|**endpointState**  <br>*optional*|The last state of the the activated endpoint|enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error, Disconnected, Unauthorized)|
|**Query**|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Query**|**securityMode**  <br>*optional*|Security Mode|enum (Best, Sign, SignAndEncrypt, None)|
|**Query**|**securityPolicy**  <br>*optional*|Security policy uri|string|
|**Query**|**siteOrGatewayId**  <br>*optional*|Site or gateway id to filter with|string|
|**Query**|**supervisorId**  <br>*optional*|Supervisor id to filter with|string|
|**Query**|**url**  <br>*optional*|Endoint url for direct server access|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[EndpointInfoListApiModel](definitions.md#endpointinfolistapimodel)|


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
|**200**|Success|[EndpointInfoApiModel](definitions.md#endpointinfoapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="activateendpoint"></a>
#### Activate endpoint
```
POST /registry/v2/endpoints/{endpointId}/activate
```


##### Description
Activates an endpoint for subsequent use in twin service. All endpoints must be activated using this API or through a activation filter during application registration or discovery.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|[X509CertificateChainApiModel](definitions.md#x509certificatechainapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="deactivateendpoint"></a>
#### Deactivate endpoint
```
POST /registry/v2/endpoints/{endpointId}/deactivate
```


##### Description
Deactivates the endpoint and disable access through twin service.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


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
|**200**|Success|[GatewayListApiModel](definitions.md#gatewaylistapimodel)|


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
|**Body**|**body**  <br>*required*|Gateway query model|[GatewayQueryApiModel](definitions.md#gatewayqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GatewayListApiModel](definitions.md#gatewaylistapimodel)|


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
|**200**|Success|[GatewayListApiModel](definitions.md#gatewaylistapimodel)|


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
|**200**|Success|[GatewayInfoApiModel](definitions.md#gatewayinfoapimodel)|


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
|**Body**|**body**  <br>*required*|Patch request|[GatewayUpdateApiModel](definitions.md#gatewayupdateapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
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
|**200**|Success|[PublisherListApiModel](definitions.md#publisherlistapimodel)|


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
|**Body**|**body**  <br>*required*|Publisher query model|[PublisherQueryApiModel](definitions.md#publisherqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublisherListApiModel](definitions.md#publisherlistapimodel)|


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
|**Query**|**siteId**  <br>*optional*|Site for the publishers|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublisherListApiModel](definitions.md#publisherlistapimodel)|


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
|**200**|Success|[PublisherApiModel](definitions.md#publisherapimodel)|


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
|**Body**|**body**  <br>*required*|Patch request|[PublisherUpdateApiModel](definitions.md#publisherupdateapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="supervisors_resource"></a>
### Supervisors
Read, Update and Query supervisor resources


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
|**200**|Success|[SupervisorListApiModel](definitions.md#supervisorlistapimodel)|


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
|**Body**|**body**  <br>*required*|Supervisors query model|[SupervisorQueryApiModel](definitions.md#supervisorqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[SupervisorListApiModel](definitions.md#supervisorlistapimodel)|


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
|**200**|Success|[SupervisorListApiModel](definitions.md#supervisorlistapimodel)|


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
|**200**|Success|[SupervisorApiModel](definitions.md#supervisorapimodel)|


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
|**Body**|**body**  <br>*required*|Patch request|[SupervisorUpdateApiModel](definitions.md#supervisorupdateapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="resetsupervisor"></a>
#### Reset supervisor
```
POST /registry/v2/supervisors/{supervisorId}/reset
```


##### Description
Allows a caller to reset the twin module using its supervisor identity identifier.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|supervisor identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


<a name="getsupervisorstatus"></a>
#### Get runtime status of supervisor
```
GET /registry/v2/supervisors/{supervisorId}/status
```


##### Description
Allows a caller to get runtime status for a supervisor.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|supervisor identifier|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[SupervisorStatusApiModel](definitions.md#supervisorstatusapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`



