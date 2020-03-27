
<a name="definitions"></a>
## Definitions

<a name="aggregateconfigurationapimodel"></a>
### AggregateConfigurationApiModel
Aggregate configuration


|Name|Description|Schema|
|---|---|---|
|**percentDataBad**  <br>*optional*|Percent of data that is bad|integer (int32)|
|**percentDataGood**  <br>*optional*|Percent of data that is good|integer (int32)|
|**treatUncertainAsBad**  <br>*optional*|Whether to treat uncertain as bad|boolean|
|**useServerCapabilitiesDefaults**  <br>*optional*|Whether to use the default server caps|boolean|
|**useSlopedExtrapolation**  <br>*optional*|Whether to use sloped extrapolation.|boolean|


<a name="contentfilterapimodel"></a>
### ContentFilterApiModel
Content filter


|Name|Description|Schema|
|---|---|---|
|**elements**  <br>*optional*|The flat list of elements in the filter AST|< [ContentFilterElementApiModel](definitions.md#contentfilterelementapimodel) > array|


<a name="contentfilterelementapimodel"></a>
### ContentFilterElementApiModel
An expression element in the filter ast


|Name|Description|Schema|
|---|---|---|
|**filterOperands**  <br>*optional*|The operands in the element for the operator|< [FilterOperandApiModel](definitions.md#filteroperandapimodel) > array|
|**filterOperator**  <br>*optional*||[FilterOperatorType](definitions.md#filteroperatortype)|


<a name="credentialapimodel"></a>
### CredentialApiModel
Credential model


|Name|Description|Schema|
|---|---|---|
|**type**  <br>*optional*||[CredentialType](definitions.md#credentialtype)|
|**value**  <br>*optional*|Value to pass to server|string|


<a name="credentialtype"></a>
### CredentialType
Type of credentials to use for authentication

*Type* : enum (None, UserName, X509Certificate, JwtToken)


<a name="deleteeventsdetailsapimodel"></a>
### DeleteEventsDetailsApiModel
The events to delete


|Name|Description|Schema|
|---|---|---|
|**eventIds**  <br>*required*|Events to delete|< string (byte) > array|


<a name="deleteeventsdetailsapimodelhistoryupdaterequestapimodel"></a>
### DeleteEventsDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteEventsDetailsApiModel](definitions.md#deleteeventsdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="deletemodifiedvaluesdetailsapimodel"></a>
### DeleteModifiedValuesDetailsApiModel
Delete raw modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to delete until|string (date-time)|
|**startTime**  <br>*optional*|Start time|string (date-time)|


<a name="deletemodifiedvaluesdetailsapimodelhistoryupdaterequestapimodel"></a>
### DeleteModifiedValuesDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteModifiedValuesDetailsApiModel](definitions.md#deletemodifiedvaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="deletevaluesattimesdetailsapimodel"></a>
### DeleteValuesAtTimesDetailsApiModel
Deletes data at times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|The timestamps to delete|< string (date-time) > array|


<a name="deletevaluesattimesdetailsapimodelhistoryupdaterequestapimodel"></a>
### DeleteValuesAtTimesDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteValuesAtTimesDetailsApiModel](definitions.md#deletevaluesattimesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="deletevaluesdetailsapimodel"></a>
### DeleteValuesDetailsApiModel
Delete raw modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to delete until|string (date-time)|
|**startTime**  <br>*optional*|Start time|string (date-time)|


<a name="deletevaluesdetailsapimodelhistoryupdaterequestapimodel"></a>
### DeleteValuesDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteValuesDetailsApiModel](definitions.md#deletevaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="diagnosticsapimodel"></a>
### DiagnosticsApiModel
Diagnostics configuration


|Name|Description|Schema|
|---|---|---|
|**auditId**  <br>*optional*|Client audit log entry.<br>(default: client generated)|string|
|**level**  <br>*optional*||[DiagnosticsLevel](definitions.md#diagnosticslevel)|
|**timeStamp**  <br>*optional*|Timestamp of request.<br>(default: client generated)|string (date-time)|


<a name="diagnosticslevel"></a>
### DiagnosticsLevel
Level of diagnostics requested in responses

*Type* : enum (None, Status, Operations, Diagnostics, Verbose)


<a name="eventfilterapimodel"></a>
### EventFilterApiModel
Event filter


|Name|Description|Schema|
|---|---|---|
|**selectClauses**  <br>*optional*|Select statements|< [SimpleAttributeOperandApiModel](definitions.md#simpleattributeoperandapimodel) > array|
|**whereClause**  <br>*optional*||[ContentFilterApiModel](definitions.md#contentfilterapimodel)|


<a name="filteroperandapimodel"></a>
### FilterOperandApiModel
Filter operand


|Name|Description|Schema|
|---|---|---|
|**alias**  <br>*optional*|Optional alias to refer to it makeing it a<br>full blown attribute operand|string|
|**attributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**browsePath**  <br>*optional*|Browse path of attribute operand|< string > array|
|**index**  <br>*optional*|Element reference in the outer list if<br>operand is an element operand|integer (int64)|
|**indexRange**  <br>*optional*|Index range of attribute operand|string|
|**nodeId**  <br>*optional*|Type definition node id if operand is<br>simple or full attribute operand.|string|
|**value**  <br>*optional*|Variant value if operand is a literal|string|


<a name="filteroperatortype"></a>
### FilterOperatorType
Filter operator type

*Type* : enum (Equals, IsNull, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Like, Not, Between, InList, And, Or, Cast, InView, OfType, RelatedTo, BitwiseAnd, BitwiseOr)


<a name="historiceventapimodel"></a>
### HistoricEventApiModel
Historic event


|Name|Description|Schema|
|---|---|---|
|**eventFields**  <br>*optional*|The selected fields of the event|< string > array|


<a name="historiceventapimodelarrayhistoryreadnextresponseapimodel"></a>
### HistoricEventApiModelArrayHistoryReadNextResponseApiModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|


<a name="historiceventapimodelarrayhistoryreadresponseapimodel"></a>
### HistoricEventApiModelArrayHistoryReadResponseApiModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|


<a name="historicvalueapimodel"></a>
### HistoricValueApiModel
Historic data


|Name|Description|Schema|
|---|---|---|
|**modificationInfo**  <br>*optional*||[ModificationInfoApiModel](definitions.md#modificationinfoapimodel)|
|**serverPicoseconds**  <br>*optional*|Additional resolution for the server timestamp.|integer (int32)|
|**serverTimestamp**  <br>*optional*|The server timestamp associated with the value.|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Additional resolution for the source timestamp.|integer (int32)|
|**sourceTimestamp**  <br>*optional*|The source timestamp associated with the value.|string (date-time)|
|**statusCode**  <br>*optional*|The status code associated with the value.|integer (int64)|
|**value**  <br>*optional*|,<br>            The value of data value.|string|


<a name="historicvalueapimodelarrayhistoryreadnextresponseapimodel"></a>
### HistoricValueApiModelArrayHistoryReadNextResponseApiModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="historicvalueapimodelarrayhistoryreadresponseapimodel"></a>
### HistoricValueApiModelArrayHistoryReadResponseApiModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="historyreadnextrequestapimodel"></a>
### HistoryReadNextRequestApiModel
Request node history read continuation


|Name|Description|Schema|
|---|---|---|
|**abort**  <br>*optional*|Abort reading after this read|boolean|
|**continuationToken**  <br>*required*|Continuation token to continue reading more<br>results.|string|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|


<a name="historyupdateoperation"></a>
### HistoryUpdateOperation
History update type

*Type* : enum (Insert, Replace, Update, Delete)


<a name="historyupdateresponseapimodel"></a>
### HistoryUpdateResponseApiModel
History update results


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**results**  <br>*optional*|List of results from the update operation|< [ServiceResultApiModel](definitions.md#serviceresultapimodel) > array|


<a name="inserteventsdetailsapimodel"></a>
### InsertEventsDetailsApiModel
Insert historic events


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*required*|The new events to insert|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|
|**filter**  <br>*optional*||[EventFilterApiModel](definitions.md#eventfilterapimodel)|


<a name="inserteventsdetailsapimodelhistoryupdaterequestapimodel"></a>
### InsertEventsDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[InsertEventsDetailsApiModel](definitions.md#inserteventsdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="insertvaluesdetailsapimodel"></a>
### InsertValuesDetailsApiModel
Insert historic data


|Name|Description|Schema|
|---|---|---|
|**values**  <br>*required*|Values to insert|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="insertvaluesdetailsapimodelhistoryupdaterequestapimodel"></a>
### InsertValuesDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[InsertValuesDetailsApiModel](definitions.md#insertvaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="modificationinfoapimodel"></a>
### ModificationInfoApiModel
Modification information


|Name|Description|Schema|
|---|---|---|
|**modificationTime**  <br>*optional*|Modification time|string (date-time)|
|**updateType**  <br>*optional*||[HistoryUpdateOperation](definitions.md#historyupdateoperation)|
|**userName**  <br>*optional*|User who made the change|string|


<a name="nodeattribute"></a>
### NodeAttribute
Node attribute identifiers

*Type* : enum (NodeClass, BrowseName, DisplayName, Description, WriteMask, UserWriteMask, IsAbstract, Symmetric, InverseName, ContainsNoLoops, EventNotifier, Value, DataType, ValueRank, ArrayDimensions, AccessLevel, UserAccessLevel, MinimumSamplingInterval, Historizing, Executable, UserExecutable, DataTypeDefinition, RolePermissions, UserRolePermissions, AccessRestrictions)


<a name="readeventsdetailsapimodel"></a>
### ReadEventsDetailsApiModel
Read event data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to read to|string (date-time)|
|**filter**  <br>*optional*||[EventFilterApiModel](definitions.md#eventfilterapimodel)|
|**numEvents**  <br>*optional*|Number of events to read|integer (int64)|
|**startTime**  <br>*optional*|Start time to read from|string (date-time)|


<a name="readeventsdetailsapimodelhistoryreadrequestapimodel"></a>
### ReadEventsDetailsApiModelHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*||[ReadEventsDetailsApiModel](definitions.md#readeventsdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="readmodifiedvaluesdetailsapimodel"></a>
### ReadModifiedValuesDetailsApiModel
Read modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|The end time to read to|string (date-time)|
|**numValues**  <br>*optional*|The number of values to read|integer (int64)|
|**startTime**  <br>*optional*|The start time to read from|string (date-time)|


<a name="readmodifiedvaluesdetailsapimodelhistoryreadrequestapimodel"></a>
### ReadModifiedValuesDetailsApiModelHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*||[ReadModifiedValuesDetailsApiModel](definitions.md#readmodifiedvaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="readprocessedvaluesdetailsapimodel"></a>
### ReadProcessedValuesDetailsApiModel
Read processed historic data


|Name|Description|Schema|
|---|---|---|
|**aggregateConfiguration**  <br>*optional*||[AggregateConfigurationApiModel](definitions.md#aggregateconfigurationapimodel)|
|**aggregateTypeId**  <br>*optional*|The aggregate type node ids|string|
|**endTime**  <br>*optional*|End time to read until|string (date-time)|
|**processingInterval**  <br>*optional*|Interval to process|number (double)|
|**startTime**  <br>*optional*|Start time to read from.|string (date-time)|


<a name="readprocessedvaluesdetailsapimodelhistoryreadrequestapimodel"></a>
### ReadProcessedValuesDetailsApiModelHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*||[ReadProcessedValuesDetailsApiModel](definitions.md#readprocessedvaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="readvaluesattimesdetailsapimodel"></a>
### ReadValuesAtTimesDetailsApiModel
Read data at specified times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|Requested datums|< string (date-time) > array|
|**useSimpleBounds**  <br>*optional*|Whether to use simple bounds|boolean|


<a name="readvaluesattimesdetailsapimodelhistoryreadrequestapimodel"></a>
### ReadValuesAtTimesDetailsApiModelHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*||[ReadValuesAtTimesDetailsApiModel](definitions.md#readvaluesattimesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="readvaluesdetailsapimodel"></a>
### ReadValuesDetailsApiModel
Read historic values


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End of period to read. Set to null if no<br>specific end time is specified.|string (date-time)|
|**numValues**  <br>*optional*|The maximum number of values returned for any Node<br>over the time range. If only one time is specified,<br>the time range shall extend to return this number<br>of values. 0 or null indicates that there is no<br>maximum.|integer (int64)|
|**returnBounds**  <br>*optional*|Whether to return the bounding values or not.|boolean|
|**startTime**  <br>*optional*|Beginning of period to read. Set to null<br>if no specific start time is specified.|string (date-time)|


<a name="readvaluesdetailsapimodelhistoryreadrequestapimodel"></a>
### ReadValuesDetailsApiModelHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*||[ReadValuesDetailsApiModel](definitions.md#readvaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="replaceeventsdetailsapimodel"></a>
### ReplaceEventsDetailsApiModel
Replace historic events


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*required*|The events to replace|< [HistoricEventApiModel](definitions.md#historiceventapimodel) > array|
|**filter**  <br>*optional*||[EventFilterApiModel](definitions.md#eventfilterapimodel)|


<a name="replaceeventsdetailsapimodelhistoryupdaterequestapimodel"></a>
### ReplaceEventsDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReplaceEventsDetailsApiModel](definitions.md#replaceeventsdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="replacevaluesdetailsapimodel"></a>
### ReplaceValuesDetailsApiModel
Replace historic data


|Name|Description|Schema|
|---|---|---|
|**values**  <br>*required*|Values to replace|< [HistoricValueApiModel](definitions.md#historicvalueapimodel) > array|


<a name="replacevaluesdetailsapimodelhistoryupdaterequestapimodel"></a>
### ReplaceValuesDetailsApiModelHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReplaceValuesDetailsApiModel](definitions.md#replacevaluesdetailsapimodel)|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|


<a name="requestheaderapimodel"></a>
### RequestHeaderApiModel
Request header model


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*||[DiagnosticsApiModel](definitions.md#diagnosticsapimodel)|
|**elevation**  <br>*optional*||[CredentialApiModel](definitions.md#credentialapimodel)|
|**locales**  <br>*optional*|Optional list of locales in preference order.|< string > array|


<a name="serviceresultapimodel"></a>
### ServiceResultApiModel
Service result


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*|Additional diagnostics information|string|
|**errorMessage**  <br>*optional*|Error message in case of error or null.|string|
|**statusCode**  <br>*optional*|Error code - if null operation succeeded.|integer (int64)|


<a name="simpleattributeoperandapimodel"></a>
### SimpleAttributeOperandApiModel
Simple attribute operand model


|Name|Description|Schema|
|---|---|---|
|**attributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**browsePath**  <br>*optional*|Browse path of attribute operand|< string > array|
|**indexRange**  <br>*optional*|Index range of attribute operand|string|
|**nodeId**  <br>*optional*|Type definition node id if operand is<br>simple or full attribute operand.|string|


<a name="variantvaluehistoryreadnextresponseapimodel"></a>
### VariantValueHistoryReadNextResponseApiModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|string|


<a name="variantvaluehistoryreadrequestapimodel"></a>
### VariantValueHistoryReadRequestApiModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*optional*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.|string|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="variantvaluehistoryreadresponseapimodel"></a>
### VariantValueHistoryReadResponseApiModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**history**  <br>*optional*|History as json encoded extension object|string|


<a name="variantvaluehistoryupdaterequestapimodel"></a>
### VariantValueHistoryUpdateRequestApiModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.|string|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to update|string|



