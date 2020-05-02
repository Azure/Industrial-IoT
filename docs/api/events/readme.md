# SignalR-Event-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT SignalR Event Service


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

* Discovery : Configure discovery
* Telemetry : Value and Event monitoring services




<a name="paths"></a>
## Resources

<a name="discovery_resource"></a>
### Discovery
Configure discovery


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|


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
|**200**|Success|No Content|



