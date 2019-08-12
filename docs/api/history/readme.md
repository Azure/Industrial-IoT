# Opc-History-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Historic Access Service


### Version information
*Version* : v2


### URI scheme
*Schemes* : HTTPS, HTTP


### Tags

* Delete : Services to delete history
* History : History raw access services
* Insert : History insert services
* Read : Historic access read services
* Replace : History replace services
* Status : Status checks




<a name="paths"></a>
## Resources

<a name="delete_resource"></a>
### Delete
Services to delete history


<a name="historydeleteevents"></a>
#### Delete historic events
```
POST /v2/delete/{endpointId}/events
```


##### Description
Delete historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deleteeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="historydeletevalues"></a>
#### Delete historic values
```
POST /v2/delete/{endpointId}/values
```


##### Description
Delete historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletevaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="historydeletemodifiedvalues"></a>
#### Delete historic values
```
POST /v2/delete/{endpointId}/values/modified
```


##### Description
Delete historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteModifiedValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletemodifiedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="historydeletevaluesattimes"></a>
#### Delete value history at specified times
```
POST /v2/delete/{endpointId}/values/pick
```


##### Description
Delete value history using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteValuesAtTimesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletevaluesattimesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="history_resource"></a>
### History
History raw access services


<a name="historyreadraw"></a>
#### Read history using json details
```
POST /v2/history/read/{endpointId}
```


##### Description
Read node history if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[JToken]](definitions.md#historyreadrequestapimodel-jtoken)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[JToken]](definitions.md#historyreadresponseapimodel-jtoken)|


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


<a name="historyreadrawnext"></a>
#### Read next batch of history as json
```
POST /v2/history/read/{endpointId}/next
```


##### Description
Read next batch of node history values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[JToken]](definitions.md#historyreadnextresponseapimodel-jtoken)|


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


<a name="historyupdateraw"></a>
#### Update node history using raw json
```
POST /v2/history/update/{endpointId}
```


##### Description
Update node history using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[JToken]](definitions.md#historyupdaterequestapimodel-jtoken)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="insert_resource"></a>
### Insert
History insert services


<a name="historyinsertevents"></a>
#### Insert historic events
```
POST /v2/insert/{endpointId}/events
```


##### Description
Insert historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history insert request|[HistoryUpdateRequestApiModel[InsertEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-inserteventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="historyinsertvalues"></a>
#### Insert historic values
```
POST /v2/insert/{endpointId}/values
```


##### Description
Insert historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history insert request|[HistoryUpdateRequestApiModel[InsertValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-insertvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="read_resource"></a>
### Read
Historic access read services


<a name="historyreadevents"></a>
#### Read historic events
```
POST /v2/read/{endpointId}/events
```


##### Description
Read historic events of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadEventsDetailsApiModel]](definitions.md#historyreadrequestapimodel-readeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricEventApiModel[]]](definitions.md#historyreadresponseapimodel-historiceventapimodel)|


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


<a name="historyreadeventsnext"></a>
#### Read next batch of historic events
```
POST /v2/read/{endpointId}/events/next
```


##### Description
Read next batch of historic events of a node using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[HistoricEventApiModel[]]](definitions.md#historyreadnextresponseapimodel-historiceventapimodel)|


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


<a name="historyreadvalues"></a>
#### Read historic processed values at specified times
```
POST /v2/read/{endpointId}/values
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


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


<a name="historyreadmodifiedvalues"></a>
#### Read historic modified values at specified times
```
POST /v2/read/{endpointId}/values/modified
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadModifiedValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readmodifiedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


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


<a name="historyreadvaluenext"></a>
#### Read next batch of historic values
```
POST /v2/read/{endpointId}/values/next
```


##### Description
Read next batch of historic values of a node using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadnextresponseapimodel-historicvalueapimodel)|


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


<a name="historyreadvaluesattimes"></a>
#### Read historic values at specified times
```
POST /v2/read/{endpointId}/values/pick
```


##### Description
Read historic values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadValuesAtTimesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readvaluesattimesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


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


<a name="historyreadprocessedvalues"></a>
#### Read historic processed values at specified times
```
POST /v2/read/{endpointId}/values/processed
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadProcessedValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readprocessedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


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


<a name="replace_resource"></a>
### Replace
History replace services


<a name="historyreplaceevents"></a>
#### Replace historic events
```
POST /v2/replace/{endpointId}/events
```


##### Description
Replace historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history replace request|[HistoryUpdateRequestApiModel[ReplaceEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-replaceeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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


<a name="historyreplacevalues"></a>
#### Replace historic values
```
POST /v2/replace/{endpointId}/values
```


##### Description
Replace historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history replace request|[HistoryUpdateRequestApiModel[ReplaceValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-replacevaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


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



