
<a name="definitions"></a>
## Definitions

<a name="credentialapimodel"></a>
### CredentialApiModel
Credential model


|Name|Description|Schema|
|---|---|---|
|**type**  <br>*optional*|Type of credential|enum (None, UserName, X509Certificate, JwtToken)|
|**value**  <br>*optional*|Value to pass to server|object|


<a name="diagnosticsapimodel"></a>
### DiagnosticsApiModel
Diagnostics configuration


|Name|Description|Schema|
|---|---|---|
|**auditId**  <br>*optional*|Client audit log entry.<br>(default: client generated)|string|
|**level**  <br>*optional*|Requested level of response diagnostics.<br>(default: Status)|enum (None, Status, Operations, Diagnostics, Verbose)|
|**timeStamp**  <br>*optional*|Timestamp of request.<br>(default: client generated)|string (date-time)|


<a name="publishstartrequestapimodel"></a>
### PublishStartRequestApiModel
Publish request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**item**  <br>*required*|Item to publish|[PublishedItemApiModel](definitions.md#publisheditemapimodel)|


<a name="publishstartresponseapimodel"></a>
### PublishStartResponseApiModel
Result of publish request


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="publishstoprequestapimodel"></a>
### PublishStopRequestApiModel
Unpublish request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*required*|Node of published item to unpublish|string|


<a name="publishstopresponseapimodel"></a>
### PublishStopResponseApiModel
Result of unpublish request


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="publisheditemapimodel"></a>
### PublishedItemApiModel
A monitored and published item


|Name|Description|Schema|
|---|---|---|
|**nodeId**  <br>*required*|Node to monitor|string|
|**publishingInterval**  <br>*optional*|Publishing interval to use|string|
|**samplingInterval**  <br>*optional*|Sampling interval to use|string|


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
|**diagnostics**  <br>*optional*|Optional diagnostics configuration|[DiagnosticsApiModel](definitions.md#diagnosticsapimodel)|
|**elevation**  <br>*optional*|Optional User elevation|[CredentialApiModel](definitions.md#credentialapimodel)|
|**locales**  <br>*optional*|Optional list of locales in preference order.|< string > array|


<a name="serviceresultapimodel"></a>
### ServiceResultApiModel
Service result


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*|Additional diagnostics information|object|
|**errorMessage**  <br>*optional*|Error message in case of error or null.|string|
|**statusCode**  <br>*optional*|Error code - if null operation succeeded.|integer (int32)|


<a name="statusresponseapimodel"></a>
### StatusResponseApiModel
Status response model


|Name|Description|Schema|
|---|---|---|
|**$metadata**  <br>*optional*  <br>*read-only*|Optional meta data.|< string, string > map|
|**currentTime**  <br>*optional*  <br>*read-only*|Current time|string|
|**dependencies**  <br>*optional*  <br>*read-only*|A property bag with details about the internal dependencies|< string, string > map|
|**name**  <br>*optional*|Name of this service|string|
|**properties**  <br>*optional*  <br>*read-only*|A property bag with details about the service|< string, string > map|
|**startTime**  <br>*optional*  <br>*read-only*|Start time of service|string|
|**status**  <br>*optional*|Operational status|string|
|**uid**  <br>*optional*  <br>*read-only*|Value generated at bootstrap by each instance of the service and<br>used to correlate logs coming from the same instance. The value<br>changes every time the service starts.|string|
|**upTime**  <br>*optional*  <br>*read-only*|Up time of service|integer (int64)|



