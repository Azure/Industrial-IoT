# Opc-Registry


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Registry Service


### Version information
*Version* : v1


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
POST /v1/applications
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications
```


###### Request body
```json
{
  "discoveryUrl" : "string",
  "id" : "string",
  "callback" : {
    "uri" : "string",
    "method" : "string",
    "authenticationHeader" : "string"
  },
  "activationFilter" : {
    "trustLists" : [ "string" ],
    "securityPolicies" : [ "string" ],
    "securityMode" : "string"
  }
}
```


<a name="getlistofapplications"></a>
#### Get list of applications
```
GET /v1/applications
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/applications
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "applicationId" : "string",
    "applicationType" : "Server",
    "applicationUri" : "string",
    "productUri" : "string",
    "applicationName" : "string",
    "locale" : "en",
    "certificate" : "string",
    "capabilities" : "LDS",
    "discoveryUrls" : [ "string" ],
    "discoveryProfileUri" : "string",
    "hostAddresses" : [ "string" ],
    "siteId" : "productionlineA",
    "supervisorId" : "string",
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="createapplication"></a>
#### Create new application
```
PUT /v1/applications
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications
```


###### Request body
```json
{
  "applicationUri" : "string",
  "applicationType" : "Server",
  "productUri" : "http://contoso.com/fridge/1.0",
  "applicationName" : "string",
  "locale" : "en",
  "capabilities" : "LDS",
  "discoveryUrls" : [ "string" ],
  "discoveryProfileUri" : "string"
}
```


##### Example HTTP response

###### Response 200
```json
{
  "id" : "string"
}
```


<a name="deletealldisabledapplications"></a>
#### Purge applications
```
DELETE /v1/applications
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/applications
```


<a name="discoverserver"></a>
#### Discover servers
```
POST /v1/applications/discover
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications/discover
```


###### Request body
```json
{
  "id" : "string",
  "discovery" : "string",
  "configuration" : {
    "addressRangesToScan" : "string",
    "networkProbeTimeoutMs" : 0,
    "maxNetworkProbes" : 0,
    "portRangesToScan" : "string",
    "portProbeTimeoutMs" : 0,
    "maxPortProbes" : 0,
    "minPortProbesPercent" : 0,
    "idleTimeBetweenScansSec" : 0,
    "discoveryUrls" : [ "string" ],
    "locales" : [ "string" ],
    "callbacks" : [ {
      "uri" : "string",
      "method" : "string",
      "authenticationHeader" : "string"
    } ],
    "activationFilter" : {
      "trustLists" : [ "string" ],
      "securityPolicies" : [ "string" ],
      "securityMode" : "string"
    }
  }
}
```


<a name="queryapplications"></a>
#### Query applications
```
POST /v1/applications/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications/query
```


###### Request body
```json
{
  "applicationType" : "string",
  "applicationUri" : "string",
  "productUri" : "string",
  "applicationName" : "string",
  "locale" : "string",
  "capability" : "string",
  "siteOrSupervisorId" : "string",
  "includeNotSeenSince" : true
}
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "applicationId" : "string",
    "applicationType" : "Server",
    "applicationUri" : "string",
    "productUri" : "string",
    "applicationName" : "string",
    "locale" : "en",
    "certificate" : "string",
    "capabilities" : "LDS",
    "discoveryUrls" : [ "string" ],
    "discoveryProfileUri" : "string",
    "hostAddresses" : [ "string" ],
    "siteId" : "productionlineA",
    "supervisorId" : "string",
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="getfilteredlistofapplications"></a>
#### Get filtered list of applications
```
GET /v1/applications/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications/query
```


###### Request body
```json
{
  "applicationType" : "string",
  "applicationUri" : "string",
  "productUri" : "string",
  "applicationName" : "string",
  "locale" : "string",
  "capability" : "string",
  "siteOrSupervisorId" : "string",
  "includeNotSeenSince" : true
}
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "applicationId" : "string",
    "applicationType" : "Server",
    "applicationUri" : "string",
    "productUri" : "string",
    "applicationName" : "string",
    "locale" : "en",
    "certificate" : "string",
    "capabilities" : "LDS",
    "discoveryUrls" : [ "string" ],
    "discoveryProfileUri" : "string",
    "hostAddresses" : [ "string" ],
    "siteId" : "productionlineA",
    "supervisorId" : "string",
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="getlistofsites"></a>
#### Get list of sites
```
GET /v1/applications/sites
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/applications/sites
```


