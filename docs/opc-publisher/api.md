
<a name="paths"></a>
## Resources

<a name="discoverymethods_resource"></a>
### DiscoveryMethods
Discovery methods controller


<a name="discover"></a>
#### Discover application
```
POST /v2/discovery
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
POST /v2/discovery/cancel
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### Find server with endpoint
```
POST /v2/discovery/findserver
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="register"></a>
#### Start server registration
```
POST /v2/discovery/register
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historymethods_resource"></a>
### HistoryMethods
History methods controller


<a name="historydeleteevents"></a>
#### Delete events
```
POST /v2/history/events/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert events
```
POST /v2/history/events/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### Stream modified historic events
```
POST /v2/history/events/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

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
POST /v2/history/events/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read next set of events
```
POST /v2/history/events/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

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
#### Replace events
```
POST /v2/history/events/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert events
```
POST /v2/history/events/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values
```
POST /v2/history/values/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values at specified times
```
POST /v2/history/values/delete/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete modified values
```
POST /v2/history/values/delete/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert values
```
POST /v2/history/values/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### Stream values
```
POST /v2/history/values/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### Stream historic values at times
```
POST /v2/history/values/read/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Read historic values
```
POST /v2/history/values/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read historic values at times
```
POST /v2/history/values/read/first/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read modified values
```
POST /v2/history/values/read/first/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read processed historic values
```
POST /v2/history/values/read/first/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### Stream modified historic values
```
POST /v2/history/values/read/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### Read next set of historic values
```
POST /v2/history/values/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### Stream processed historic values
```
POST /v2/history/values/read/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Replace values
```
POST /v2/history/values/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert values
```
POST /v2/history/values/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishermethods_resource"></a>
### PublisherMethods
Publisher methods controller


<a name="publishbulk"></a>
#### Configure node values to publish and unpublish in bulk
```
POST /v2/publish/bulk
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### Handler for GetDiagnosticInfo direct method
```
POST /v2/publish/diagnostics
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### Handler for AddOrUpdateEndpoints direct method
```
POST /v2/publish/endpoints/addorupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfiguredendpoints"></a>
#### Handler for GetConfiguredEndpoints direct method
```
POST /v2/publish/endpoints/list
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### Handler for GetConfiguredNodesOnEndpoint direct method
```
POST /v2/publish/endpoints/list/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### Get all published nodes for a server endpoint.
```
POST /v2/publish/list
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### Handler for PublishNodes direct method
```
POST /v2/publish/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### Handler for UnpublishNodes direct method
```
POST /v2/publish/nodes/unpublish
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### Handler for UnpublishAllNodes direct method
```
POST /v2/publish/nodes/unpublish/all
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### Start publishing values from a node
```
POST /v2/publish/start
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### Stop publishing values from a node
```
POST /v2/publish/stop
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="twinmethods_resource"></a>
### TwinMethods
Twin methods controller


<a name="browsestream"></a>
#### Browse next
```
POST /v2/browse
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next
```
POST /v2/browse/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### Browse by path
```
POST /v2/browse/path
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### Call method
```
POST /v2/call
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### Get method meta data
```
POST /v2/call/$metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

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
#### Get the capabilities of the server
```
POST /v2/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate
```
POST /v2/certificate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### Get the historian capabilities of the server
```
POST /v2/history/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

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
#### Get the historian configuration
```
POST /v2/history/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### Read history
```
POST /v2/historyread/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### Read next history
```
POST /v2/historyread/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### Update history
```
POST /v2/historyupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get node metadata.
```
POST /v2/metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### Read value
```
POST /v2/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### Read attributes
```
POST /v2/read/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test connection
```
POST /v2/test
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### Write value
```
POST /v2/write
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### Write attributes
```
POST /v2/write/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`




<a name="paths"></a>
## Resources

<a name="discoverymethods_resource"></a>
### DiscoveryMethods
Discovery methods controller


