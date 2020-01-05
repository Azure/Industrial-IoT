
<a name="definitions"></a>
## Definitions

<a name="attributereadrequestapimodel"></a>
### AttributeReadRequestApiModel
Attribute to read


|Name|Description|Schema|
|---|---|---|
|**attribute**  <br>*required*|Attribute to read or write|enum (NodeClass, BrowseName, DisplayName, Description, WriteMask, UserWriteMask, IsAbstract, Symmetric, InverseName, ContainsNoLoops, EventNotifier, Value, DataType, ValueRank, ArrayDimensions, AccessLevel, UserAccessLevel, MinimumSamplingInterval, Historizing, Executable, UserExecutable, DataTypeDefinition, RolePermissions, UserRolePermissions, AccessRestrictions)|
|**nodeId**  <br>*required*|Node to read from or write to (mandatory)|string|


<a name="attributereadresponseapimodel"></a>
### AttributeReadResponseApiModel
Attribute value read


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**value**  <br>*optional*|Attribute value|object|


<a name="attributewriterequestapimodel"></a>
### AttributeWriteRequestApiModel
Attribute and value to write to it


|Name|Description|Schema|
|---|---|---|
|**attribute**  <br>*required*|Attribute to write (mandatory)|enum (NodeClass, BrowseName, DisplayName, Description, WriteMask, UserWriteMask, IsAbstract, Symmetric, InverseName, ContainsNoLoops, EventNotifier, Value, DataType, ValueRank, ArrayDimensions, AccessLevel, UserAccessLevel, MinimumSamplingInterval, Historizing, Executable, UserExecutable, DataTypeDefinition, RolePermissions, UserRolePermissions, AccessRestrictions)|
|**nodeId**  <br>*required*|Node to write to (mandatory)|string|
|**value**  <br>*required*|Value to write (mandatory)|object|


<a name="attributewriteresponseapimodel"></a>
### AttributeWriteResponseApiModel
Attribute write result


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="browsenextrequestapimodel"></a>
### BrowseNextRequestApiModel
Request node browsing continuation


|Name|Description|Schema|
|---|---|---|
|**abort**  <br>*optional*|Whether to abort browse and release.<br>(default: false)  <br>**Default** : `false`|boolean|
|**continuationToken**  <br>*required*|Continuation token from previews browse request.<br>(mandatory)|string|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)  <br>**Default** : `false`|boolean|
|**targetNodesOnly**  <br>*optional*|Whether to collapse all references into a set of<br>unique target nodes and not show reference<br>information.<br>(default is false)  <br>**Default** : `false`|boolean|


<a name="browsenextresponseapimodel"></a>
### BrowseNextResponseApiModel
Result of node browse continuation


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**references**  <br>*optional*|References, if included, otherwise null.|< [NodeReferenceApiModel](definitions.md#nodereferenceapimodel) > array|


<a name="browsepathrequestapimodel"></a>
### BrowsePathRequestApiModel
Browse nodes by path


|Name|Description|Schema|
|---|---|---|
|**browsePaths**  <br>*required*|The paths to browse from node.<br>(mandatory)|< < string > array > array|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**nodeId**  <br>*optional*|Node to browse from.<br>(default: RootFolder).|string|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)  <br>**Default** : `false`|boolean|