##### Example HTTP response

###### Response 200
```json
{
  "sites" : [ "string" ],
  "continuationToken" : "string"
}
```


<a name="getapplicationregistration"></a>
#### Get application registration
```
GET /v1/applications/{applicationId}
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**applicationId**  <br>*required*|Application id for the server|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationApiModel](definitions.md#applicationregistrationapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/applications/string
```


##### Example HTTP response

###### Response 200
```json
{
  "application" : {
    "applicationId" : "string",
    "applicationType" : "Server",
    "applicationUri" : "string",
    "productUri" : "string",
    "applicationName" : "string",
    "locale" : "en",
    "certificate" : "string",
    "capabilities" : "LDS",
    "discoveryUrls" : [ "string" ],
    "discoveryProfileUri" : "string",
    "hostAddresses" : [ "string" ],
    "siteId" : "productionlineA",
    "supervisorId" : "string",
    "notSeenSince" : "string"
  },
  "endpoints" : [ {
    "id" : "string",
    "siteId" : "string",
    "endpoint" : {
      "url" : "string",
      "user" : {
        "type" : "string",
        "value" : "object"
      },
      "securityMode" : "string",
      "securityPolicy" : "string",
      "serverThumbprint" : "string"
    },
    "securityLevel" : 0,
    "certificate" : "string",
    "authenticationMethods" : [ {
      "id" : "string",
      "credentialType" : "string",
      "securityPolicy" : "string",
      "configuration" : "object"
    } ]
  } ],
  "securityAssessment" : "string"
}
```


<a name="deleteapplication"></a>
#### Unregister application
```
DELETE /v1/applications/{applicationId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/applications/string
```


<a name="updateapplicationregistration"></a>
#### Update application registration
```
PATCH /v1/applications/{applicationId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/applications/string
```


###### Request body
```json
{
  "productUri" : "string",
  "applicationName" : "string",
  "locale" : "string",
  "certificate" : "string",
  "capabilities" : [ "string" ],
  "discoveryUrls" : [ "string" ],
  "discoveryProfileUri" : "string"
}
```


<a name="endpoints_resource"></a>
### Endpoints
Activate, Deactivate and Query endpoint resources


<a name="getlistofendpoints"></a>
#### Get list of endpoints
```
GET /v1/endpoints
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/endpoints
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "registration" : {
      "id" : "string",
      "siteId" : "string",
      "endpoint" : {
        "url" : "string",
        "user" : {
          "type" : "string",
          "value" : "object"
        },
        "securityMode" : "string",
        "securityPolicy" : "string",
        "serverThumbprint" : "string"
      },
      "securityLevel" : 0,
      "certificate" : "string",
      "authenticationMethods" : [ {
        "id" : "string",
        "credentialType" : "string",
        "securityPolicy" : "string",
        "configuration" : "object"
      } ]
    },
    "applicationId" : "string",
    "activated" : true,
    "connected" : true,
    "outOfSync" : true,
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="queryendpoints"></a>
#### Query endpoints
```
POST /v1/endpoints/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/endpoints/query
```


###### Request body
```json
{
  "url" : "string",
  "userAuthentication" : "string",
  "certificate" : "string",
  "securityMode" : "string",
  "securityPolicy" : "string",
  "activated" : true,
  "connected" : true,
  "includeNotSeenSince" : true
}
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "registration" : {
      "id" : "string",
      "siteId" : "string",
      "endpoint" : {
        "url" : "string",
        "user" : {
          "type" : "string",
          "value" : "object"
        },
        "securityMode" : "string",
        "securityPolicy" : "string",
        "serverThumbprint" : "string"
      },
      "securityLevel" : 0,
      "certificate" : "string",
      "authenticationMethods" : [ {
        "id" : "string",
        "credentialType" : "string",
        "securityPolicy" : "string",
        "configuration" : "object"
      } ]
    },
    "applicationId" : "string",
    "activated" : true,
    "connected" : true,
    "outOfSync" : true,
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="getfilteredlistofendpoints"></a>
#### Get filtered list of endpoints
```
GET /v1/endpoints/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/endpoints/query
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "registration" : {
      "id" : "string",
      "siteId" : "string",
      "endpoint" : {
        "url" : "string",
        "user" : {
          "type" : "string",
          "value" : "object"
        },
        "securityMode" : "string",
        "securityPolicy" : "string",
        "serverThumbprint" : "string"
      },
      "securityLevel" : 0,
      "certificate" : "string",
      "authenticationMethods" : [ {
        "id" : "string",
        "credentialType" : "string",
        "securityPolicy" : "string",
        "configuration" : "object"
      } ]
    },
    "applicationId" : "string",
    "activated" : true,
    "connected" : true,
    "outOfSync" : true,
    "notSeenSince" : "string"
  } ],
  "continuationToken" : "string"
}
```