<a name="discover"></a>
#### Discover application
```
POST /v2/discovery
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
POST /v2/discovery/cancel
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### Find server with endpoint
```
POST /v2/discovery/findserver
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="register"></a>
#### Start server registration
```
POST /v2/discovery/register
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historymethods_resource"></a>
### HistoryMethods
History methods controller


<a name="historydeleteevents"></a>
#### Delete events
```
POST /v2/history/events/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert events
```
POST /v2/history/events/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### Stream modified historic events
```
POST /v2/history/events/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

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
POST /v2/history/events/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read next set of events
```
POST /v2/history/events/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

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
#### Replace events
```
POST /v2/history/events/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert events
```
POST /v2/history/events/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values
```
POST /v2/history/values/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values at specified times
```
POST /v2/history/values/delete/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete modified values
```
POST /v2/history/values/delete/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert values
```
POST /v2/history/values/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### Stream values
```
POST /v2/history/values/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### Stream historic values at times
```
POST /v2/history/values/read/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Read historic values
```
POST /v2/history/values/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read historic values at times
```
POST /v2/history/values/read/first/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read modified values
```
POST /v2/history/values/read/first/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read processed historic values
```
POST /v2/history/values/read/first/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### Stream modified historic values
```
POST /v2/history/values/read/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### Read next set of historic values
```
POST /v2/history/values/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### Stream processed historic values
```
POST /v2/history/values/read/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Replace values
```
POST /v2/history/values/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert values
```
POST /v2/history/values/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishermethods_resource"></a>
### PublisherMethods
Publisher methods controller


<a name="publishbulk"></a>
#### Configure node values to publish and unpublish in bulk
```
POST /v2/publish/bulk
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### Handler for GetDiagnosticInfo direct method
```
POST /v2/publish/diagnostics
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### Handler for AddOrUpdateEndpoints direct method
```
POST /v2/publish/endpoints/addorupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfiguredendpoints"></a>
#### Handler for GetConfiguredEndpoints direct method
```
POST /v2/publish/endpoints/list
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### Handler for GetConfiguredNodesOnEndpoint direct method
```
POST /v2/publish/endpoints/list/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### Get all published nodes for a server endpoint.
```
POST /v2/publish/list
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### Handler for PublishNodes direct method
```
POST /v2/publish/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### Handler for UnpublishNodes direct method
```
POST /v2/publish/nodes/unpublish
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### Handler for UnpublishAllNodes direct method
```
POST /v2/publish/nodes/unpublish/all
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### Start publishing values from a node
```
POST /v2/publish/start
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### Stop publishing values from a node
```
POST /v2/publish/stop
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="twinmethods_resource"></a>
### TwinMethods
Twin methods controller


<a name="browsestream"></a>
#### Browse next
```
POST /v2/browse
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next
```
POST /v2/browse/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### Browse by path
```
POST /v2/browse/path
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### Call method
```
POST /v2/call
```


##### Description
Call a method on the OPC UA server endpoint with the specified input arguments and received the result in the form of the method output arguments.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### Get method meta data
```
POST /v2/call/$metadata
```


##### Description
Get the metadata for calling the method. This API is obsolete. Use the more powerful M:Azure.IIoT.OpcUa.Publisher.Module.Controllers.TwinMethodsController.GetMetadataAsync(Azure.IIoT.OpcUa.Publisher.Models.RequestEnvelope{Azure.IIoT.OpcUa.Publisher.Models.NodeMetadataRequestModel},System.Threading.CancellationToken) instead.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

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
#### Get the capabilities of the server
```
POST /v2/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate
```
POST /v2/certificate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### Get the historian capabilities of the server
```
POST /v2/history/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

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
#### Get the historian configuration
```
POST /v2/history/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### Read history
```
POST /v2/historyread/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### Read next history
```
POST /v2/historyread/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### Update history
```
POST /v2/historyupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get node metadata.
```
POST /v2/metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="compilequery"></a>
#### Compile query
```
POST /v2/query/compile
```


