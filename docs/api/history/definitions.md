
<a name="definitions"></a>
## Definitions

<a name="aggregateconfigapimodel"></a>
### AggregateConfigApiModel
Aggregate configuration


|Name|Description|Schema|
|---|---|---|
|**percentDataBad**  <br>*optional*|Percent of data that is bad  <br>**Example** : `0`|integer (int32)|
|**percentDataGood**  <br>*optional*|Percent of data that is good  <br>**Example** : `0`|integer (int32)|
|**treatUncertainAsBad**  <br>*optional*|Whether to treat uncertain as bad  <br>**Example** : `true`|boolean|
|**useServerCapabilitiesDefaults**  <br>*optional*|Whether to use the default server caps  <br>**Example** : `true`|boolean|
|**useSlopedExtrapolation**  <br>*optional*|Whether to use sloped extrapolation.  <br>**Example** : `true`|boolean|


<a name="credentialapimodel"></a>
### CredentialApiModel
Credential model


|Name|Description|Schema|
|---|---|---|
|**type**  <br>*optional*|Type of credential  <br>**Default** : `"None"`  <br>**Example** : `"string"`|enum (None, UserName, X509Certificate, JwtToken)|
|**value**  <br>*optional*|Value to pass to server  <br>**Example** : `"object"`|object|


<a name="deleteeventsdetailsapimodel"></a>
### DeleteEventsDetailsApiModel
The events to delete


|Name|Description|Schema|
|---|---|---|
|**eventIds**  <br>*required*|Events to delete  <br>**Example** : `[ "string" ]`|< string (byte) > array|


<a name="deletemodifiedvaluesdetailsapimodel"></a>
### DeleteModifiedValuesDetailsApiModel
Delete raw modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to delete until  <br>**Example** : `"string"`|string (date-time)|
|**startTime**  <br>*optional*|Start time  <br>**Example** : `"string"`|string (date-time)|


<a name="deletevaluesattimesdetailsapimodel"></a>
### DeleteValuesAtTimesDetailsApiModel
Deletes data at times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|The timestamps to delete  <br>**Example** : `[ "string" ]`|< string (date-time) > array|


<a name="deletevaluesdetailsapimodel"></a>
### DeleteValuesDetailsApiModel
Delete raw modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to delete until  <br>**Example** : `"string"`|string (date-time)|
|**startTime**  <br>*optional*|Start time  <br>**Example** : `"string"`|string (date-time)|


<a name="diagnosticsapimodel"></a>
### DiagnosticsApiModel
Diagnostics configuration


|Name|Description|Schema|
|---|---|---|
|**auditId**  <br>*optional*|Client audit log entry.<br>(default: client generated)  <br>**Example** : `"string"`|string|
|**level**  <br>*optional*|Requested level of response diagnostics.<br>(default: Status)  <br>**Example** : `"string"`|enum (None, Status, Operations, Diagnostics, Verbose)|
|**timeStamp**  <br>*optional*|Timestamp of request.<br>(default: client generated)  <br>**Example** : `"string"`|string (date-time)|


<a name="historiceventapimodel"></a>
### HistoricEventApiModel
Historic event


|Name|Description|Schema|
|---|---|---|
|**eventFields**  <br>*optional*|The selected fields of the event  <br>**Example** : `[ "object" ]`|< object > array|


<a name="historicvalueapimodel"></a>
### HistoricValueApiModel
Historic data