<a name="getendpoint"></a>
#### Get endpoint information
```
GET /v1/endpoints/{endpointId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/endpoints/string
```


##### Example HTTP response

###### Response 200
```json
{
  "registration" : {
    "id" : "string",
    "siteId" : "string",
    "endpoint" : {
      "url" : "string",
      "user" : {
        "type" : "string",
        "value" : "object"
      },
      "securityMode" : "string",
      "securityPolicy" : "string",
      "serverThumbprint" : "string"
    },
    "securityLevel" : 0,
    "certificate" : "string",
    "authenticationMethods" : [ {
      "id" : "string",
      "credentialType" : "string",
      "securityPolicy" : "string",
      "configuration" : "object"
    } ]
  },
  "applicationId" : "string",
  "activated" : true,
  "connected" : true,
  "outOfSync" : true,
  "notSeenSince" : "string"
}
```


<a name="updateendpoint"></a>
#### Update endpoint information
```
PATCH /v1/endpoints/{endpointId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/endpoints/string
```


###### Request body
```json
{
  "user" : {
    "type" : "string",
    "value" : "object"
  }
}
```


<a name="activateendpoint"></a>
#### Activate endpoint
```
POST /v1/endpoints/{endpointId}/activate
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/endpoints/string/activate
```


<a name="deactivateendpoint"></a>
#### Deactivate endpoint
```
POST /v1/endpoints/{endpointId}/deactivate
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/endpoints/string/deactivate
```


<a name="status_resource"></a>
### Status
Status checks


<a name="getstatus"></a>
#### Return the service status in the form of the service status api model.
```
GET /v1/status
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StatusResponseApiModel](definitions.md#statusresponseapimodel)|


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/status
```


##### Example HTTP response

###### Response 200
```json
{
  "name" : "string",
  "status" : "string",
  "currentTime" : "string",
  "startTime" : "string",
  "upTime" : 0,
  "uid" : "string",
  "properties" : {
    "string" : "string"
  },
  "dependencies" : {
    "string" : "string"
  },
  "$metadata" : {
    "string" : "string"
  }
}
```


<a name="supervisors_resource"></a>
### Supervisors
Read, Update and Query supervisor resources


<a name="getlistofsupervisors"></a>
#### Get list of supervisors
```
GET /v1/supervisors
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/supervisors
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "id" : "string",
    "siteId" : "string",
    "discovery" : "string",
    "discoveryConfig" : {
      "addressRangesToScan" : "string",
      "networkProbeTimeoutMs" : 0,
      "maxNetworkProbes" : 0,
      "portRangesToScan" : "string",
      "portProbeTimeoutMs" : 0,
      "maxPortProbes" : 0,
      "minPortProbesPercent" : 0,
      "idleTimeBetweenScansSec" : 0,
      "discoveryUrls" : [ "string" ],
      "locales" : [ "string" ],
      "callbacks" : [ {
        "uri" : "string",
        "method" : "string",
        "authenticationHeader" : "string"
      } ],
      "activationFilter" : {
        "trustLists" : [ "string" ],
        "securityPolicies" : [ "string" ],
        "securityMode" : "string"
      }
    },
    "certificate" : "string",
    "outOfSync" : true,
    "connected" : true
  } ],
  "continuationToken" : "string"
}
```


<a name="querysupervisors"></a>
#### Query supervisors
```
POST /v1/supervisors/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/supervisors/query
```