##### Description
Compile a query string into a query spec that can be used when setting up event filters on monitored items that monitor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The compilation request|[QueryCompilationRequestModelRequestEnvelope](definitions.md#querycompilationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### Read value
```
POST /v2/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### Read attributes
```
POST /v2/read/attributes
```


##### Description
Read an attribute of a node. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test connection
```
POST /v2/test
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### Write value
```
POST /v2/write
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### Write attributes
```
POST /v2/write/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`




<a name="paths"></a>
## Resources

<a name="discoverymethods_resource"></a>
### DiscoveryMethods
Discovery methods controller


<a name="discover"></a>
#### Discover application
```
POST /v2/discovery
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
POST /v2/discovery/cancel
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### Find server with endpoint
```
POST /v2/discovery/findserver
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="register"></a>
#### Start server registration
```
POST /v2/discovery/register
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historymethods_resource"></a>
### HistoryMethods
History methods controller


<a name="historydeleteevents"></a>
#### Delete events
```
POST /v2/history/events/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert events
```
POST /v2/history/events/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### Stream modified historic events
```
POST /v2/history/events/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

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
POST /v2/history/events/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read next set of events
```
POST /v2/history/events/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

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
#### Replace events
```
POST /v2/history/events/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert events
```
POST /v2/history/events/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values
```
POST /v2/history/values/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values at specified times
```
POST /v2/history/values/delete/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete modified values
```
POST /v2/history/values/delete/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert values
```
POST /v2/history/values/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### Stream values
```
POST /v2/history/values/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### Stream historic values at times
```
POST /v2/history/values/read/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Read historic values
```
POST /v2/history/values/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read historic values at times
```
POST /v2/history/values/read/first/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read modified values
```
POST /v2/history/values/read/first/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read processed historic values
```
POST /v2/history/values/read/first/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### Stream modified historic values
```
POST /v2/history/values/read/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### Read next set of historic values
```
POST /v2/history/values/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### Stream processed historic values
```
POST /v2/history/values/read/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Replace values
```
POST /v2/history/values/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert values
```
POST /v2/history/values/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishermethods_resource"></a>
### PublisherMethods
Publisher methods controller


<a name="publishbulk"></a>
#### Configure node values to publish and unpublish in bulk
```
POST /v2/publish/bulk
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### Handler for GetDiagnosticInfo direct method
```
POST /v2/publish/diagnostics
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### Handler for AddOrUpdateEndpoints direct method
```
POST /v2/publish/endpoints/addorupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfiguredendpoints"></a>
#### Handler for GetConfiguredEndpoints direct method
```
POST /v2/publish/endpoints/list
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### Handler for GetConfiguredNodesOnEndpoint direct method
```
POST /v2/publish/endpoints/list/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### Get all published nodes for a server endpoint.
```
POST /v2/publish/list
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### Handler for PublishNodes direct method
```
POST /v2/publish/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### Handler for UnpublishNodes direct method
```
POST /v2/publish/nodes/unpublish
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### Handler for UnpublishAllNodes direct method
```
POST /v2/publish/nodes/unpublish/all
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### Start publishing values from a node
```
POST /v2/publish/start
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### Stop publishing values from a node
```
POST /v2/publish/stop
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="twinmethods_resource"></a>
### TwinMethods
Twin methods controller


<a name="browsestream"></a>
#### Browse next
```
POST /v2/browse
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next
```
POST /v2/browse/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### Browse by path
```
POST /v2/browse/path
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### Call method
```
POST /v2/call
```


##### Description
Call a method on the OPC UA server endpoint with the specified input arguments and received the result in the form of the method output arguments.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### Get method meta data
```
POST /v2/call/$metadata
```


##### Description
Get the metadata for calling the method. This API is obsolete. Use the more powerful M:Azure.IIoT.OpcUa.Publisher.Module.Controllers.TwinMethodsController.GetMetadataAsync(Azure.IIoT.OpcUa.Publisher.Models.RequestEnvelope{Azure.IIoT.OpcUa.Publisher.Models.NodeMetadataRequestModel},System.Threading.CancellationToken) instead.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

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
#### Get the capabilities of the server
```
POST /v2/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate
```
POST /v2/certificate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### Get the historian capabilities of the server
```
POST /v2/history/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

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
#### Get the historian configuration
```
POST /v2/history/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### Read history
```
POST /v2/historyread/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### Read next history
```
POST /v2/historyread/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### Update history
```
POST /v2/historyupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get node metadata.
```
POST /v2/metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="compilequery"></a>
#### Compile query
```
POST /v2/query/compile
```


##### Description
Compile a query string into a query spec that can be used when setting up event filters on monitored items that monitor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The compilation request|[QueryCompilationRequestModelRequestEnvelope](definitions.md#querycompilationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### Read value
```
POST /v2/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### Read attributes
```
POST /v2/read/attributes
```


##### Description
Read an attribute of a node. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test connection
```
POST /v2/test
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### Write value
```
POST /v2/write
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### Write attributes
```
POST /v2/write/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`




<a name="paths"></a>
## Resources

<a name="discoverymethods_resource"></a>
### DiscoveryMethods
Discovery methods controller


<a name="discover"></a>
#### Discover application
```
POST /v2/discovery
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
POST /v2/discovery/cancel
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### Find server with endpoint
```
POST /v2/discovery/findserver
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="register"></a>
#### Start server registration
```
POST /v2/discovery/register
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historymethods_resource"></a>
### HistoryMethods
History methods controller


<a name="historydeleteevents"></a>
#### Delete events
```
POST /v2/history/events/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert events
```
POST /v2/history/events/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### Stream modified historic events
```
POST /v2/history/events/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

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
POST /v2/history/events/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read next set of events
```
POST /v2/history/events/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

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
#### Replace events
```
POST /v2/history/events/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert events
```
POST /v2/history/events/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values
```
POST /v2/history/values/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values at specified times
```
POST /v2/history/values/delete/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete modified values
```
POST /v2/history/values/delete/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert values
```
POST /v2/history/values/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### Stream values
```
POST /v2/history/values/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### Stream historic values at times
```
POST /v2/history/values/read/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Read historic values
```
POST /v2/history/values/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read historic values at times
```
POST /v2/history/values/read/first/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read modified values
```
POST /v2/history/values/read/first/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read processed historic values
```
POST /v2/history/values/read/first/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### Stream modified historic values
```
POST /v2/history/values/read/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### Read next set of historic values
```
POST /v2/history/values/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### Stream processed historic values
```
POST /v2/history/values/read/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Replace values
```
POST /v2/history/values/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert values
```
POST /v2/history/values/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishermethods_resource"></a>
### PublisherMethods
Publisher methods controller


<a name="getconfiguredendpoints"></a>
#### Handler for GetConfiguredEndpoints direct method
```
GET /v2/configuration
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="setconfiguredendpoints"></a>
#### Handler for SetConfiguredEndpoints direct method
```
PUT /v2/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[SetConfiguredEndpointsRequestModel](definitions.md#setconfiguredendpointsrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### Handler for AddOrUpdateEndpoints direct method
```
PATCH /v2/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishbulk"></a>
#### Configure node values to publish and unpublish in bulk
```
POST /v2/configuration/bulk
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### Handler for GetDiagnosticInfo direct method
```
POST /v2/configuration/diagnostics
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### Handler for GetConfiguredNodesOnEndpoint direct method
```
POST /v2/configuration/endpoints/list/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### Get all published nodes for a server endpoint.
```
POST /v2/configuration/list
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### Handler for PublishNodes direct method
```
POST /v2/configuration/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### Handler for UnpublishNodes direct method
```
POST /v2/configuration/nodes/unpublish
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### Handler for UnpublishAllNodes direct method
```
POST /v2/configuration/nodes/unpublish/all
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### Start publishing values from a node
```
POST /v2/configuration/start
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### Stop publishing values from a node
```
POST /v2/configuration/stop
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="twinmethods_resource"></a>
### TwinMethods
Twin methods controller


<a name="browsestream"></a>
#### Browse next
```
POST /v2/browse
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next
```
POST /v2/browse/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### Browse by path
```
POST /v2/browse/path
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### Call method
```
POST /v2/call
```


##### Description
Call a method on the OPC UA server endpoint with the specified input arguments and received the result in the form of the method output arguments.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### Get method meta data
```
POST /v2/call/$metadata
```


##### Description
Get the metadata for calling the method. This API is obsolete. Use the more powerful M:Azure.IIoT.OpcUa.Publisher.Module.Controllers.TwinMethodsController.GetMetadataAsync(Azure.IIoT.OpcUa.Publisher.Models.RequestEnvelope{Azure.IIoT.OpcUa.Publisher.Models.NodeMetadataRequestModel},System.Threading.CancellationToken) instead.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

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
#### Get the capabilities of the server
```
POST /v2/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate
```
POST /v2/certificate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### Get the historian capabilities of the server
```
POST /v2/history/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

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
#### Get the historian configuration
```
POST /v2/history/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### Read history
```
POST /v2/historyread/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### Read next history
```
POST /v2/historyread/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### Update history
```
POST /v2/historyupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get node metadata.
```
POST /v2/metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="compilequery"></a>
#### Compile query
```
POST /v2/query/compile
```


##### Description
Compile a query string into a query spec that can be used when setting up event filters on monitored items that monitor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The compilation request|[QueryCompilationRequestModelRequestEnvelope](definitions.md#querycompilationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### Read value
```
POST /v2/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### Read attributes
```
POST /v2/read/attributes
```


##### Description
Read an attribute of a node. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test connection
```
POST /v2/test
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### Write value
```
POST /v2/write
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### Write attributes
```
POST /v2/write/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`




<a name="paths"></a>
## Resources

<a name="discoverymethods_resource"></a>
### DiscoveryMethods
Discovery methods controller


<a name="discover"></a>
#### Discover application
```
POST /v2/discovery
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryRequestModel](definitions.md#discoveryrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="cancel"></a>
#### Cancel discovery
```
POST /v2/discovery/cancel
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DiscoveryCancelRequestModel](definitions.md#discoverycancelrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="findserver"></a>
#### Find server with endpoint
```
POST /v2/discovery/findserver
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerEndpointQueryModel](definitions.md#serverendpointquerymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ApplicationRegistrationModel](definitions.md#applicationregistrationmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="register"></a>
#### Start server registration
```
POST /v2/discovery/register
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ServerRegistrationRequestModel](definitions.md#serverregistrationrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historymethods_resource"></a>
### HistoryMethods
History methods controller


<a name="historydeleteevents"></a>
#### Delete events
```
POST /v2/history/events/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert events
```
POST /v2/history/events/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamevents"></a>
#### Stream modified historic events
```
POST /v2/history/events/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelIAsyncEnumerable](definitions.md#historiceventmodeliasyncenumerable)|


##### Consumes

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
POST /v2/history/events/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadResponseModel](definitions.md#historiceventmodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read next set of events
```
POST /v2/history/events/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricEventModelArrayHistoryReadNextResponseModel](definitions.md#historiceventmodelarrayhistoryreadnextresponsemodel)|


##### Consumes

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
#### Replace events
```
POST /v2/history/events/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert events
```
POST /v2/history/events/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values
```
POST /v2/history/values/delete
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete values at specified times
```
POST /v2/history/values/delete/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Delete modified values
```
POST /v2/history/values/delete/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Insert values
```
POST /v2/history/values/insert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvalues"></a>
#### Stream values
```
POST /v2/history/values/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamvaluesattimes"></a>
#### Stream historic values at times
```
POST /v2/history/values/read/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Read historic values
```
POST /v2/history/values/read/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read historic values at times
```
POST /v2/history/values/read/first/attimes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read modified values
```
POST /v2/history/values/read/first/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

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
#### Read processed historic values
```
POST /v2/history/values/read/first/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadResponseModel](definitions.md#historicvaluemodelarrayhistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreammodifiedvalues"></a>
#### Stream modified historic values
```
POST /v2/history/values/read/modified
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadvaluesnext"></a>
#### Read next set of historic values
```
POST /v2/history/values/read/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelArrayHistoryReadNextResponseModel](definitions.md#historicvaluemodelarrayhistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historystreamprocessedvalues"></a>
#### Stream processed historic values
```
POST /v2/history/values/read/processed
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoricValueModelIAsyncEnumerable](definitions.md#historicvaluemodeliasyncenumerable)|


##### Consumes

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
#### Replace values
```
POST /v2/history/values/replace
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

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
#### Upsert values
```
POST /v2/history/values/upsert
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishermethods_resource"></a>
### PublisherMethods
Publisher methods controller


<a name="getconfiguredendpoints"></a>
#### Handler for GetConfiguredEndpoints direct method
```
GET /v2/configuration
```


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**IncludeNodes**  <br>*optional*|Include nodes that make up the configuration|boolean|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredEndpointsResponseModel](definitions.md#getconfiguredendpointsresponsemodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="setconfiguredendpoints"></a>
#### Handler for SetConfiguredEndpoints direct method
```
PUT /v2/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[SetConfiguredEndpointsRequestModel](definitions.md#setconfiguredendpointsrequestmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="addorupdateendpoints"></a>
#### Handler for AddOrUpdateEndpoints direct method
```
PATCH /v2/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishbulk"></a>
#### Configure node values to publish and unpublish in bulk
```
POST /v2/configuration/bulk
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishBulkRequestModelRequestEnvelope](definitions.md#publishbulkrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseModel](definitions.md#publishbulkresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getdiagnosticinfo"></a>
#### Handler for GetDiagnosticInfo direct method
```
POST /v2/configuration/diagnostics
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PublishDiagnosticInfoModel](definitions.md#publishdiagnosticinfomodel) > array|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getconfigurednodesonendpoint"></a>
#### Handler for GetConfiguredNodesOnEndpoint direct method
```
POST /v2/configuration/endpoints/list/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetConfiguredNodesOnEndpointResponseModel](definitions.md#getconfigurednodesonendpointresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishlist"></a>
#### Get all published nodes for a server endpoint.
```
POST /v2/configuration/list
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedItemListRequestModelRequestEnvelope](definitions.md#publisheditemlistrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseModel](definitions.md#publisheditemlistresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishnodes"></a>
#### Handler for PublishNodes direct method
```
POST /v2/configuration/nodes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishnodes"></a>
#### Handler for UnpublishNodes direct method
```
POST /v2/configuration/nodes/unpublish
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="unpublishallnodes"></a>
#### Handler for UnpublishAllNodes direct method
```
POST /v2/configuration/nodes/unpublish/all
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedNodesResponseModel](definitions.md#publishednodesresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstart"></a>
#### Start publishing values from a node
```
POST /v2/configuration/start
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStartRequestModelRequestEnvelope](definitions.md#publishstartrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseModel](definitions.md#publishstartresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="publishstop"></a>
#### Stop publishing values from a node
```
POST /v2/configuration/stop
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[PublishStopRequestModelRequestEnvelope](definitions.md#publishstoprequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseModel](definitions.md#publishstopresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="twinmethods_resource"></a>
### TwinMethods
Twin methods controller


<a name="browsestream"></a>
#### Browse next
```
POST /v2/browse
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseStreamRequestModelRequestEnvelope](definitions.md#browsestreamrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseStreamChunkModelIAsyncEnumerable](definitions.md#browsestreamchunkmodeliasyncenumerable)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browse"></a>
#### Browse
```
POST /v2/browse/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseFirstRequestModelRequestEnvelope](definitions.md#browsefirstrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseFirstResponseModel](definitions.md#browsefirstresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsenext"></a>
#### Browse next
```
POST /v2/browse/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowseNextRequestModelRequestEnvelope](definitions.md#browsenextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseModel](definitions.md#browsenextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="browsepath"></a>
#### Browse by path
```
POST /v2/browse/path
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[BrowsePathRequestModelRequestEnvelope](definitions.md#browsepathrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseModel](definitions.md#browsepathresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodcall"></a>
#### Call method
```
POST /v2/call
```


##### Description
Call a method on the OPC UA server endpoint with the specified input arguments and received the result in the form of the method output arguments.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodCallRequestModelRequestEnvelope](definitions.md#methodcallrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseModel](definitions.md#methodcallresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="methodmetadata"></a>
#### Get method meta data
```
POST /v2/call/$metadata
```


##### Description
Get the metadata for calling the method. This API is obsolete. Use the more powerful M:Azure.IIoT.OpcUa.Publisher.Module.Controllers.TwinMethodsController.GetMetadataAsync(Azure.IIoT.OpcUa.Publisher.Models.RequestEnvelope{Azure.IIoT.OpcUa.Publisher.Models.NodeMetadataRequestModel},System.Threading.CancellationToken) instead.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[MethodMetadataRequestModelRequestEnvelope](definitions.md#methodmetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseModel](definitions.md#methodmetadataresponsemodel)|


##### Consumes

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
#### Get the capabilities of the server
```
POST /v2/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ServerCapabilitiesModel](definitions.md#servercapabilitiesmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getendpointcertificate"></a>
#### Get endpoint certificate
```
POST /v2/certificate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[EndpointModel](definitions.md#endpointmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[X509CertificateChainModel](definitions.md#x509certificatechainmodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="connect"></a>
#### Connect
```
POST /v2/connect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectRequestModelRequestEnvelope](definitions.md#connectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ConnectResponseModel](definitions.md#connectresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="disconnect"></a>
#### Disconnect
```
POST /v2/disconnect
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[DisconnectRequestModelRequestEnvelope](definitions.md#disconnectrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


<a name="historygetservercapabilities"></a>
#### Get the historian capabilities of the server
```
POST /v2/history/capabilities
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ConnectionModel](definitions.md#connectionmodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryServerCapabilitiesModel](definitions.md#historyservercapabilitiesmodel)|


##### Consumes

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
#### Get the historian configuration
```
POST /v2/history/configuration
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryConfigurationRequestModelRequestEnvelope](definitions.md#historyconfigurationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryConfigurationResponseModel](definitions.md#historyconfigurationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyread"></a>
#### Read history
```
POST /v2/historyread/first
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryReadRequestModelRequestEnvelope](definitions.md#variantvaluehistoryreadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadResponseModel](definitions.md#variantvaluehistoryreadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyreadnext"></a>
#### Read next history
```
POST /v2/historyread/next
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[HistoryReadNextRequestModelRequestEnvelope](definitions.md#historyreadnextrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[VariantValueHistoryReadNextResponseModel](definitions.md#variantvaluehistoryreadnextresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="historyupdate"></a>
#### Update history
```
POST /v2/historyupdate
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[VariantValueHistoryUpdateRequestModelRequestEnvelope](definitions.md#variantvaluehistoryupdaterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseModel](definitions.md#historyupdateresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getmetadata"></a>
#### Get node metadata.
```
POST /v2/metadata
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[NodeMetadataRequestModelRequestEnvelope](definitions.md#nodemetadatarequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[NodeMetadataResponseModel](definitions.md#nodemetadataresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="compilequery"></a>
#### Compile query
```
POST /v2/query/compile
```


##### Description
Compile a query string into a query spec that can be used when setting up event filters on monitored items that monitor events.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Body**|**body**  <br>*optional*|The compilation request|[QueryCompilationRequestModelRequestEnvelope](definitions.md#querycompilationrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[QueryCompilationResponseModel](definitions.md#querycompilationresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valueread"></a>
#### Read value
```
POST /v2/read
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueReadRequestModelRequestEnvelope](definitions.md#valuereadrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseModel](definitions.md#valuereadresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="noderead"></a>
#### Read attributes
```
POST /v2/read/attributes
```


##### Description
Read an attribute of a node. The attributes supported by the node are dependend on the node class of the node.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ReadRequestModelRequestEnvelope](definitions.md#readrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseModel](definitions.md#readresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="testconnection"></a>
#### Test connection
```
POST /v2/test
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[TestConnectionRequestModelRequestEnvelope](definitions.md#testconnectionrequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TestConnectionResponseModel](definitions.md#testconnectionresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="valuewrite"></a>
#### Write value
```
POST /v2/write
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[ValueWriteRequestModelRequestEnvelope](definitions.md#valuewriterequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseModel](definitions.md#valuewriteresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="nodewrite"></a>
#### Write attributes
```
POST /v2/write/attributes
```


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**body**  <br>*optional*|[WriteRequestModelRequestEnvelope](definitions.md#writerequestmodelrequestenvelope)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseModel](definitions.md#writeresponsemodel)|


##### Consumes

* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`



