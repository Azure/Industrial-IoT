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
* Discoverers : Configure discovery
* Endpoints : Activate, Deactivate and Query endpoint resources
* Gateways : Read, Update and Query Gateway resources
* Publishers : Read, Update and Query publisher resources
* Status : Status checks
* Supervisors : Read, Update and Query supervisor resources




<a name="paths"></a>
## Resources

<a name="applications_resource"></a>
### Applications
CRUD and Query application resources


<a name="registerserver"></a>
#### Register new server
```
POST /v2/applications
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getlistofapplications"></a>
#### Get list of applications
```
GET /v2/applications
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="createapplication"></a>
#### Create new application
```
PUT /v2/applications
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="deletealldisabledapplications"></a>
#### Purge applications
```
DELETE /v2/applications
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="discoverserver"></a>
#### Discover servers
```
POST /v2/applications/discover
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="cancel"></a>
#### Cancel discovery
```
DELETE /v2/applications/discover/{requestId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe for application events
```
PUT /v2/applications/events
```


##### Description
Register a client to receive application events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user that will receive application events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe from application events
```
DELETE /v2/applications/events/{userId}
```


##### Description
Unregister a user and stop it from receiving events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="queryapplications"></a>
#### Query applications
```
POST /v2/applications/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofapplications"></a>
#### Get filtered list of applications
```
GET /v2/applications/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="queryapplicationsbyid"></a>
#### Query applications by id.
```
POST /v2/applications/querybyid
```


##### Description
A query model which supports the OPC UA Global Discovery Server query.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ApplicationRecordQueryApiModel](definitions.md#applicationrecordqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRecordListApiModel](definitions.md#applicationrecordlistapimodel)|


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


<a name="getlistofsites"></a>
#### Get list of sites
```
GET /v2/applications/sites
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getapplicationregistration"></a>
#### Get application registration
```
GET /v2/applications/{applicationId}
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="deleteapplication"></a>
#### Unregister application
```
DELETE /v2/applications/{applicationId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updateapplicationregistration"></a>
#### Update application registration
```
PATCH /v2/applications/{applicationId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="disableapplication"></a>
#### Disable an enabled application.
```
POST /v2/applications/{applicationId}/disable
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="enableapplication"></a>
#### Re-enable a disabled application.
```
POST /v2/applications/{applicationId}/enable
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="discoverers_resource"></a>
### Discoverers
Configure discovery


<a name="getlistofdiscoverers"></a>
#### Get list of discoverers
```
GET /v2/discovery
```


##### Description
Get all registered discoverers and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[DiscovererListApiModel](definitions.md#discovererlistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe to discoverer registry events
```
PUT /v2/discovery/events
```


##### Description
Register a user to receive discoverer events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user id that will receive discoverer events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe registry events
```
DELETE /v2/discovery/events/{userId}
```


##### Description
Unregister a user and stop it from receiving discoverer events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more discoverer events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="querydiscoverers"></a>
#### Query discoverers
```
POST /v2/discovery/query
```


##### Description
Get all discoverers that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofdiscoverers"></a>
#### Get filtered list of discoverers
```
GET /v2/discovery/query
```


##### Description
Get a list of discoverers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**discovery**  <br>*optional*|Discovery mode of discoverer|enum (Off, Local, Network, Fast, Scan)|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site of the discoverer|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[DiscovererListApiModel](definitions.md#discovererlistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribebyrequestid"></a>
#### Subscribe to discovery progress for a request
```
PUT /v2/discovery/requests/{requestId}/events
```


##### Description
Register a client to receive discovery progress events through SignalR for a particular request.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The request to monitor|string|
|**Body**|**body**  <br>*optional*|The user id that will receive discovery events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribebyrequestid"></a>
#### Unsubscribe from discovery progress for a request.
```
DELETE /v2/discovery/requests/{requestId}/events/{userId}
```


##### Description
Unregister a client and stop it from receiving discovery events for a particular request.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**requestId**  <br>*required*|The request to unsubscribe from|string|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more discovery progress|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="setdiscoverymode"></a>
#### Enable server discovery
```
POST /v2/discovery/{discovererId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getdiscoverer"></a>
#### Get discoverer registration information
```
GET /v2/discovery/{discovererId}
```


##### Description
Returns a discoverer's registration and connectivity information. A discoverer id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|Discoverer identifier|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[DiscovererApiModel](definitions.md#discovererapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updatediscoverer"></a>
#### Update discoverer information
```
PATCH /v2/discovery/{discovererId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribebydiscovererid"></a>
#### Subscribe to discovery progress from discoverer
```
PUT /v2/discovery/{discovererId}/events
```


##### Description
Register a client to receive discovery progress events through SignalR from a particular discoverer.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|The discoverer to subscribe to|string|
|**Body**|**body**  <br>*optional*|The user id that will receive discovery events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribebydiscovererid"></a>
#### Unsubscribe from discovery progress from discoverer.
```
DELETE /v2/discovery/{discovererId}/events/{userId}
```


##### Description
Unregister a client and stop it from receiving discovery events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**discovererId**  <br>*required*|The discoverer to unsubscribe from|string|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more discovery progress|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="endpoints_resource"></a>
### Endpoints
Activate, Deactivate and Query endpoint resources


<a name="getlistofendpoints"></a>
#### Get list of endpoints
```
GET /v2/endpoints
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe for endpoint events
```
PUT /v2/endpoints/events
```


##### Description
Register a user to receive endpoint events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user id that will receive endpoint events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe from endpoint events
```
DELETE /v2/endpoints/events/{userId}
```


##### Description
Unregister a user and stop it from receiving endpoint events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more endpoint events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="queryendpoints"></a>
#### Query endpoints
```
POST /v2/endpoints/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofendpoints"></a>
#### Get filtered list of endpoints
```
GET /v2/endpoints/query
```


##### Description
Get a list of endpoints filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfEndpoints operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**activated**  <br>*optional*|Whether the endpoint was activated|boolean|
|**Query**|**applicationId**  <br>*optional*|Application id to filter|string|
|**Query**|**certificate**  <br>*optional*|Certificate of the endpoint|string (byte)|
|**Query**|**connected**  <br>*optional*|Whether the endpoint is connected on supervisor.|boolean|
|**Query**|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**Query**|**endpointState**  <br>*optional*|The last state of the the activated endpoint|enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error)|
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getendpoint"></a>
#### Get endpoint information
```
GET /v2/endpoints/{endpointId}
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="activateendpoint"></a>
#### Activate endpoint
```
POST /v2/endpoints/{endpointId}/activate
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="deactivateendpoint"></a>
#### Deactivate endpoint
```
POST /v2/endpoints/{endpointId}/deactivate
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="gateways_resource"></a>
### Gateways
Read, Update and Query Gateway resources


<a name="getlistofgateway"></a>
#### Get list of Gateways
```
GET /v2/gateways
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe to Gateway registry events
```
PUT /v2/gateways/events
```


##### Description
Register a user to receive Gateway events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user id that will receive Gateway events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe registry events
```
DELETE /v2/gateways/events/{userId}
```


##### Description
Unregister a user and stop it from receiving Gateway events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more Gateway events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="querygateway"></a>
#### Query Gateways
```
POST /v2/gateways/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofgateway"></a>
#### Get filtered list of Gateways
```
GET /v2/gateways/query
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getgateway"></a>
#### Get Gateway registration information
```
GET /v2/gateways/{GatewayId}
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updategateway"></a>
#### Update Gateway configuration
```
PATCH /v2/gateways/{GatewayId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="publishers_resource"></a>
### Publishers
Read, Update and Query publisher resources


<a name="getlistofpublisher"></a>
#### Get list of publishers
```
GET /v2/publishers
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe to publisher registry events
```
PUT /v2/publishers/events
```


##### Description
Register a user to receive publisher events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user id that will receive publisher events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe registry events
```
DELETE /v2/publishers/events/{userId}
```


##### Description
Unregister a user and stop it from receiving publisher events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more publisher events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="querypublisher"></a>
#### Query publishers
```
POST /v2/publishers/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofpublisher"></a>
#### Get filtered list of publishers
```
GET /v2/publishers/query
```


##### Description
Get a list of publishers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfPublisher operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site of the publisher|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublisherListApiModel](definitions.md#publisherlistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getpublisher"></a>
#### Get publisher registration information
```
GET /v2/publishers/{publisherId}
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updatepublisher"></a>
#### Update publisher configuration
```
PATCH /v2/publishers/{publisherId}
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


<a name="supervisors_resource"></a>
### Supervisors
Read, Update and Query supervisor resources


<a name="getlistofsupervisors"></a>
#### Get list of supervisors
```
GET /v2/supervisors
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="subscribe"></a>
#### Subscribe to supervisor registry events
```
PUT /v2/supervisors/events
```


##### Description
Register a user to receive supervisor events through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The user id that will receive supervisor events.|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="unsubscribe"></a>
#### Unsubscribe registry events
```
DELETE /v2/supervisors/events/{userId}
```


##### Description
Unregister a user and stop it from receiving supervisor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**userId**  <br>*required*|The user id that will not receive any more supervisor events|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="querysupervisors"></a>
#### Query supervisors
```
POST /v2/supervisors/query
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


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getfilteredlistofsupervisors"></a>
#### Get filtered list of supervisors
```
GET /v2/supervisors/query
```


##### Description
Get a list of supervisors filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfSupervisors operation using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Query**|**siteId**  <br>*optional*|Site of the supervisor|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[SupervisorListApiModel](definitions.md#supervisorlistapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getsupervisor"></a>
#### Get supervisor registration information
```
GET /v2/supervisors/{supervisorId}
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="updatesupervisor"></a>
#### Update supervisor information
```
PATCH /v2/supervisors/{supervisorId}
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="resetsupervisor"></a>
#### Reset supervisor
```
POST /v2/supervisors/{supervisorId}/reset
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


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="getsupervisorstatus"></a>
#### Get runtime status of supervisor
```
GET /v2/supervisors/{supervisorId}/status
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

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|



