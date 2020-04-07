# Opc-History-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Historic Access Service


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

* Delete : Services to delete history
* History : History raw access services
* Insert : History insert services
* Read : Historic access read services
* Replace : History replace services




<a name="paths"></a>
## Resources

<a name="delete_resource"></a>
### Delete
Services to delete history


<a name="historydeleteevents"></a>
#### Delete historic events
```
POST /history/v2/delete/{endpointId}/events
```


##### Description
Delete historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteEventsDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#deleteeventsdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/delete/{endpointId}/values
```


##### Description
Delete historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteValuesDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#deletevaluesdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/delete/{endpointId}/values/modified
```


##### Description
Delete historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteModifiedValuesDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#deletemodifiedvaluesdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/delete/{endpointId}/values/pick
```


##### Description
Delete value history using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[DeleteValuesAtTimesDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#deletevaluesattimesdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/history/read/{endpointId}
```


##### Description
Read node history if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[VariantValueHistoryReadRequestApiModel](definitions.md#variantvaluehistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseApiModel](definitions.md#variantvaluehistoryreadresponseapimodel)|


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
POST /history/v2/history/read/{endpointId}/next
```


##### Description
Read next batch of node history values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseApiModel](definitions.md#variantvaluehistoryreadnextresponseapimodel)|


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
POST /history/v2/history/update/{endpointId}
```


##### Description
Update node history using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history update request|[VariantValueHistoryUpdateRequestApiModel](definitions.md#variantvaluehistoryupdaterequestapimodel)|


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
POST /history/v2/insert/{endpointId}/events
```


##### Description
Insert historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history insert request|[InsertEventsDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#inserteventsdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/insert/{endpointId}/values
```


##### Description
Insert historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history insert request|[InsertValuesDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#insertvaluesdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/read/{endpointId}/events
```


##### Description
Read historic events of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadEventsDetailsApiModelHistoryReadRequestApiModel](definitions.md#readeventsdetailsapimodelhistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventApiModelArrayHistoryReadResponseApiModel](definitions.md#historiceventapimodelarrayhistoryreadresponseapimodel)|


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
POST /history/v2/read/{endpointId}/events/next
```


##### Description
Read next batch of historic events of a node using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventApiModelArrayHistoryReadNextResponseApiModel](definitions.md#historiceventapimodelarrayhistoryreadnextresponseapimodel)|


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
POST /history/v2/read/{endpointId}/values
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadValuesDetailsApiModelHistoryReadRequestApiModel](definitions.md#readvaluesdetailsapimodelhistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueApiModelArrayHistoryReadResponseApiModel](definitions.md#historicvalueapimodelarrayhistoryreadresponseapimodel)|


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
POST /history/v2/read/{endpointId}/values/modified
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadModifiedValuesDetailsApiModelHistoryReadRequestApiModel](definitions.md#readmodifiedvaluesdetailsapimodelhistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueApiModelArrayHistoryReadResponseApiModel](definitions.md#historicvalueapimodelarrayhistoryreadresponseapimodel)|


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
POST /history/v2/read/{endpointId}/values/next
```


##### Description
Read next batch of historic values of a node using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueApiModelArrayHistoryReadNextResponseApiModel](definitions.md#historicvalueapimodelarrayhistoryreadnextresponseapimodel)|


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
POST /history/v2/read/{endpointId}/values/pick
```


##### Description
Read historic values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadValuesAtTimesDetailsApiModelHistoryReadRequestApiModel](definitions.md#readvaluesattimesdetailsapimodelhistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueApiModelArrayHistoryReadResponseApiModel](definitions.md#historicvalueapimodelarrayhistoryreadresponseapimodel)|


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
POST /history/v2/read/{endpointId}/values/processed
```


##### Description
Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history read request|[ReadProcessedValuesDetailsApiModelHistoryReadRequestApiModel](definitions.md#readprocessedvaluesdetailsapimodelhistoryreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueApiModelArrayHistoryReadResponseApiModel](definitions.md#historicvalueapimodelarrayhistoryreadresponseapimodel)|


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
POST /history/v2/replace/{endpointId}/events
```


##### Description
Replace historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history replace request|[ReplaceEventsDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#replaceeventsdetailsapimodelhistoryupdaterequestapimodel)|


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
POST /history/v2/replace/{endpointId}/values
```


##### Description
Replace historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The history replace request|[ReplaceValuesDetailsApiModelHistoryUpdateRequestApiModel](definitions.md#replacevaluesdetailsapimodelhistoryupdaterequestapimodel)|


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



