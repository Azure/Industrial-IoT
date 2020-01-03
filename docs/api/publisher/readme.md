# Opc-Publisher-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Publisher Service


### Version information
*Version* : v2


### URI scheme
*BasePath* : /publisher  
*Schemes* : HTTPS, HTTP


### Tags

* Monitor : Value and Event monitoring services
* Publish : Value and Event publishing services
* Status : Status checks




<a name="paths"></a>
## Resources

<a name="monitor_resource"></a>
### Monitor
Value and Event monitoring services


<a name="subscribe"></a>
#### Subscribe to receive samples
```
PUT /v2/monitor/{endpointId}/samples
```


##### Description
Register a client to receive publisher samples through SignalR.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The endpoint to subscribe to|string|
|**Body**|**userId**  <br>*optional*|The user id that will receive publisher<br>            samples.|string|


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


<a name="unsubscribe"></a>
#### Unsubscribe from receiving samples.
```
DELETE /v2/monitor/{endpointId}/samples/{userId}
```


##### Description
Unregister a client and stop it from receiving samples.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The endpoint to unsubscribe from|string|
|**Path**|**userId**  <br>*required*|The user id that will not receive<br>            any more published samples|string|


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


<a name="publish_resource"></a>
### Publish
Value and Event publishing services


<a name="getfirstlistofpublishednodes"></a>
#### Get currently published nodes
```
POST /v2/publish/{endpointId}
```


##### Description
Returns currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The list request|[PublishedItemListRequestApiModel](definitions.md#publisheditemlistrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|


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


<a name="getnextlistofpublishednodes"></a>
#### Get next set of published nodes
```
GET /v2/publish/{endpointId}
```


##### Description
Returns next set of currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|The continuation token to continue with|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


<a name="startpublishingvalues"></a>
#### Start publishing node values
```
POST /v2/publish/{endpointId}/start
```


##### Description
Start publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The publish request|[PublishStartRequestApiModel](definitions.md#publishstartrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseApiModel](definitions.md#publishstartresponseapimodel)|


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


<a name="stoppublishingvalues"></a>
#### Stop publishing node values
```
POST /v2/publish/{endpointId}/stop
```


##### Description
Stop publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The unpublish request|[PublishStopRequestApiModel](definitions.md#publishstoprequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseApiModel](definitions.md#publishstopresponseapimodel)|


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



