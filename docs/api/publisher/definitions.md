
<a name="definitions"></a>
## Definitions

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


<a name="publishbulkrequestapimodel"></a>
### PublishBulkRequestApiModel
Publish in bulk request


|Name|Description|Schema|
|---|---|---|
|**Header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**NodesToAdd**  <br>*optional*|Node to add|< [PublishedItemApiModel](definitions.md#publisheditemapimodel) > array|
|**NodesToRemove**  <br>*optional*|Node to remove|< string > array|


<a name="publishbulkresponseapimodel"></a>
### PublishBulkResponseApiModel
Result of bulk request


|Name|Description|Schema|
|---|---|---|
|**NodesToAdd**  <br>*optional*|Node to add|< [ServiceResultApiModel](definitions.md#serviceresultapimodel) > array|
|**NodesToRemove**  <br>*optional*|Node to remove|< [ServiceResultApiModel](definitions.md#serviceresultapimodel) > array|


<a name="publishstartrequestapimodel"></a>
### PublishStartRequestApiModel
Publish request


|Name|Schema|
|---|---|
|**header**  <br>*optional*|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**item**  <br>*required*|[PublishedItemApiModel](definitions.md#publisheditemapimodel)|


<a name="publishstartresponseapimodel"></a>
### PublishStartResponseApiModel
Result of publish request


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="publishstoprequestapimodel"></a>
### PublishStopRequestApiModel
Unpublish request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*required*|Node of published item to unpublish|string|


<a name="publishstopresponseapimodel"></a>
### PublishStopResponseApiModel
Result of publish stop request


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="publisheditemapimodel"></a>
### PublishedItemApiModel
A monitored and published item


|Name|Description|Schema|
|---|---|---|
|**displayName**  <br>*optional*|Display name of the node to monitor|string|
|**nodeId**  <br>*required*|Node to monitor|string|
|**publishingInterval**  <br>*optional*|Publishing interval to use|string (date-span)|
|**samplingInterval**  <br>*optional*|Sampling interval to use|string (date-span)|


<a name="publisheditemlistrequestapimodel"></a>
### PublishedItemListRequestApiModel
Request list of published items


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token or null to start|string|


<a name="publisheditemlistresponseapimodel"></a>
### PublishedItemListResponseApiModel
List of published nodes


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Monitored items|< [PublishedItemApiModel](definitions.md#publisheditemapimodel) > array|


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