|Name|Description|Schema|
|---|---|---|
|**modificationInfo**  <br>*optional*|modification information when reading modifications.  <br>**Example** : `"[modificationinfoapimodel](#modificationinfoapimodel)"`|[ModificationInfoApiModel](definitions.md#modificationinfoapimodel)|
|**serverPicoseconds**  <br>*optional*|Additional resolution for the server timestamp.  <br>**Example** : `0`|integer (int32)|
|**serverTimestamp**  <br>*optional*|The server timestamp associated with the value.  <br>**Example** : `"string"`|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Additional resolution for the source timestamp.  <br>**Example** : `0`|integer (int32)|
|**sourceTimestamp**  <br>*optional*|The source timestamp associated with the value.  <br>**Example** : `"string"`|string (date-time)|
|**statusCode**  <br>*optional*|The status code associated with the value.  <br>**Example** : `0`|integer (int32)|
|**value**  <br>*optional*|,<br>            The value of data value.  <br>**Example** : `"object"`|object|


<a name="historyreadnextrequestapimodel"></a>
### HistoryReadNextRequestApiModel
Request node history read continuation


|Name|Description|Schema|
|---|---|---|
|**abort**  <br>*optional*|Abort reading after this read  <br>**Default** : `false`  <br>**Example** : `true`|boolean|
|**continuationToken**  <br>*required*|Continuation token to continue reading more<br>results.  <br>**Example** : `"string"`|string|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|


<a name="historyreadnextresponseapimodel-historiceventapimodel"></a>
### HistoryReadNextResponseApiModel[HistoricEventApiModel[]]
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `[ "[historiceventapimodel](#historiceventapimodel)" ]`|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|


<a name="historyreadnextresponseapimodel-historicvalueapimodel"></a>
### HistoryReadNextResponseApiModel[HistoricValueApiModel[]]
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `[ "[historicvalueapimodel](#historicvalueapimodel)" ]`|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="historyreadnextresponseapimodel-jtoken"></a>
### HistoryReadNextResponseApiModel[JToken]
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `"object"`|object|


<a name="historyreadrequestapimodel-jtoken"></a>
### HistoryReadRequestApiModel[JToken]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"object"`|object|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadrequestapimodel-readeventsdetailsapimodel"></a>
### HistoryReadRequestApiModel[ReadEventsDetailsApiModel]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"[readeventsdetailsapimodel](#readeventsdetailsapimodel)"`|[ReadEventsDetailsApiModel](definitions.md#readeventsdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadrequestapimodel-readmodifiedvaluesdetailsapimodel"></a>
### HistoryReadRequestApiModel[ReadModifiedValuesDetailsApiModel]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"[readmodifiedvaluesdetailsapimodel](#readmodifiedvaluesdetailsapimodel)"`|[ReadModifiedValuesDetailsApiModel](definitions.md#readmodifiedvaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadrequestapimodel-readprocessedvaluesdetailsapimodel"></a>
### HistoryReadRequestApiModel[ReadProcessedValuesDetailsApiModel]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"[readprocessedvaluesdetailsapimodel](#readprocessedvaluesdetailsapimodel)"`|[ReadProcessedValuesDetailsApiModel](definitions.md#readprocessedvaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadrequestapimodel-readvaluesattimesdetailsapimodel"></a>
### HistoryReadRequestApiModel[ReadValuesAtTimesDetailsApiModel]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"[readvaluesattimesdetailsapimodel](#readvaluesattimesdetailsapimodel)"`|[ReadValuesAtTimesDetailsApiModel](definitions.md#readvaluesattimesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadrequestapimodel-readvaluesdetailsapimodel"></a>
### HistoryReadRequestApiModel[ReadValuesDetailsApiModel]
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.  <br>**Example** : `"[readvaluesdetailsapimodel](#readvaluesdetailsapimodel)"`|[ReadValuesDetailsApiModel](definitions.md#readvaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.  <br>**Example** : `"string"`|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)  <br>**Example** : `"string"`|string|


<a name="historyreadresponseapimodel-historiceventapimodel"></a>
### HistoryReadResponseApiModel[HistoricEventApiModel[]]
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `[ "[historiceventapimodel](#historiceventapimodel)" ]`|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|


<a name="historyreadresponseapimodel-historicvalueapimodel"></a>
### HistoryReadResponseApiModel[HistoricValueApiModel[]]
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `[ "[historicvalueapimodel](#historicvalueapimodel)" ]`|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="historyreadresponseapimodel-jtoken"></a>
### HistoryReadResponseApiModel[JToken]
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.  <br>**Example** : `"string"`|string|
|**errorInfo**  <br>*optional*|Service result in case of error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object  <br>**Example** : `"object"`|object|


<a name="historyupdaterequestapimodel-deleteeventsdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[DeleteEventsDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[deleteeventsdetailsapimodel](#deleteeventsdetailsapimodel)"`|[DeleteEventsDetailsApiModel](definitions.md#deleteeventsdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-deletemodifiedvaluesdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[DeleteModifiedValuesDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[deletemodifiedvaluesdetailsapimodel](#deletemodifiedvaluesdetailsapimodel)"`|[DeleteModifiedValuesDetailsApiModel](definitions.md#deletemodifiedvaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-deletevaluesattimesdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[DeleteValuesAtTimesDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[deletevaluesattimesdetailsapimodel](#deletevaluesattimesdetailsapimodel)"`|[DeleteValuesAtTimesDetailsApiModel](definitions.md#deletevaluesattimesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-deletevaluesdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[DeleteValuesDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[deletevaluesdetailsapimodel](#deletevaluesdetailsapimodel)"`|[DeleteValuesDetailsApiModel](definitions.md#deletevaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-inserteventsdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[InsertEventsDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[inserteventsdetailsapimodel](#inserteventsdetailsapimodel)"`|[InsertEventsDetailsApiModel](definitions.md#inserteventsdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-insertvaluesdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[InsertValuesDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[insertvaluesdetailsapimodel](#insertvaluesdetailsapimodel)"`|[InsertValuesDetailsApiModel](definitions.md#insertvaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-jtoken"></a>
### HistoryUpdateRequestApiModel[JToken]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"object"`|object|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-replaceeventsdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[ReplaceEventsDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[replaceeventsdetailsapimodel](#replaceeventsdetailsapimodel)"`|[ReplaceEventsDetailsApiModel](definitions.md#replaceeventsdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdaterequestapimodel-replacevaluesdetailsapimodel"></a>
### HistoryUpdateRequestApiModel[ReplaceValuesDetailsApiModel]
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.  <br>**Example** : `[ "string" ]`|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.  <br>**Example** : `"[replacevaluesdetailsapimodel](#replacevaluesdetailsapimodel)"`|[ReplaceValuesDetailsApiModel](definitions.md#replacevaluesdetailsapimodel)|
|**header**  <br>*optional*|Optional request header  <br>**Example** : `"[requestheaderapimodel](#requestheaderapimodel)"`|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update  <br>**Example** : `"string"`|string|


<a name="historyupdateresponseapimodel"></a>
### HistoryUpdateResponseApiModel
History update results


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of service call error  <br>**Example** : `"[serviceresultapimodel](#serviceresultapimodel)"`|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**results**  <br>*optional*|List of results from the update operation  <br>**Example** : `[ "[serviceresultapimodel](#serviceresultapimodel)" ]`|< [ServiceResultApiModel](definitions.md#serviceresultapimodel) > array|


<a name="inserteventsdetailsapimodel"></a>
### InsertEventsDetailsApiModel
Insert historic events


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*required*|The new events to insert  <br>**Example** : `[ "[historiceventapimodel](#historiceventapimodel)" ]`|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|
|**filter**  <br>*optional*|The filter to use to select the events  <br>**Example** : `"object"`|object|


<a name="insertvaluesdetailsapimodel"></a>
### InsertValuesDetailsApiModel
Insert historic data


|Name|Description|Schema|
|---|---|---|
|**values**  <br>*required*|Values to insert  <br>**Example** : `[ "[historicvalueapimodel](#historicvalueapimodel)" ]`|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="modificationinfoapimodel"></a>
### ModificationInfoApiModel
Modification information


|Name|Description|Schema|
|---|---|---|
|**modificationTime**  <br>*optional*|Modification time  <br>**Example** : `"string"`|string (date-time)|
|**updateType**  <br>*optional*|Operation  <br>**Example** : `"string"`|enum (Insert, Replace, Update, Delete)|
|**userName**  <br>*optional*|User who made the change  <br>**Example** : `"string"`|string|


<a name="readeventsdetailsapimodel"></a>
### ReadEventsDetailsApiModel
Read event data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to read to  <br>**Example** : `"string"`|string (date-time)|
|**filter**  <br>*optional*|The filter to use to select the event fields  <br>**Example** : `"object"`|object|
|**numEvents**  <br>*optional*|Number of events to read  <br>**Example** : `0`|integer (int32)|
|**startTime**  <br>*optional*|Start time to read from  <br>**Example** : `"string"`|string (date-time)|


<a name="readmodifiedvaluesdetailsapimodel"></a>
### ReadModifiedValuesDetailsApiModel
Read modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|The end time to read to  <br>**Example** : `"string"`|string (date-time)|
|**numValues**  <br>*optional*|The number of values to read  <br>**Example** : `0`|integer (int32)|
|**startTime**  <br>*optional*|The start time to read from  <br>**Example** : `"string"`|string (date-time)|


<a name="readprocessedvaluesdetailsapimodel"></a>
### ReadProcessedValuesDetailsApiModel
Read processed historic data


|Name|Description|Schema|
|---|---|---|
|**aggregateConfiguration**  <br>*optional*|A configuration for the aggregate  <br>**Example** : `"[aggregateconfigapimodel](#aggregateconfigapimodel)"`|[AggregateConfigApiModel](definitions.md#aggregateconfigapimodel)|
|**aggregateTypeId**  <br>*optional*|The aggregate type node ids  <br>**Example** : `"string"`|string|
|**endTime**  <br>*optional*|End time to read until  <br>**Example** : `"string"`|string (date-time)|
|**processingInterval**  <br>*optional*|Interval to process  <br>**Example** : `0.0`|number (double)|
|**startTime**  <br>*optional*|Start time to read from.  <br>**Example** : `"string"`|string (date-time)|


<a name="readvaluesattimesdetailsapimodel"></a>
### ReadValuesAtTimesDetailsApiModel
Read data at specified times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|Requested datums  <br>**Example** : `[ "string" ]`|< string (date-time) > array|
|**useSimpleBounds**  <br>*optional*|Whether to use simple bounds  <br>**Example** : `true`|boolean|


<a name="readvaluesdetailsapimodel"></a>
### ReadValuesDetailsApiModel
Read historic values


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End of period to read. Set to null if no<br>specific end time is specified.  <br>**Example** : `"string"`|string (date-time)|
|**numValues**  <br>*optional*|The maximum number of values returned for any Node<br>over the time range. If only one time is specified,<br>the time range shall extend to return this number<br>of values. 0 or null indicates that there is no<br>maximum.  <br>**Example** : `0`|integer (int32)|
|**returnBounds**  <br>*optional*|Whether to return the bounding values or not.  <br>**Example** : `true`|boolean|
|**startTime**  <br>*optional*|Beginning of period to read. Set to null<br>if no specific start time is specified.  <br>**Example** : `"string"`|string (date-time)|


<a name="replaceeventsdetailsapimodel"></a>
### ReplaceEventsDetailsApiModel
Replace historic events


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*required*|The events to replace  <br>**Example** : `[ "[historiceventapimodel](#historiceventapimodel)" ]`|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|
|**filter**  <br>*optional*|The filter to use to select the events  <br>**Example** : `"object"`|object|


<a name="replacevaluesdetailsapimodel"></a>
### ReplaceValuesDetailsApiModel
Replace historic data


|Name|Description|Schema|
|---|---|---|
|**values**  <br>*required*|Values to replace  <br>**Example** : `[ "[historicvalueapimodel](#historicvalueapimodel)" ]`|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="requestheaderapimodel"></a>
### RequestHeaderApiModel
Request header model


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*|Optional diagnostics configuration  <br>**Example** : `"[diagnosticsapimodel](#diagnosticsapimodel)"`|[DiagnosticsApiModel](definitions.md#diagnosticsapimodel)|
|**elevation**  <br>*optional*|Optional User elevation  <br>**Example** : `"[credentialapimodel](#credentialapimodel)"`|[CredentialApiModel](definitions.md#credentialapimodel)|
|**locales**  <br>*optional*|Optional list of locales in preference order.  <br>**Example** : `[ "string" ]`|< string > array|


<a name="serviceresultapimodel"></a>
### ServiceResultApiModel
Service result


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*|Additional diagnostics information  <br>**Example** : `"object"`|object|
|**errorMessage**  <br>*optional*|Error message in case of error or null.  <br>**Example** : `"string"`|string|
|**statusCode**  <br>*optional*|Error code - if null operation succeeded.  <br>**Example** : `0`|integer (int32)|


<a name="statusresponseapimodel"></a>
### StatusResponseApiModel
Status response model


|Name|Description|Schema|
|---|---|---|
|**$metadata**  <br>*optional*  <br>*read-only*|Optional meta data.  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**currentTime**  <br>*optional*  <br>*read-only*|Current time  <br>**Example** : `"string"`|string|
|**dependencies**  <br>*optional*  <br>*read-only*|A property bag with details about the internal dependencies  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**name**  <br>*optional*  <br>*read-only*|Name of this service  <br>**Example** : `"string"`|string|
|**properties**  <br>*optional*  <br>*read-only*|A property bag with details about the service  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**startTime**  <br>*optional*  <br>*read-only*|Start time of service  <br>**Example** : `"string"`|string|
|**status**  <br>*optional*|Operational status  <br>**Example** : `"string"`|string|
|**uid**  <br>*optional*  <br>*read-only*|Value generated at bootstrap by each instance of the service and<br>used to correlate logs coming from the same instance. The value<br>changes every time the service starts.  <br>**Example** : `"string"`|string|
|**upTime**  <br>*optional*  <br>*read-only*|Up time of service  <br>**Example** : `0`|integer (int64)|