###### Request body
```json
{
  "siteId" : "string",
  "discovery" : "string",
  "connected" : true
}
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "id" : "string",
    "siteId" : "string",
    "discovery" : "string",
    "discoveryConfig" : {
      "addressRangesToScan" : "string",
      "networkProbeTimeoutMs" : 0,
      "maxNetworkProbes" : 0,
      "portRangesToScan" : "string",
      "portProbeTimeoutMs" : 0,
      "maxPortProbes" : 0,
      "minPortProbesPercent" : 0,
      "idleTimeBetweenScansSec" : 0,
      "discoveryUrls" : [ "string" ],
      "locales" : [ "string" ],
      "callbacks" : [ {
        "uri" : "string",
        "method" : "string",
        "authenticationHeader" : "string"
      } ],
      "activationFilter" : {
        "trustLists" : [ "string" ],
        "securityPolicies" : [ "string" ],
        "securityMode" : "string"
      }
    },
    "certificate" : "string",
    "outOfSync" : true,
    "connected" : true
  } ],
  "continuationToken" : "string"
}
```


<a name="getfilteredlistofsupervisors"></a>
#### Get filtered list of supervisors
```
GET /v1/supervisors/query
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/supervisors/query
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "id" : "string",
    "siteId" : "string",
    "discovery" : "string",
    "discoveryConfig" : {
      "addressRangesToScan" : "string",
      "networkProbeTimeoutMs" : 0,
      "maxNetworkProbes" : 0,
      "portRangesToScan" : "string",
      "portProbeTimeoutMs" : 0,
      "maxPortProbes" : 0,
      "minPortProbesPercent" : 0,
      "idleTimeBetweenScansSec" : 0,
      "discoveryUrls" : [ "string" ],
      "locales" : [ "string" ],
      "callbacks" : [ {
        "uri" : "string",
        "method" : "string",
        "authenticationHeader" : "string"
      } ],
      "activationFilter" : {
        "trustLists" : [ "string" ],
        "securityPolicies" : [ "string" ],
        "securityMode" : "string"
      }
    },
    "certificate" : "string",
    "outOfSync" : true,
    "connected" : true
  } ],
  "continuationToken" : "string"
}
```


<a name="getsupervisor"></a>
#### Get supervisor registration information
```
GET /v1/supervisors/{supervisorId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/supervisors/string
```


##### Example HTTP response

###### Response 200
```json
{
  "id" : "string",
  "siteId" : "string",
  "discovery" : "string",
  "discoveryConfig" : {
    "addressRangesToScan" : "string",
    "networkProbeTimeoutMs" : 0,
    "maxNetworkProbes" : 0,
    "portRangesToScan" : "string",
    "portProbeTimeoutMs" : 0,
    "maxPortProbes" : 0,
    "minPortProbesPercent" : 0,
    "idleTimeBetweenScansSec" : 0,
    "discoveryUrls" : [ "string" ],
    "locales" : [ "string" ],
    "callbacks" : [ {
      "uri" : "string",
      "method" : "string",
      "authenticationHeader" : "string"
    } ],
    "activationFilter" : {
      "trustLists" : [ "string" ],
      "securityPolicies" : [ "string" ],
      "securityMode" : "string"
    }
  },
  "certificate" : "string",
  "outOfSync" : true,
  "connected" : true
}
```


<a name="updatesupervisor"></a>
#### Update supervisor information
```
PATCH /v1/supervisors/{supervisorId}
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
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/supervisors/string
```


###### Request body
```json
{
  "siteId" : "string",
  "discovery" : "string",
  "discoveryConfig" : {
    "addressRangesToScan" : "string",
    "networkProbeTimeoutMs" : 0,
    "maxNetworkProbes" : 0,
    "portRangesToScan" : "string",
    "portProbeTimeoutMs" : 0,
    "maxPortProbes" : 0,
    "minPortProbesPercent" : 0,
    "idleTimeBetweenScansSec" : 0,
    "discoveryUrls" : [ "string" ],
    "locales" : [ "string" ],
    "callbacks" : [ {
      "uri" : "string",
      "method" : "string",
      "authenticationHeader" : "string"
    } ],
    "activationFilter" : {
      "trustLists" : [ "string" ],
      "securityPolicies" : [ "string" ],
      "securityMode" : "string"
    }
  },
  "discoveryCallbacks" : [ {
    "uri" : "string",
    "method" : "string",
    "authenticationHeader" : "string"
  } ],
  "removeDiscoveryCallbacks" : true
}
```