<a name="browsepathresponseapimodel"></a>
### BrowsePathResponseApiModel
Result of node browse continuation


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**targets**  <br>*optional*|Targets|< [NodePathTargetApiModel](definitions.md#nodepathtargetapimodel) > array|


<a name="browserequestapimodel"></a>
### BrowseRequestApiModel
Browse request model


|Name|Description|Schema|
|---|---|---|
|**direction**  <br>*optional*|Direction to browse in<br>(default: forward)|enum (Forward, Backward, Both)|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**maxReferencesToReturn**  <br>*optional*|Max number of references to return. There might<br>be less returned as this is up to the client<br>restrictions.  Set to 0 to return no references<br>or target nodes.<br>(default is decided by client e.g. 60)|integer (int32)|
|**noSubtypes**  <br>*optional*|Whether to include subtypes of the reference type.<br>(default is false)  <br>**Default** : `false`|boolean|
|**nodeId**  <br>*optional*|Node to browse.<br>(default: RootFolder).|string|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)  <br>**Default** : `false`|boolean|
|**referenceTypeId**  <br>*optional*|Reference types to browse.<br>(default: hierarchical).|string|
|**targetNodesOnly**  <br>*optional*|Whether to collapse all references into a set of<br>unique target nodes and not show reference<br>information.<br>(default is false)  <br>**Default** : `false`|boolean|
|**view**  <br>*optional*|View to browse<br>(default: null = new view = All nodes).|[BrowseViewApiModel](definitions.md#browseviewapimodel)|


<a name="browseresponseapimodel"></a>
### BrowseResponseApiModel
browse response model


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**node**  <br>*optional*|Node info for the currently browsed node|[NodeApiModel](definitions.md#nodeapimodel)|
|**references**  <br>*optional*|References, if included, otherwise null.|< [NodeReferenceApiModel](definitions.md#nodereferenceapimodel) > array|


<a name="browseviewapimodel"></a>
### BrowseViewApiModel
browse view model


|Name|Description|Schema|
|---|---|---|
|**timestamp**  <br>*optional*|Browses at or before this timestamp.|string (date-time)|
|**version**  <br>*optional*|Browses specific version of the view.|integer (int32)|
|**viewId**  <br>*required*|Node of the view to browse|string|


<a name="credentialapimodel"></a>
### CredentialApiModel
Credential model


|Name|Description|Schema|
|---|---|---|
|**type**  <br>*optional*|Type of credential  <br>**Default** : `"None"`|enum (None, UserName, X509Certificate, JwtToken)|
|**value**  <br>*optional*|Value to pass to server|object|


<a name="diagnosticsapimodel"></a>
### DiagnosticsApiModel
Diagnostics configuration


|Name|Description|Schema|
|---|---|---|
|**auditId**  <br>*optional*|Client audit log entry.<br>(default: client generated)|string|
|**level**  <br>*optional*|Requested level of response diagnostics.<br>(default: Status)|enum (None, Status, Operations, Diagnostics, Verbose)|
|**timeStamp**  <br>*optional*|Timestamp of request.<br>(default: client generated)|string (date-time)|


<a name="methodcallargumentapimodel"></a>
### MethodCallArgumentApiModel
method arg model


|Name|Description|Schema|
|---|---|---|
|**dataType**  <br>*optional*|Data type Id of the value (from meta data)|string|
|**value**  <br>*optional*|Initial value or value to use|object|


<a name="methodcallrequestapimodel"></a>
### MethodCallRequestApiModel
Call request model


|Name|Description|Schema|
|---|---|---|
|**arguments**  <br>*optional*|Arguments for the method - null means no args|< [MethodCallArgumentApiModel](definitions.md#methodcallargumentapimodel) > array|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**methodBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>MethodId or from a resolved objectId to the actual<br>method node.|< string > array|
|**methodId**  <br>*optional*|Method id of method to call.|string|
|**objectBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>ObjectId to the actual object or objectType node.<br>If ObjectId is null, the root node (i=84) is used.|< string > array|
|**objectId**  <br>*optional*|Context of the method, i.e. an object or object type<br>node.|string|


<a name="methodcallresponseapimodel"></a>
### MethodCallResponseApiModel
Method call response model


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**results**  <br>*optional*|Output results|< [MethodCallArgumentApiModel](definitions.md#methodcallargumentapimodel) > array|


<a name="methodmetadataargumentapimodel"></a>
### MethodMetadataArgumentApiModel
Method argument metadata model


|Name|Description|Schema|
|---|---|---|
|**arrayDimensions**  <br>*optional*|Optional, array dimension|< integer (int32) > array|
|**defaultValue**  <br>*optional*|Default value|object|
|**description**  <br>*optional*|Optional description|string|
|**name**  <br>*optional*|Argument name|string|
|**type**  <br>*optional*|Data type node of the argument|[NodeApiModel](definitions.md#nodeapimodel)|
|**valueRank**  <br>*optional*|Optional, scalar if not set|enum (ScalarOrOneDimension, Any, Scalar, OneOrMoreDimensions, OneDimension, TwoDimensions)|


<a name="methodmetadatarequestapimodel"></a>
### MethodMetadataRequestApiModel
Method metadata request model


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**methodBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>MethodId to the actual method node.|< string > array|
|**methodId**  <br>*optional*|Method id of method to call.<br>(Required)|string|


<a name="methodmetadataresponseapimodel"></a>
### MethodMetadataResponseApiModel
Method metadata query model


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**inputArguments**  <br>*optional*|Input argument meta data|< [MethodMetadataArgumentApiModel](definitions.md#methodmetadataargumentapimodel) > array|
|**objectId**  <br>*optional*|Id of object that the method is a component of|string|
|**outputArguments**  <br>*optional*|output argument meta data|< [MethodMetadataArgumentApiModel](definitions.md#methodmetadataargumentapimodel) > array|


<a name="nodeapimodel"></a>
### NodeApiModel
Node model


|Name|Description|Schema|
|---|---|---|
|**accessLevel**  <br>*optional*|Default access level for variable node.<br>(default: 0)|enum (CurrentRead, CurrentWrite, HistoryRead, HistoryWrite, SemanticChange, StatusWrite, TimestampWrite, NonatomicRead, NonatomicWrite, WriteFullArrayOnly)|
|**accessRestrictions**  <br>*optional*|Node access restrictions if any.<br>(default: none)|enum (SigningRequired, EncryptionRequired, SessionRequired)|
|**arrayDimensions**  <br>*optional*|Array dimensions of variable or variable type.<br>(default: empty array)|< integer (int32) > array|
|**browseName**  <br>*optional*|Browse name|string|
|**children**  <br>*optional*|Whether node has children which are defined as<br>any forward hierarchical references.<br>(default: unknown)|boolean|
|**containsNoLoops**  <br>*optional*|Whether a view contains loops. Null if<br>not a view.|boolean|
|**dataType**  <br>*optional*|If variable the datatype of the variable.<br>(default: null)|string|
|**dataTypeDefinition**  <br>*optional*|Data type definition in case node is a<br>data type node and definition is available,<br>otherwise null.|object|
|**description**  <br>*optional*|Description if any|string|
|**displayName**  <br>*optional*|Display name|string|
|**eventNotifier**  <br>*optional*|If object or view and eventing, event notifier<br>to subscribe to.<br>(default: no events supported)|enum (SubscribeToEvents, HistoryRead, HistoryWrite)|
|**executable**  <br>*optional*|If method node class, whether method can<br>be called.|boolean|
|**historizing**  <br>*optional*|Whether the value of a variable is historizing.<br>(default: false)|boolean|
|**inverseName**  <br>*optional*|Inverse name of the reference if the node is<br>a reference type, otherwise null.|string|
|**isAbstract**  <br>*optional*|Whether type is abstract, if type can<br>be abstract.  Null if not type node.<br>(default: false)|boolean|
|**minimumSamplingInterval**  <br>*optional*|Minimum sampling interval for the variable<br>value, otherwise null if not a variable node.<br>(default: null)|number (double)|
|**nodeClass**  <br>*optional*|Type of node|enum (Object, Variable, Method, ObjectType, VariableType, ReferenceType, DataType, View)|
|**nodeId**  <br>*required*|Id of node.<br>(Mandatory).|string|
|**rolePermissions**  <br>*optional*|Role permissions|< [RolePermissionApiModel](definitions.md#rolepermissionapimodel) > array|
|**symmetric**  <br>*optional*|Whether the reference is symmetric in case<br>the node is a reference type, otherwise<br>null.|boolean|
|**typeDefinitionId**  <br>*optional*|Optional type definition of the node|string|
|**userAccessLevel**  <br>*optional*|User access level for variable node or null.<br>(default: 0)|enum (CurrentRead, CurrentWrite, HistoryRead, HistoryWrite, SemanticChange, StatusWrite, TimestampWrite, NonatomicRead, NonatomicWrite, WriteFullArrayOnly)|
|**userExecutable**  <br>*optional*|If method node class, whether method can<br>be called by current user.<br>(default: false if not executable)|boolean|
|**userRolePermissions**  <br>*optional*|User Role permissions|< [RolePermissionApiModel](definitions.md#rolepermissionapimodel) > array|
|**userWriteMask**  <br>*optional*|User write mask for the node<br>(default: 0)|integer (int32)|
|**value**  <br>*optional*|Value of variable or default value of the<br>subtyped variable in case node is a variable<br>type, otherwise null.|object|
|**valueRank**  <br>*optional*|Value rank of the variable data of a variable<br>or variable type, otherwise null.<br>(default: scalar = -1)|enum (ScalarOrOneDimension, Any, Scalar, OneOrMoreDimensions, OneDimension, TwoDimensions)|
|**writeMask**  <br>*optional*|Default write mask for the node<br>(default: 0)|integer (int32)|


<a name="nodepathtargetapimodel"></a>
### NodePathTargetApiModel
Node path target


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|The target browse path|< string > array|
|**remainingPathIndex**  <br>*optional*|Remaining index in path|integer (int32)|
|**target**  <br>*optional*|Target node|[NodeApiModel](definitions.md#nodeapimodel)|


<a name="nodereferenceapimodel"></a>
### NodeReferenceApiModel
reference model


|Name|Description|Schema|
|---|---|---|
|**direction**  <br>*optional*|Browse direction of reference|enum (Forward, Backward, Both)|
|**referenceTypeId**  <br>*optional*|Reference Type identifier|string|
|**target**  <br>*required*|Target node|[NodeApiModel](definitions.md#nodeapimodel)|


<a name="readrequestapimodel"></a>
### ReadRequestApiModel
Request node attribute read


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to read|< [AttributeReadRequestApiModel](definitions.md#attributereadrequestapimodel) > array|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|


<a name="readresponseapimodel"></a>
### ReadResponseApiModel
Result of attribute reads


|Name|Description|Schema|
|---|---|---|
|**results**  <br>*optional*|All results of attribute reads|< [AttributeReadResponseApiModel](definitions.md#attributereadresponseapimodel) > array|


<a name="requestheaderapimodel"></a>
### RequestHeaderApiModel
Request header model


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*|Optional diagnostics configuration|[DiagnosticsApiModel](definitions.md#diagnosticsapimodel)|
|**elevation**  <br>*optional*|Optional User elevation|[CredentialApiModel](definitions.md#credentialapimodel)|
|**locales**  <br>*optional*|Optional list of locales in preference order.|< string > array|


<a name="rolepermissionapimodel"></a>
### RolePermissionApiModel
Role permission model


|Name|Description|Schema|
|---|---|---|
|**permissions**  <br>*optional*|Permissions assigned for the role.|enum (Browse, ReadRolePermissions, WriteAttribute, WriteRolePermissions, WriteHistorizing, Read, Write, ReadHistory, InsertHistory, ModifyHistory, DeleteHistory, ReceiveEvents, Call, AddReference, RemoveReference, DeleteNode, AddNode)|
|**roleId**  <br>*required*|Identifier of the role object.|string|


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


<a name="valuereadrequestapimodel"></a>
### ValueReadRequestApiModel
Node value read request webservice api model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|


<a name="valuereadresponseapimodel"></a>
### ValueReadResponseApiModel
Value read response model


|Name|Description|Schema|
|---|---|---|
|**dataType**  <br>*optional*|Built in data type of the value read.|string|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|
|**serverPicoseconds**  <br>*optional*|Pico seconds part of when value was read at server.|integer (int32)|
|**serverTimestamp**  <br>*optional*|Timestamp of when value was read at server.|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Pico seconds part of when value was read at source.|integer (int32)|
|**sourceTimestamp**  <br>*optional*|Timestamp of when value was read at source.|string (date-time)|
|**value**  <br>*optional*|Value read|object|


<a name="valuewriterequestapimodel"></a>
### ValueWriteRequestApiModel
Value write request model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**dataType**  <br>*optional*|A built in datatype for the value. This can<br>be a data type from browse, or a built in<br>type.<br>(default: best effort)|string|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|
|**indexRange**  <br>*optional*|Index range to write|string|
|**nodeId**  <br>*optional*|Node id to to write value to.|string|
|**value**  <br>*required*|Value to write. The system tries to convert<br>the value according to the data type value,<br>e.g. convert comma seperated value strings<br>into arrays.  (Mandatory)|object|


<a name="valuewriteresponseapimodel"></a>
### ValueWriteResponseApiModel
Value write response model


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*|Service result in case of error|[ServiceResultApiModel](definitions.md#serviceresultapimodel)|


<a name="writerequestapimodel"></a>
### WriteRequestApiModel
Request node attribute write


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to update|< [AttributeWriteRequestApiModel](definitions.md#attributewriterequestapimodel) > array|
|**header**  <br>*optional*|Optional request header|[RequestHeaderApiModel](definitions.md#requestheaderapimodel)|


<a name="writeresponseapimodel"></a>
### WriteResponseApiModel
Result of attribute write


|Name|Description|Schema|
|---|---|---|
|**results**  <br>*optional*|All results of attribute writes|< [AttributeWriteResponseApiModel](definitions.md#attributewriteresponseapimodel) > array|



