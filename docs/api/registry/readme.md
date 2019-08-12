# Opc-Registry-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Registry Service


### Version information
*Version* : v2


### URI scheme
*Schemes* : HTTPS, HTTP


### Tags

* Applications : CRUD and Query application resources
* Endpoints : Activate, Deactivate and Query endpoint resources
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
Registers a server solely using a discovery url. Requires that
the onboarding agent service is running and the server can be
located by a supervisor in its network using the discovery url.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**request**  <br>*required*|Server registration request|[ServerRegistrationRequestApiModel](definitions.md#serverregistrationrequestapimodel)|


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


<a name="getlistofapplications"></a>
#### Get list of applications
```
GET /v2/applications
```


##### Description
Get all registered applications in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation<br>            token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to<br>            return|integer (int32)|


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
The application is registered using the provided information, but it
is not associated with a supervisor.  This is useful for when you need
to register clients or you want to register a server that is located
in a network not reachable through a Twin module.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**request**  <br>*required*|Application registration request|[ApplicationRegistrationRequestApiModel](definitions.md#applicationregistrationrequestapimodel)|


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
|**Query**|**notSeenFor**  <br>*optional*|A duration in milliseconds|string|


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


<a name="discoverserver"></a>
#### Discover servers
```
POST /v2/applications/discover
```


##### Description
Registers servers by running a discovery scan in a supervisor's
network. Requires that the onboarding agent service is running.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**request**  <br>*required*|Discovery request|[DiscoveryRequestApiModel](definitions.md#discoveryrequestapimodel)|


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


<a name="queryapplications"></a>
#### Query applications
```
POST /v2/applications/query
```


##### Description
List applications that match a query model.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfApplications operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to<br>            return|integer (int32)|
|**Body**|**query**  <br>*required*|Application query|[ApplicationRegistrationQueryApiModel](definitions.md#applicationregistrationqueryapimodel)|


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
Get a list of applications filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfApplications operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**query**  <br>*required*|Applications Query model|[ApplicationRegistrationQueryApiModel](definitions.md#applicationregistrationqueryapimodel)|


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
|**Body**|**query**  <br>*optional*|[ApplicationRecordQueryApiModel](definitions.md#applicationrecordqueryapimodel)|


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
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation<br>            token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to<br>            return|integer (int32)|


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


##### Produces

* `application/json`


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
The application information is updated with new properties.  Note that
this information might be overridden if the application is re-discovered
during a discovery run (recurring or one-time).


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|The identifier of the application|string|
|**Body**|**request**  <br>*required*|Application update request|[ApplicationRegistrationUpdateApiModel](definitions.md#applicationregistrationupdateapimodel)|


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


##### Produces

* `application/json`


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


##### Produces

* `application/json`


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
Get all registered endpoints in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if available|boolean|
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


<a name="queryendpoints"></a>
#### Query endpoints
```
POST /v2/endpoints/query
```


##### Description
Return endpoints that match the specified query.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfEndpoints operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Body**|**query**  <br>*required*|Query to match|[EndpointRegistrationQueryApiModel](definitions.md#endpointregistrationqueryapimodel)|


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
Get a list of endpoints filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfEndpoints operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**Activated**  <br>*optional*|Whether the endpoint was activated|boolean|
|**Query**|**Certificate**  <br>*optional*|Certificate of the endpoint|string (byte)|
|**Query**|**Connected**  <br>*optional*|Whether the endpoint is connected on supervisor.|boolean|
|**Query**|**EndpointState**  <br>*optional*|The last state of the the activated endpoint|enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error)|
|**Query**|**IncludeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**Query**|**SecurityMode**  <br>*optional*|Security Mode|enum (Best, Sign, SignAndEncrypt, None)|
|**Query**|**SecurityPolicy**  <br>*optional*|Security policy uri|string|
|**Query**|**Url**  <br>*optional*|Endoint url for direct server access|string|
|**Query**|**UserAuthentication**  <br>*optional*|Type of credential selected for authentication|enum (None, UserName, X509Certificate, JwtToken)|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server state, or display<br>            current client state of the endpoint if available|boolean|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to<br>            return|integer (int32)|


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
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if<br>            available|boolean|


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


<a name="updateendpoint"></a>
#### Update endpoint information
```
PATCH /v2/endpoints/{endpointId}
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|
|**Body**|**request**  <br>*required*|Endpoint update request|[EndpointRegistrationUpdateApiModel](definitions.md#endpointregistrationupdateapimodel)|


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


<a name="activateendpoint"></a>
#### Activate endpoint
```
POST /v2/endpoints/{endpointId}/activate
```


##### Description
Activates an endpoint for subsequent use in twin service.
All endpoints must be activated using this API or through a
activation filter during application registration or discovery.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|endpoint identifier|string|


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


<a name="supervisors_resource"></a>
### Supervisors
Read, Update and Query supervisor resources


<a name="getlistofsupervisors"></a>
#### Get list of supervisors
```
GET /v2/supervisors
```


##### Description
Get all registered supervisors and therefore twin modules in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if available|boolean|
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


<a name="querysupervisors"></a>
#### Query supervisors
```
POST /v2/supervisors/query
```


##### Description
Get all supervisors that match a specified query.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfSupervisors operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if<br>            available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|
|**Body**|**query**  <br>*required*|Supervisors query model|[SupervisorQueryApiModel](definitions.md#supervisorqueryapimodel)|


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
Get a list of supervisors filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfSupervisors operation using the token to retrieve
more results.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**Connected**  <br>*optional*|Included connected or disconnected|boolean|
|**Query**|**Discovery**  <br>*optional*|Discovery mode of supervisor|enum (Off, Local, Network, Fast, Scan)|
|**Query**|**SiteId**  <br>*optional*|Site of the supervisor|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if<br>            available|boolean|
|**Query**|**pageSize**  <br>*optional*|Number of results to return|integer (int32)|


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
Returns a supervisor's registration and connectivity information.
A supervisor id corresponds to the twin modules module identity.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|Supervisor identifier|string|
|**Query**|**onlyServerState**  <br>*optional*|Whether to include only server<br>            state, or display current client state of the endpoint if<br>            available|boolean|


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
Allows a caller to configure recurring discovery runs on the twin module
identified by the supervisor id or update site information.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|supervisor identifier|string|
|**Body**|**request**  <br>*required*|Patch request|[SupervisorUpdateApiModel](definitions.md#supervisorupdateapimodel)|


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


<a name="resetsupervisor"></a>
#### Reset supervisor
```
POST /v2/supervisors/{supervisorId}/reset
```


##### Description
Allows a caller to reset the twin module using its supervisor
identity identifier.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**supervisorId**  <br>*required*|supervisor identifier|string|


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



