
<a name="definitions"></a>
## Definitions

<a name="additionaldata"></a>
### AdditionalData
Flags that are set by the historian when
returning archived values.

*Type* : enum (None, Partial, ExtraData, MultipleValues)


<a name="aggregateconfigurationmodel"></a>
### AggregateConfigurationModel
Aggregate configuration


|Name|Description|Schema|
|---|---|---|
|**percentDataBad**  <br>*optional*|Percent of data that is bad|integer (int32)|
|**percentDataGood**  <br>*optional*|Percent of data that is good|integer (int32)|
|**treatUncertainAsBad**  <br>*optional*|Whether to treat uncertain as bad|boolean|
|**useSlopedExtrapolation**  <br>*optional*|Whether to use sloped extrapolation.|boolean|


<a name="applicationinfomodel"></a>
### ApplicationInfoModel
Application info model


|Name|Description|Schema|
|---|---|---|
|**applicationId**  <br>*required*|Unique application id|string|
|**applicationName**  <br>*optional*|Default name of application|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*required*|Unique application uri|string|
|**capabilities**  <br>*optional*|The capabilities advertised by the server.|< string > array|
|**created**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**discovererId**  <br>*optional*|Discoverer that registered the application|string|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**hostAddresses**  <br>*optional*|Host addresses of server application or null|< string > array|
|**locale**  <br>*optional*|Locale of default name - defaults to "en"|string|
|**localizedNames**  <br>*optional*|Localized Names of application keyed on locale|< string, string > map|
|**notSeenSince**  <br>*optional*|Last time application was seen if not visible|string (date-time)|
|**productUri**  <br>*optional*|Product uri|string|
|**siteId**  <br>*optional*|Site of the application  <br>**Example** : `"productionlineA"`|string|
|**updated**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|


<a name="applicationregistrationmodel"></a>
### ApplicationRegistrationModel
Application with optional list of endpoints


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*required*||[ApplicationInfoModel](definitions.md#applicationinfomodel)|
|**endpoints**  <br>*optional*|List of endpoints for it|< [EndpointRegistrationModel](definitions.md#endpointregistrationmodel) > array|


<a name="applicationtype"></a>
### ApplicationType
Application type

*Type* : enum (Server, Client, ClientAndServer, DiscoveryServer)


<a name="attributereadrequestmodel"></a>
### AttributeReadRequestModel
Attribute to read


|Name|Description|Schema|
|---|---|---|
|**attribute**  <br>*required*||[NodeAttribute](definitions.md#nodeattribute)|
|**nodeId**  <br>*required*|Node to read from or write to (mandatory)  <br>**Minimum length** : `1`|string|


<a name="attributereadresponsemodel"></a>
### AttributeReadResponseModel
Attribute value read


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**value**  <br>*required*|Attribute value|object|


<a name="attributewriterequestmodel"></a>
### AttributeWriteRequestModel
Attribute and value to write to it


|Name|Description|Schema|
|---|---|---|
|**attribute**  <br>*required*||[NodeAttribute](definitions.md#nodeattribute)|
|**nodeId**  <br>*required*|Node to write to (mandatory)  <br>**Minimum length** : `1`|string|
|**value**  <br>*required*|Value to write (mandatory)|object|


<a name="attributewriteresponsemodel"></a>
### AttributeWriteResponseModel
Attribute write result


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="authenticationmethodmodel"></a>
### AuthenticationMethodModel
Authentication Method model


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*|Method specific configuration|object|
|**credentialType**  <br>*optional*||[CredentialType](definitions.md#credentialtype)|
|**id**  <br>*required*|Method id  <br>**Minimum length** : `1`|string|
|**securityPolicy**  <br>*optional*|Security policy to use when passing credential.|string|


<a name="browsedirection"></a>
### BrowseDirection
Direction to browse

*Type* : enum (Forward, Backward, Both)


<a name="browsefirstrequestmodel"></a>
### BrowseFirstRequestModel
Browse request model


|Name|Description|Schema|
|---|---|---|
|**direction**  <br>*optional*||[BrowseDirection](definitions.md#browsedirection)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**maxReferencesToReturn**  <br>*optional*|Max number of references to return. There might<br>be less returned as this is up to the client<br>restrictions.  Set to 0 to return no references<br>or target nodes.<br>(default is decided by client e.g. 60)|integer (int64)|
|**noSubtypes**  <br>*optional*|Whether to include subtypes of the reference type.<br>(default is false)|boolean|
|**nodeClassFilter**  <br>*optional*|Filter returned target nodes by only returning<br>nodes that have classes defined in this array.<br>(default: null - all targets are returned)|enum (Object, Variable, Method, ObjectType, VariableType, ReferenceType, DataType, View)|
|**nodeId**  <br>*optional*|Node to browse.<br>(defaults to root folder).|string|
|**nodeIdsOnly**  <br>*optional*|Whether to only return the raw node id<br>information and not read the target node.<br>(default is false)|boolean|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)|boolean|
|**referenceTypeId**  <br>*optional*|Reference types to browse.<br>(default: hierarchical).|string|
|**targetNodesOnly**  <br>*optional*|Whether to collapse all references into a set of<br>unique target nodes and not show reference<br>information.<br>(default is false)|boolean|
|**view**  <br>*optional*||[BrowseViewModel](definitions.md#browseviewmodel)|


<a name="browsefirstrequestmodelrequestenvelope"></a>
### BrowseFirstRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[BrowseFirstRequestModel](definitions.md#browsefirstrequestmodel)|


<a name="browsefirstresponsemodel"></a>
### BrowseFirstResponseModel
Browse response model


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**node**  <br>*required*||[NodeModel](definitions.md#nodemodel)|
|**references**  <br>*required*|References returned|< [NodeReferenceModel](definitions.md#nodereferencemodel) > array|


<a name="browsenextrequestmodel"></a>
### BrowseNextRequestModel
Request node browsing continuation


|Name|Description|Schema|
|---|---|---|
|**abort**  <br>*optional*|Whether to abort browse and release.<br>(default: false)|boolean|
|**continuationToken**  <br>*required*|Continuation token from previews browse request.<br>(mandatory)  <br>**Minimum length** : `1`|string|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeIdsOnly**  <br>*optional*|Whether to only return the raw node id<br>information and not read the target node.<br>(default is false)|boolean|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)|boolean|
|**targetNodesOnly**  <br>*optional*|Whether to collapse all references into a set of<br>unique target nodes and not show reference<br>information.<br>(default is false)|boolean|


<a name="browsenextrequestmodelrequestenvelope"></a>
### BrowseNextRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[BrowseNextRequestModel](definitions.md#browsenextrequestmodel)|


<a name="browsenextresponsemodel"></a>
### BrowseNextResponseModel
Result of node browse continuation


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**references**  <br>*required*|References returned|< [NodeReferenceModel](definitions.md#nodereferencemodel) > array|


<a name="browsepathrequestmodel"></a>
### BrowsePathRequestModel
Browse nodes by path


|Name|Description|Schema|
|---|---|---|
|**browsePaths**  <br>*required*|The paths to browse from node.<br>(mandatory)|< < string > array > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to browse from (defaults to root folder).|string|
|**nodeIdsOnly**  <br>*optional*|Whether to only return the raw node id<br>information and not read the target node.<br>(default is false)|boolean|
|**readVariableValues**  <br>*optional*|Whether to read variable values on target nodes.<br>(default is false)|boolean|


<a name="browsepathrequestmodelrequestenvelope"></a>
### BrowsePathRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[BrowsePathRequestModel](definitions.md#browsepathrequestmodel)|


<a name="browsepathresponsemodel"></a>
### BrowsePathResponseModel
Result of node browse continuation


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**targets**  <br>*optional*|Targets|< [NodePathTargetModel](definitions.md#nodepathtargetmodel) > array|


<a name="browsestreamchunkmodeliasyncenumerable"></a>
### BrowseStreamChunkModelIAsyncEnumerable
*Type* : object


<a name="browsestreamrequestmodel"></a>
### BrowseStreamRequestModel
Browse stream request model


|Name|Description|Schema|
|---|---|---|
|**direction**  <br>*optional*||[BrowseDirection](definitions.md#browsedirection)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**noRecurse**  <br>*optional*|Whether to not browse recursively<br>(default is false)|boolean|
|**noSubtypes**  <br>*optional*|Whether to include subtypes of the reference type.<br>(default is false)|boolean|
|**nodeClassFilter**  <br>*optional*|Filter returned target nodes by only returning<br>nodes that have classes defined in this array.<br>(default: null - all targets are returned)|enum (Object, Variable, Method, ObjectType, VariableType, ReferenceType, DataType, View)|
|**nodeId**  <br>*optional*|Start nodes to browse.<br>(defaults to root folder).|< string > array|
|**readVariableValues**  <br>*optional*|Whether to read variable values on source nodes.<br>(default is false)|boolean|
|**referenceTypeId**  <br>*optional*|Reference types to browse.<br>(default: hierarchical).|string|
|**view**  <br>*optional*||[BrowseViewModel](definitions.md#browseviewmodel)|


<a name="browsestreamrequestmodelrequestenvelope"></a>
### BrowseStreamRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[BrowseStreamRequestModel](definitions.md#browsestreamrequestmodel)|


<a name="browseviewmodel"></a>
### BrowseViewModel
View to browse


|Name|Description|Schema|
|---|---|---|
|**timestamp**  <br>*optional*|Browses at or before this timestamp.|string (date-time)|
|**version**  <br>*optional*|Browses specific version of the view.|integer (int64)|
|**viewId**  <br>*required*|Node of the view to browse  <br>**Minimum length** : `1`|string|


<a name="bytearraypublishednodecreateassetrequestmodel"></a>
### ByteArrayPublishedNodeCreateAssetRequestModel
Request to create an asset in the configuration api


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*required*|The asset configuration to use when creating the asset.|string (byte)|
|**entry**  <br>*required*||[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**waitTime**  <br>*optional*|Time to wait after the configuration is applied to perform<br>the configuration of the asset in the configuration api.<br>This is to let the server settle.|string (date-span)|


<a name="channeldiagnosticmodel"></a>
### ChannelDiagnosticModel
Channel diagnostics model


|Name|Description|Schema|
|---|---|---|
|**channelId**  <br>*optional*|The id assigned to the channel that the token<br>belongs to.|integer (int64)|
|**client**  <br>*optional*||[ChannelKeyModel](definitions.md#channelkeymodel)|
|**connection**  <br>*required*||[ConnectionModel](definitions.md#connectionmodel)|
|**createdAt**  <br>*optional*|When the token was created by the server<br>(refers to the server's clock).|string (date-time)|
|**lifetime**  <br>*optional*|The lifetime of the token|string (date-span)|
|**localIpAddress**  <br>*optional*|Effective local ip address used for the connection<br>if connected. Empty if disconnected.|string|
|**localPort**  <br>*optional*|The effective local port used when connected,<br>null if disconnected.|integer (int32)|
|**remoteIpAddress**  <br>*optional*|Effective remote ip address used for the<br>connection if connected. Empty if disconnected.|string|
|**remotePort**  <br>*optional*|The effective remote port used when connected,<br>null if disconnected.|integer (int32)|
|**server**  <br>*optional*||[ChannelKeyModel](definitions.md#channelkeymodel)|
|**sessionCreated**  <br>*optional*|When the session was created.|string (date-time)|
|**sessionId**  <br>*optional*|The session id if connected. Empty if disconnected.|string|
|**timeStamp**  <br>*required*|Timestamp of the diagnostic information|string (date-time)|
|**tokenId**  <br>*optional*|The id assigned to the token.|integer (int64)|


<a name="channeldiagnosticmodeliasyncenumerable"></a>
### ChannelDiagnosticModelIAsyncEnumerable
*Type* : object


<a name="channelkeymodel"></a>
### ChannelKeyModel
Channel token key model.


|Name|Description|Schema|
|---|---|---|
|**iv**  <br>*required*|Iv|< integer (int32) > array|
|**key**  <br>*required*|Key|< integer (int32) > array|
|**sigLen**  <br>*required*|Signature length|integer (int32)|


<a name="conditionhandlingoptionsmodel"></a>
### ConditionHandlingOptionsModel
Condition handling options model


|Name|Description|Schema|
|---|---|---|
|**snapshotInterval**  <br>*optional*|Time interval for sending pending interval snapshot in seconds.|integer (int32)|
|**updateInterval**  <br>*optional*|Time interval for sending pending interval updates in seconds.|integer (int32)|


<a name="connectiondiagnosticsmodeliasyncenumerable"></a>
### ConnectionDiagnosticsModelIAsyncEnumerable
*Type* : object


<a name="connectionmodel"></a>
### ConnectionModel
Connection model


|Name|Description|Schema|
|---|---|---|
|**diagnostics**  <br>*optional*||[DiagnosticsModel](definitions.md#diagnosticsmodel)|
|**endpoint**  <br>*required*||[EndpointModel](definitions.md#endpointmodel)|
|**group**  <br>*optional*|Connection group allows splitting connections<br>per purpose.|string|
|**locales**  <br>*optional*|Optional list of preferred locales in preference order.|< string > array|
|**options**  <br>*optional*||[ConnectionOptions](definitions.md#connectionoptions)|
|**user**  <br>*optional*||[CredentialModel](definitions.md#credentialmodel)|


<a name="connectionoptions"></a>
### ConnectionOptions
Options that can be applied to a connection

*Type* : enum (None, UseReverseConnect, NoComplexTypeSystem, NoSubscriptionTransfer, DumpDiagnostics)


<a name="contentfilterelementmodel"></a>
### ContentFilterElementModel
An expression element in the filter ast


|Name|Description|Schema|
|---|---|---|
|**filterOperands**  <br>*optional*|The operands in the element for the operator|< [FilterOperandModel](definitions.md#filteroperandmodel) > array|
|**filterOperator**  <br>*optional*||[FilterOperatorType](definitions.md#filteroperatortype)|


<a name="contentfiltermodel"></a>
### ContentFilterModel
Content filter


|Name|Description|Schema|
|---|---|---|
|**elements**  <br>*optional*|The flat list of elements in the filter AST|< [ContentFilterElementModel](definitions.md#contentfilterelementmodel) > array|


<a name="credentialmodel"></a>
### CredentialModel
Credential model. For backwards compatibility
the actual credentials to pass to the server is set
through the value property.


|Name|Schema|
|---|---|
|**type**  <br>*optional*|[CredentialType](definitions.md#credentialtype)|
|**value**  <br>*optional*|[UserIdentityModel](definitions.md#useridentitymodel)|


<a name="credentialtype"></a>
### CredentialType
Type of credentials to use for authentication

*Type* : enum (None, UserName, X509Certificate, JwtToken)


<a name="datachangetriggertype"></a>
### DataChangeTriggerType
Data change trigger

*Type* : enum (Status, StatusValue, StatusValueTimestamp)


<a name="datalocation"></a>
### DataLocation
Indicate the data location

*Type* : enum (Raw, Calculated, Interpolated)


<a name="datasetroutingmode"></a>
### DataSetRoutingMode
Specifies how OPC UA node paths are mapped to message routing paths/topics.
Controls automatic topic structure generation from OPC UA address space.
Used to create a unified namespace when publishing to message brokers
that support hierarchical routing like MQTT.

*Type* : enum (None, UseBrowseNames, UseBrowseNamesWithNamespaceIndex)


<a name="datatypemetadatamodel"></a>
### DataTypeMetadataModel
Data type metadata model


|Name|Description|Schema|
|---|---|---|
|**dataType**  <br>*optional*|The data type for the instance declaration.|string|


<a name="deadbandtype"></a>
### DeadbandType
Deadband type

*Type* : enum (Absolute, Percent)


<a name="deleteeventsdetailsmodel"></a>
### DeleteEventsDetailsModel
The events to delete


|Name|Description|Schema|
|---|---|---|
|**eventIds**  <br>*required*|Events to delete|< string (byte) > array|


<a name="deleteeventsdetailsmodelhistoryupdaterequestmodel"></a>
### DeleteEventsDetailsModelHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteEventsDetailsModel](definitions.md#deleteeventsdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="deleteeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope"></a>
### DeleteEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[DeleteEventsDetailsModelHistoryUpdateRequestModel](definitions.md#deleteeventsdetailsmodelhistoryupdaterequestmodel)|


<a name="deletevaluesattimesdetailsmodel"></a>
### DeleteValuesAtTimesDetailsModel
Deletes data at times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|The timestamps to delete|< string (date-time) > array|


<a name="deletevaluesattimesdetailsmodelhistoryupdaterequestmodel"></a>
### DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteValuesAtTimesDetailsModel](definitions.md#deletevaluesattimesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="deletevaluesattimesdetailsmodelhistoryupdaterequestmodelrequestenvelope"></a>
### DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[DeleteValuesAtTimesDetailsModelHistoryUpdateRequestModel](definitions.md#deletevaluesattimesdetailsmodelhistoryupdaterequestmodel)|


<a name="deletevaluesdetailsmodel"></a>
### DeleteValuesDetailsModel
Delete values


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to delete until|string (date-time)|
|**startTime**  <br>*optional*|Start time|string (date-time)|


<a name="deletevaluesdetailsmodelhistoryupdaterequestmodel"></a>
### DeleteValuesDetailsModelHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[DeleteValuesDetailsModel](definitions.md#deletevaluesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="deletevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope"></a>
### DeleteValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[DeleteValuesDetailsModelHistoryUpdateRequestModel](definitions.md#deletevaluesdetailsmodelhistoryupdaterequestmodel)|


<a name="diagnosticslevel"></a>
### DiagnosticsLevel
Level of diagnostics requested in responses

*Type* : enum (None, Status, Information, Debug, Verbose)


<a name="diagnosticsmodel"></a>
### DiagnosticsModel
Diagnostics configuration


|Name|Description|Schema|
|---|---|---|
|**auditId**  <br>*optional*|Client audit log entry.<br>(default: client generated)|string|
|**level**  <br>*optional*||[DiagnosticsLevel](definitions.md#diagnosticslevel)|
|**timeStamp**  <br>*optional*|Timestamp of request.<br>(default: client generated)|string (date-time)|


<a name="discoverycancelrequestmodel"></a>
### DiscoveryCancelRequestModel
Discovery cancel request


|Name|Description|Schema|
|---|---|---|
|**context**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**id**  <br>*optional*|Id of discovery request|string|


<a name="discoveryconfigmodel"></a>
### DiscoveryConfigModel
Discovery configuration api model


|Name|Description|Schema|
|---|---|---|
|**addressRangesToScan**  <br>*optional*|Address ranges to scan (null == all wired nics)|string|
|**discoveryUrls**  <br>*optional*|List of preset discovery urls to use|< string > array|
|**idleTimeBetweenScans**  <br>*optional*|Delay time between discovery sweeps|string (date-span)|
|**locales**  <br>*optional*|List of locales to filter with during discovery|< string > array|
|**maxNetworkProbes**  <br>*optional*|Max network probes that should ever run.|integer (int32)|
|**maxPortProbes**  <br>*optional*|Max port probes that should ever run.|integer (int32)|
|**minPortProbesPercent**  <br>*optional*|Probes that must always be there as percent of max.|integer (int32)|
|**networkProbeTimeout**  <br>*optional*|Network probe timeout|string (date-span)|
|**portProbeTimeout**  <br>*optional*|Port probe timeout|string (date-span)|
|**portRangesToScan**  <br>*optional*|Port ranges to scan (null == all unassigned)|string|


<a name="discoverymode"></a>
### DiscoveryMode
Discovery mode to use

*Type* : enum (Off, Local, Network, Fast, Scan)


<a name="discoveryrequestmodel"></a>
### DiscoveryRequestModel
Discovery request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[DiscoveryConfigModel](definitions.md#discoveryconfigmodel)|
|**context**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**id**  <br>*optional*|Id of discovery request|string|


<a name="endpointmodel"></a>
### EndpointModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**alternativeUrls**  <br>*optional*|Alternative endpoint urls that can be used for<br>accessing and validating the server|< string > array|
|**certificate**  <br>*optional*|Endpoint certificate thumbprint|string|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Security policy uri to use for communication.<br>default to best.|string|
|**url**  <br>*required*|Endpoint url to use to connect with  <br>**Minimum length** : `1`|string|


<a name="endpointregistrationmodel"></a>
### EndpointRegistrationModel
Endpoint registration


|Name|Description|Schema|
|---|---|---|
|**authenticationMethods**  <br>*optional*|Supported authentication methods that can be selected to<br>obtain a credential and used to interact with the endpoint.|< [AuthenticationMethodModel](definitions.md#authenticationmethodmodel) > array|
|**discovererId**  <br>*optional*|Entity that registered and can access the endpoint|string|
|**endpoint**  <br>*optional*||[EndpointModel](definitions.md#endpointmodel)|
|**endpointUrl**  <br>*optional*|Original endpoint url of the endpoint|string|
|**id**  <br>*required*|Endpoint identifier which is hashed from<br>the supervisor, site and url.  <br>**Minimum length** : `1`|string|
|**securityLevel**  <br>*optional*|Security level of the endpoint|integer (int32)|
|**siteId**  <br>*optional*|Registered site of the endpoint|string|


<a name="eventfiltermodel"></a>
### EventFilterModel
Event filter


|Name|Description|Schema|
|---|---|---|
|**selectClauses**  <br>*optional*|Select clauses|< [SimpleAttributeOperandModel](definitions.md#simpleattributeoperandmodel) > array|
|**typeDefinitionId**  <br>*optional*|Simple event Type definition node id|string|
|**whereClause**  <br>*optional*||[ContentFilterModel](definitions.md#contentfiltermodel)|


<a name="exceptiondeviationtype"></a>
### ExceptionDeviationType
Exception deviation type

*Type* : enum (AbsoluteValue, PercentOfValue, PercentOfRange, PercentOfEURange)


<a name="fileinfomodel"></a>
### FileInfoModel
File info


|Name|Description|Schema|
|---|---|---|
|**lastModified**  <br>*optional*|The time the file was last modified.|string (date-time)|
|**maxBufferSize**  <br>*optional*|The maximum number of bytes of<br>the read and write buffers.|integer (int64)|
|**mimeType**  <br>*optional*|The media type of the file based on RFC 2046.|string|
|**openCount**  <br>*optional*|The number of currently valid file handles on<br>the file.|integer (int32)|
|**size**  <br>*optional*|The size of the file in Bytes. When a file is<br>currently opened for write, the size might not be<br>accurate or available.|integer (int64)|
|**writable**  <br>*optional*|Whether the file is writable.|boolean|


<a name="fileinfomodelserviceresponse"></a>
### FileInfoModelServiceResponse
Response envelope


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**result**  <br>*optional*|[FileInfoModel](definitions.md#fileinfomodel)|


<a name="filesystemobjectmodel"></a>
### FileSystemObjectModel
File system object model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|The browse path to the filesystem object|< string > array|
|**name**  <br>*optional*|The name of the filesystem object|string|
|**nodeId**  <br>*optional*|The node id of the filesystem object|string|


<a name="filesystemobjectmodelienumerableserviceresponse"></a>
### FileSystemObjectModelIEnumerableServiceResponse
Response envelope


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**result**  <br>*optional*|Result|< [FileSystemObjectModel](definitions.md#filesystemobjectmodel) > array|


<a name="filesystemobjectmodelrequestenvelope"></a>
### FileSystemObjectModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[FileSystemObjectModel](definitions.md#filesystemobjectmodel)|


<a name="filesystemobjectmodelserviceresponse"></a>
### FileSystemObjectModelServiceResponse
Response envelope


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**result**  <br>*optional*|[FileSystemObjectModel](definitions.md#filesystemobjectmodel)|


<a name="filesystemobjectmodelserviceresponseiasyncenumerable"></a>
### FileSystemObjectModelServiceResponseIAsyncEnumerable
*Type* : object


<a name="filteroperandmodel"></a>
### FilterOperandModel
Filter operand


|Name|Description|Schema|
|---|---|---|
|**alias**  <br>*optional*|Optional alias to refer to it makeing it a<br>full blown attribute operand|string|
|**attributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**browsePath**  <br>*optional*|Browse path of attribute operand|< string > array|
|**dataType**  <br>*optional*|Data type if operand is a literal|string|
|**index**  <br>*optional*|Element reference in the outer list if<br>operand is an element operand|integer (int64)|
|**indexRange**  <br>*optional*|Index range of attribute operand|string|
|**nodeId**  <br>*optional*|Type definition node id if operand is<br>simple or full attribute operand.|string|
|**value**  <br>*optional*|Variant value if operand is a literal|object|


<a name="filteroperatortype"></a>
### FilterOperatorType
Filter operator type

*Type* : enum (Equals, IsNull, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Like, Not, Between, InList, And, Or, Cast, InView, OfType, RelatedTo, BitwiseAnd, BitwiseOr)


<a name="getconfiguredendpointsresponsemodel"></a>
### GetConfiguredEndpointsResponseModel
Result of GetConfiguredEndpoints method call


|Name|Description|Schema|
|---|---|---|
|**endpoints**  <br>*optional*|Collection of Endpoints in the configuration|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


<a name="getconfigurednodesonendpointresponsemodel"></a>
### GetConfiguredNodesOnEndpointResponseModel
Result of GetConfiguredNodesOnEndpoint method call


|Name|Description|Schema|
|---|---|---|
|**opcNodes**  <br>*optional*|Collection of Nodes configured for a particular endpoint|< [OpcNodeModel](definitions.md#opcnodemodel) > array|


<a name="heartbeatbehavior"></a>
### HeartbeatBehavior
Controls how heartbeat messages are handled for monitored items.
Heartbeats help maintain awareness of node state and connection health
even when values don't change. Can be configured globally via the
--hbb command line option. Works with heartbeat interval settings.

*Type* : enum (WatchdogLKV, WatchdogLKG, PeriodicLKV, PeriodicLKG, WatchdogLKVWithUpdatedTimestamps, WatchdogLKVDiagnosticsOnly, Reserved, PeriodicLKVDropValue, PeriodicLKGDropValue)


<a name="historiceventmodel"></a>
### HistoricEventModel
Historic event


|Name|Description|Schema|
|---|---|---|
|**eventFields**  <br>*required*|The selected fields of the event|object|


<a name="historiceventmodelarrayhistoryreadnextresponsemodel"></a>
### HistoricEventModelArrayHistoryReadNextResponseModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|< [HistoricEventModel](definitions.md#historiceventmodel) > array|


<a name="historiceventmodelarrayhistoryreadresponsemodel"></a>
### HistoricEventModelArrayHistoryReadResponseModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|< [HistoricEventModel](definitions.md#historiceventmodel) > array|


<a name="historiceventmodeliasyncenumerable"></a>
### HistoricEventModelIAsyncEnumerable
*Type* : object


<a name="historicvaluemodel"></a>
### HistoricValueModel
Historic data


|Name|Description|Schema|
|---|---|---|
|**additionalData**  <br>*optional*||[AdditionalData](definitions.md#additionaldata)|
|**dataLocation**  <br>*optional*||[DataLocation](definitions.md#datalocation)|
|**dataType**  <br>*optional*|Built in data type of the updated values|string|
|**modificationInfo**  <br>*optional*||[ModificationInfoModel](definitions.md#modificationinfomodel)|
|**serverPicoseconds**  <br>*optional*|Additional resolution for the server timestamp.|integer (int32)|
|**serverTimestamp**  <br>*optional*|The server timestamp associated with the value.|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Additional resolution for the source timestamp.|integer (int32)|
|**sourceTimestamp**  <br>*optional*|The source timestamp associated with the value.|string (date-time)|
|**status**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**value**  <br>*optional*|The value of data value.|object|


<a name="historicvaluemodelarrayhistoryreadnextresponsemodel"></a>
### HistoricValueModelArrayHistoryReadNextResponseModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|< [HistoricValueModel](definitions.md#historicvaluemodel) > array|


<a name="historicvaluemodelarrayhistoryreadresponsemodel"></a>
### HistoricValueModelArrayHistoryReadResponseModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|< [HistoricValueModel](definitions.md#historicvaluemodel) > array|


<a name="historicvaluemodeliasyncenumerable"></a>
### HistoricValueModelIAsyncEnumerable
*Type* : object


<a name="historyconfigurationmodel"></a>
### HistoryConfigurationModel
History configuration


|Name|Description|Schema|
|---|---|---|
|**aggregateConfiguration**  <br>*optional*||[AggregateConfigurationModel](definitions.md#aggregateconfigurationmodel)|
|**aggregateFunctions**  <br>*optional*|Allowed aggregate functions|< string, string > map|
|**definition**  <br>*optional*|Human readable string that specifies how<br>the value of this HistoricalDataNode is<br>calculated|string|
|**endOfArchive**  <br>*optional*|The last date of the archive|string (date-time)|
|**exceptionDeviation**  <br>*optional*|Minimum amount that the data for the<br>Node shall change in order for the change<br>to be reported to the history database|number (double)|
|**exceptionDeviationType**  <br>*optional*||[ExceptionDeviationType](definitions.md#exceptiondeviationtype)|
|**maxTimeInterval**  <br>*optional*|Specifies the maximum interval between data<br>points in the history repository<br>regardless of their value change|string (date-span)|
|**minTimeInterval**  <br>*optional*|Specifies the minimum interval between<br>data points in the history repository<br>regardless of their value change|string (date-span)|
|**serverTimestampSupported**  <br>*optional*|Server supports ServerTimestamps in addition<br>to SourceTimestamp|boolean|
|**startOfArchive**  <br>*optional*|The date before which there is no data in the<br>archive either online or offline|string (date-time)|
|**startOfOnlineArchive**  <br>*optional*|Date of the earliest data in the online archive|string (date-time)|
|**stepped**  <br>*optional*|specifies whether the historical data was<br>collected in such a manner that it should<br>be displayed as SlopedInterpolation (sloped<br>line between points) or as SteppedInterpolation<br>(vertically-connected horizontal lines<br>between points) when raw data is examined.<br>This Property also effects how some<br>Aggregates are calculated|boolean|


<a name="historyconfigurationrequestmodel"></a>
### HistoryConfigurationRequestModel
Request history configuration


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*required*|Continuation token to continue reading more<br>results.  <br>**Minimum length** : `1`|string|


<a name="historyconfigurationrequestmodelrequestenvelope"></a>
### HistoryConfigurationRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[HistoryConfigurationRequestModel](definitions.md#historyconfigurationrequestmodel)|


<a name="historyconfigurationresponsemodel"></a>
### HistoryConfigurationResponseModel
Response with history configuration


|Name|Schema|
|---|---|
|**configuration**  <br>*optional*|[HistoryConfigurationModel](definitions.md#historyconfigurationmodel)|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="historyreadnextrequestmodel"></a>
### HistoryReadNextRequestModel
Request node history read continuation


|Name|Description|Schema|
|---|---|---|
|**abort**  <br>*optional*|Abort reading after this read|boolean|
|**continuationToken**  <br>*required*|Continuation token to continue reading more<br>results.  <br>**Minimum length** : `1`|string|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="historyreadnextrequestmodelrequestenvelope"></a>
### HistoryReadNextRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[HistoryReadNextRequestModel](definitions.md#historyreadnextrequestmodel)|


<a name="historyservercapabilitiesmodel"></a>
### HistoryServerCapabilitiesModel
History Server capabilities


|Name|Description|Schema|
|---|---|---|
|**aggregateFunctions**  <br>*optional*|Supported aggregate functions|< string, string > map|
|**deleteAtTimeCapability**  <br>*optional*|Server support deleting data at times|boolean|
|**deleteEventCapability**  <br>*optional*|Server supports deleting events|boolean|
|**deleteRawCapability**  <br>*optional*|Server supports deleting raw data|boolean|
|**insertAnnotationCapability**  <br>*optional*|Allows inserting annotations|boolean|
|**insertDataCapability**  <br>*optional*|Server supports inserting data|boolean|
|**insertEventCapability**  <br>*optional*|Server supports inserting events|boolean|
|**maxReturnDataValues**  <br>*optional*|Maximum number of historic data values that will<br>be returned in a single read.|integer (int64)|
|**maxReturnEventValues**  <br>*optional*|Maximum number of events that will be returned<br>in a single read.|integer (int64)|
|**replaceDataCapability**  <br>*optional*|Server supports replacing historic data|boolean|
|**replaceEventCapability**  <br>*optional*|Server supports replacing events|boolean|
|**serverTimestampSupported**  <br>*optional*|Server supports ServerTimestamps in addition<br>to SourceTimestamp|boolean|
|**supportsHistoricData**  <br>*optional*|Server supports historic data access|boolean|
|**supportsHistoricEvents**  <br>*optional*|Server supports historic event access|boolean|
|**updateDataCapability**  <br>*optional*|Server supports updating historic data|boolean|
|**updateEventCapability**  <br>*optional*|Server supports updating events|boolean|


<a name="historyupdateoperation"></a>
### HistoryUpdateOperation
History update type

*Type* : enum (Insert, Replace, Update, Delete)


<a name="historyupdateresponsemodel"></a>
### HistoryUpdateResponseModel
History update results


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**results**  <br>*optional*|List of results from the update operation|< [ServiceResultModel](definitions.md#serviceresultmodel) > array|


<a name="instancedeclarationmodel"></a>
### InstanceDeclarationModel
Instance declaration meta data


|Name|Description|Schema|
|---|---|---|
|**browseName**  <br>*optional*|The browse name for the instance declaration.|string|
|**browsePath**  <br>*optional*|The browse path|< string > array|
|**description**  <br>*optional*|The description for the instance declaration.|string|
|**displayName**  <br>*optional*|The display name for the instance declaration.|string|
|**displayPath**  <br>*optional*|A localized path to the instance declaration.|string|
|**method**  <br>*optional*||[MethodMetadataModel](definitions.md#methodmetadatamodel)|
|**modellingRule**  <br>*optional*|The modelling rule for the instance<br>declaration (i.e. Mandatory or Optional).|string|
|**modellingRuleId**  <br>*optional*|The modelling rule node id.|string|
|**nodeClass**  <br>*optional*||[NodeClass](definitions.md#nodeclass)|
|**nodeId**  <br>*optional*|The node id for the instance.|string|
|**overriddenDeclaration**  <br>*optional*||[InstanceDeclarationModel](definitions.md#instancedeclarationmodel)|
|**rootTypeId**  <br>*optional*|The type that the declaration belongs to.|string|
|**variable**  <br>*optional*||[VariableMetadataModel](definitions.md#variablemetadatamodel)|


<a name="messageencoding"></a>
### MessageEncoding
Specifies the encoding format for OPC UA Publisher messages.
Can be combined with compression and reversibility flags to control
message format characteristics.

*Type* : enum (Uadp, Json, Xml, Avro, IsReversible, JsonReversible, IsGzipCompressed, JsonGzip, AvroGzip, JsonReversibleGzip)


<a name="messagingmode"></a>
### MessagingMode
Defines how OPC UA Publisher formats and structures messages for transport.
Each mode provides different trade-offs between message completeness,
bandwidth efficiency, and compatibility with different message consumers.

*Type* : enum (PubSub, Samples, FullNetworkMessages, FullSamples, DataSetMessages, SingleDataSetMessage, DataSets, SingleDataSet, RawDataSets, SingleRawDataSet)


<a name="methodcallargumentmodel"></a>
### MethodCallArgumentModel
Method argument model


|Name|Description|Schema|
|---|---|---|
|**dataType**  <br>*optional*|Data type Id of the value (from meta data)|string|
|**value**  <br>*optional*|Initial value or value to use|object|


<a name="methodcallrequestmodel"></a>
### MethodCallRequestModel
Call request model


|Name|Description|Schema|
|---|---|---|
|**arguments**  <br>*optional*|Arguments for the method - null means no args|< [MethodCallArgumentModel](definitions.md#methodcallargumentmodel) > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**methodBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>MethodId or from a resolved objectId to the actual<br>method node.|< string > array|
|**methodId**  <br>*optional*|Method id of method to call.|string|
|**objectBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>ObjectId to the actual object or objectType node.<br>If ObjectId is null, the root node (i=84) is used|< string > array|
|**objectId**  <br>*optional*|Context of the method, i.e. an object or object type<br>node.  If null then the method is called in the context<br>of the inverse HasComponent reference of the MethodId<br>if it exists.|string|


<a name="methodcallrequestmodelrequestenvelope"></a>
### MethodCallRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[MethodCallRequestModel](definitions.md#methodcallrequestmodel)|


<a name="methodcallresponsemodel"></a>
### MethodCallResponseModel
Method call response model


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**results**  <br>*required*|Resulting output values of method call|< [MethodCallArgumentModel](definitions.md#methodcallargumentmodel) > array|


<a name="methodmetadataargumentmodel"></a>
### MethodMetadataArgumentModel
Method argument metadata model


|Name|Description|Schema|
|---|---|---|
|**arrayDimensions**  <br>*optional*|Optional Array dimension of argument|integer (int64)|
|**defaultValue**  <br>*optional*|Default value for the argument|object|
|**description**  <br>*optional*|Optional description of argument|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**name**  <br>*required*|Name of the argument|string|
|**type**  <br>*required*||[NodeModel](definitions.md#nodemodel)|
|**valueRank**  <br>*optional*||[NodeValueRank](definitions.md#nodevaluerank)|


<a name="methodmetadatamodel"></a>
### MethodMetadataModel
Method metadata model


|Name|Description|Schema|
|---|---|---|
|**inputArguments**  <br>*optional*|Input argument meta data|< [MethodMetadataArgumentModel](definitions.md#methodmetadataargumentmodel) > array|
|**objectId**  <br>*optional*|Id of object that the method is a component of|string|
|**outputArguments**  <br>*optional*|output argument meta data|< [MethodMetadataArgumentModel](definitions.md#methodmetadataargumentmodel) > array|


<a name="methodmetadatarequestmodel"></a>
### MethodMetadataRequestModel
Method metadata request model


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**methodBrowsePath**  <br>*optional*|An optional component path from the node identified by<br>MethodId to the actual method node.|< string > array|
|**methodId**  <br>*optional*|Method id of method to call.<br>(Required)|string|


<a name="methodmetadatarequestmodelrequestenvelope"></a>
### MethodMetadataRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[MethodMetadataRequestModel](definitions.md#methodmetadatarequestmodel)|


<a name="methodmetadataresponsemodel"></a>
### MethodMetadataResponseModel
Result of method metadata query


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**inputArguments**  <br>*optional*|Input argument meta data|< [MethodMetadataArgumentModel](definitions.md#methodmetadataargumentmodel) > array|
|**objectId**  <br>*optional*|Id of object that the method is a component of|string|
|**outputArguments**  <br>*optional*|output argument meta data|< [MethodMetadataArgumentModel](definitions.md#methodmetadataargumentmodel) > array|


<a name="modelchangehandlingoptionsmodel"></a>
### ModelChangeHandlingOptionsModel
Describes how model changes are published


|Name|Description|Schema|
|---|---|---|
|**rebrowseIntervalTimespan**  <br>*optional*|Rebrowse period|string (date-span)|


<a name="modificationinfomodel"></a>
### ModificationInfoModel
Modification information


|Name|Description|Schema|
|---|---|---|
|**modificationTime**  <br>*optional*|Modification time|string (date-time)|
|**updateType**  <br>*optional*||[HistoryUpdateOperation](definitions.md#historyupdateoperation)|
|**userName**  <br>*optional*|User who made the change|string|


<a name="monitoreditemwatchdogcondition"></a>
### MonitoredItemWatchdogCondition
Defines the conditions that trigger the subscription watchdog behavior.
Works in conjunction with OpcNodeWatchdogTimespan to determine when nodes
are considered "late" and DataSetWriterWatchdogBehavior to define the response.
Can be configured globally via the --mwc command line option.

*Type* : enum (WhenAllAreLate, WhenAnyIsLate)


<a name="namespaceformat"></a>
### NamespaceFormat
Namespace serialization format for node ids
and qualified names.

*Type* : enum (Uri, Index, Expanded, ExpandedWithNamespace0)


<a name="nodeaccesslevel"></a>
### NodeAccessLevel
Flags that can be set for the AccessLevel attribute.

*Type* : enum (None, CurrentRead, CurrentWrite, HistoryRead, HistoryWrite, SemanticChange, StatusWrite, TimestampWrite, NonatomicRead, NonatomicWrite, WriteFullArrayOnly)


<a name="nodeaccessrestrictions"></a>
### NodeAccessRestrictions
Flags that can be read or written in the
AccessRestrictions attribute.

*Type* : enum (None, SigningRequired, EncryptionRequired, SessionRequired)


<a name="nodeattribute"></a>
### NodeAttribute
Node attribute identifiers

*Type* : enum (NodeId, NodeClass, BrowseName, DisplayName, Description, WriteMask, UserWriteMask, IsAbstract, Symmetric, InverseName, ContainsNoLoops, EventNotifier, Value, DataType, ValueRank, ArrayDimensions, AccessLevel, UserAccessLevel, MinimumSamplingInterval, Historizing, Executable, UserExecutable, DataTypeDefinition, RolePermissions, UserRolePermissions, AccessRestrictions)


<a name="nodeclass"></a>
### NodeClass
Node class

*Type* : enum (Object, Variable, Method, ObjectType, VariableType, ReferenceType, DataType, View)


<a name="nodeeventnotifier"></a>
### NodeEventNotifier
Flags that can be set for the EventNotifier attribute.

*Type* : enum (SubscribeToEvents, HistoryRead, HistoryWrite)


<a name="nodeidmodel"></a>
### NodeIdModel
Represents an OPC UA Node identifier in string format.
Used to identify nodes in the OPC UA address space for monitoring.
Supports standard OPC UA node ID formats including:
- Namespace index and identifier (ns=0;i=85)
- String identifiers (ns=2;s=MyNode)
- GUID identifiers (ns=3;g=8599E6C4-6667-4FB7-9EA9-C6896B31DB02)
- Opaque/binary identifiers (ns=4;b=FA34E...)


|Name|Description|Schema|
|---|---|---|
|**Identifier**  <br>*optional*|The node identifier string in standard OPC UA notation.<br>Format: ns={namespace};{type}={value}<br>Examples:<br>- ns=0;i=85 (numeric identifier)<br>- ns=2;s=MyNode (string identifier)<br>- ns=3;g=8599E6C4-6667-4FB7-9EA9-C6896B31DB02 (GUID)<br>- ns=4;b=FA34E... (binary/opaque)<br>If namespace index is omitted, ns=0 is assumed.|string|


<a name="nodemetadatarequestmodel"></a>
### NodeMetadataRequestModel
Node metadata request model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional component path from the node identified by<br>NodeId to the actual node.|< string > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node id of the type.<br>(Required)|string|


<a name="nodemetadatarequestmodelrequestenvelope"></a>
### NodeMetadataRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[NodeMetadataRequestModel](definitions.md#nodemetadatarequestmodel)|


<a name="nodemetadataresponsemodel"></a>
### NodeMetadataResponseModel
Node metadata model


|Name|Description|Schema|
|---|---|---|
|**dataTypeMetadata**  <br>*optional*||[DataTypeMetadataModel](definitions.md#datatypemetadatamodel)|
|**description**  <br>*optional*|The description for the node.|string|
|**displayName**  <br>*optional*|The display name of the node.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**nethodMetadata**  <br>*optional*||[MethodMetadataModel](definitions.md#methodmetadatamodel)|
|**nodeClass**  <br>*optional*||[NodeClass](definitions.md#nodeclass)|
|**nodeId**  <br>*optional*|The node id of the node|string|
|**typeDefinition**  <br>*optional*||[TypeDefinitionModel](definitions.md#typedefinitionmodel)|
|**variableMetadata**  <br>*optional*||[VariableMetadataModel](definitions.md#variablemetadatamodel)|


<a name="nodemodel"></a>
### NodeModel
Node model


|Name|Description|Schema|
|---|---|---|
|**accessLevel**  <br>*optional*||[NodeAccessLevel](definitions.md#nodeaccesslevel)|
|**accessRestrictions**  <br>*optional*||[NodeAccessRestrictions](definitions.md#nodeaccessrestrictions)|
|**arrayDimensions**  <br>*optional*|Array dimensions of variable or variable type.<br>(default: empty array)|integer (int64)|
|**browseName**  <br>*optional*|Browse name|string|
|**children**  <br>*optional*|Whether node has children which are defined as<br>any forward hierarchical references.<br>(default: unknown)|boolean|
|**containsNoLoops**  <br>*optional*|Whether a view contains loops. Null if<br>not a view.|boolean|
|**dataType**  <br>*optional*|If variable the datatype of the variable.<br>(default: null)|string|
|**dataTypeDefinition**  <br>*optional*|Data type definition in case node is a<br>data type node and definition is available,<br>otherwise null.|object|
|**description**  <br>*optional*|Description if any|string|
|**displayName**  <br>*optional*|Display name|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**eventNotifier**  <br>*optional*||[NodeEventNotifier](definitions.md#nodeeventnotifier)|
|**executable**  <br>*optional*|If method node class, whether method can<br>be called.|boolean|
|**historizing**  <br>*optional*|Whether the value of a variable is historizing.<br>(default: false)|boolean|
|**inverseName**  <br>*optional*|Inverse name of the reference if the node is<br>a reference type, otherwise null.|string|
|**isAbstract**  <br>*optional*|Whether type is abstract, if type can<br>be abstract.  Null if not type node.<br>(default: false)|boolean|
|**minimumSamplingInterval**  <br>*optional*|Minimum sampling interval for the variable<br>value, otherwise null if not a variable node.<br>(default: null)|number (double)|
|**nodeClass**  <br>*optional*||[NodeClass](definitions.md#nodeclass)|
|**nodeId**  <br>*required*|Id of node.<br>(Mandatory).  <br>**Minimum length** : `1`|string|
|**rolePermissions**  <br>*optional*|Role permissions|< [RolePermissionModel](definitions.md#rolepermissionmodel) > array|
|**serverPicoseconds**  <br>*optional*|Pico seconds part of when value was read at server.|integer (int32)|
|**serverTimestamp**  <br>*optional*|Timestamp of when value was read at server.|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Pico seconds part of when value was read at source.|integer (int32)|
|**sourceTimestamp**  <br>*optional*|Timestamp of when value was read at source.|string (date-time)|
|**symmetric**  <br>*optional*|Whether the reference is symmetric in case<br>the node is a reference type, otherwise<br>null.|boolean|
|**typeDefinitionId**  <br>*optional*|Optional type definition of the node|string|
|**userAccessLevel**  <br>*optional*||[NodeAccessLevel](definitions.md#nodeaccesslevel)|
|**userExecutable**  <br>*optional*|If method node class, whether method can<br>be called by current user.<br>(default: false if not executable)|boolean|
|**userRolePermissions**  <br>*optional*|User Role permissions|< [RolePermissionModel](definitions.md#rolepermissionmodel) > array|
|**userWriteMask**  <br>*optional*|User write mask for the node<br>(default: 0)|integer (int64)|
|**value**  <br>*optional*|Value of variable or default value of the<br>subtyped variable in case node is a variable<br>type, otherwise null.|object|
|**valueRank**  <br>*optional*||[NodeValueRank](definitions.md#nodevaluerank)|
|**writeMask**  <br>*optional*|Default write mask for the node<br>(default: 0)|integer (int64)|


<a name="nodepathtargetmodel"></a>
### NodePathTargetModel
Node path target


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*required*|The target browse path|< string > array|
|**remainingPathIndex**  <br>*optional*|Remaining index in path|integer (int32)|
|**target**  <br>*required*||[NodeModel](definitions.md#nodemodel)|


<a name="nodereferencemodel"></a>
### NodeReferenceModel
Reference model


|Name|Description|Schema|
|---|---|---|
|**direction**  <br>*optional*||[BrowseDirection](definitions.md#browsedirection)|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**referenceTypeId**  <br>*optional*|Reference Type id|string|
|**target**  <br>*required*||[NodeModel](definitions.md#nodemodel)|


<a name="nodetype"></a>
### NodeType
The node type

*Type* : enum (Unknown, Variable, DataVariable, Property, DataType, View, Object, Event, Interface)


<a name="nodevaluerank"></a>
### NodeValueRank
Constants defined for the ValueRank attribute.

*Type* : enum (OneOrMoreDimensions, OneDimension, TwoDimensions, ScalarOrOneDimension, Any, Scalar)


<a name="opcauthenticationmode"></a>
### OpcAuthenticationMode
Specifies the authentication method used to connect to OPC UA servers.
The chosen mode determines how the Publisher authenticates itself to servers.
When using credentials or certificates, encrypted communication should be enabled
via UseSecurity or EndpointSecurityMode to protect authentication secrets.

*Type* : enum (Anonymous, UsernamePassword, Certificate)


<a name="opcnodemodel"></a>
### OpcNodeModel
Defines configuration for monitoring an OPC UA node.
Contains settings for sampling, filtering, publishing
behavior, and message routing. This model allows
fine-grained control over how each node's data is collected
and transmitted. Part of a PublishedNodesEntryModel's
OpcNodes collection.


|Name|Description|Schema|
|---|---|---|
|**AttributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**BrowsePath**  <br>*optional*|Relative path through the address space to reach target<br>node. Sequence of browse names from starting node to<br>target. Example: ["Objects", "Server", "Data",<br>"Dynamic", "Scalar"]. Allows referencing nodes through<br>hierarchical structure. Alternative to direct node ID<br>addressing.|< string > array|
|**ConditionHandling**  <br>*optional*||[ConditionHandlingOptionsModel](definitions.md#conditionhandlingoptionsmodel)|
|**CyclicReadMaxAge**  <br>*optional*|Maximum age for cached values in cyclic reads. Specified in<br>milliseconds. Default: 0 (no caching) Only applies when<br>UseCyclicRead is true. Server may return cached value if<br>within max age. Helps reduce server load in high-frequency<br>reads. Ignored when CyclicReadMaxAgeTimespan is defined.|integer (int32)|
|**CyclicReadMaxAgeTimespan**  <br>*optional*|Maximum age for cached values in cyclic reads as TimeSpan.<br>Takes precedence over CyclicReadMaxAge if both defined.<br>Only applies when UseCyclicRead is true. Default:<br>"00:00:00" (no caching) Example: "00:00:00.500" for 500ms<br>max cache age. Helps optimize read performance vs data<br>freshness.|string (date-span)|
|**DataChangeTrigger**  <br>*optional*||[DataChangeTriggerType](definitions.md#datachangetriggertype)|
|**DataSetClassFieldId**  <br>*optional*|Unique identifier for correlating fields with dataset<br>class metadata. Links monitored item data with dataset<br>class field definitions. Used to provide context and type<br>information for the field. Must match corresponding field<br>ID in dataset class metadata. Important for proper message<br>decoding by subscribers.|string (uuid)|
|**DataSetFieldId**  <br>*optional*|Custom identifier for this node in dataset messages. Used<br>as field name in message payloads if specified. Falls back<br>to DisplayName if not provided. Helps correlate data with<br>specific measurements or tags. Must be unique within a<br>dataset writer.|string|
|**DeadbandType**  <br>*optional*||[DeadbandType](definitions.md#deadbandtype)|
|**DeadbandValue**  <br>*optional*|Deadband value of the data change filter to apply. Does<br>not apply to events|number (double)|
|**DiscardNew**  <br>*optional*|Controls queue overflow behavior for monitored items.<br>True: Discard newest values when queue is full (LIFO).<br>False: Discard oldest values when queue is full (FIFO,<br>default). Use True to preserve historical data during<br>connection issues. Use False to maintain current value<br>accuracy.|boolean|
|**DisplayName**  <br>*optional*|Human-readable name for the monitored item. Used as field<br>identifier if DataSetFieldId not specified. Can be<br>overridden by actual node DisplayName if<br>FetchDisplayName=true. Helps identify data sources in<br>messages and logs. Should be unique within a dataset for<br>clear identification.|string|
|**EventFilter**  <br>*optional*||[EventFilterModel](definitions.md#eventfiltermodel)|
|**ExpandedNodeId**  <br>*optional*|Alternative node identifier with full namespace URI. Same<br>as Id but uses complete namespace URI instead of index.<br>Format: "nsu={uri};{type}={value}" Example:<br>"nsu=http://opcfoundation.org/UA/;i=2258" Provides more<br>portable node identification across servers.|string|
|**FetchDisplayName**  <br>*optional*|Retrieve node's DisplayName attribute on startup. True:<br>Query and use actual display name False: Use configured<br>DisplayName (default) Overrides DataSetFetchDisplayNames<br>setting. Used for human-readable field identification.|boolean|
|**HeartbeatBehavior**  <br>*optional*||[HeartbeatBehavior](definitions.md#heartbeatbehavior)|
|**HeartbeatInterval**  <br>*optional*|Node-specific heartbeat interval in milliseconds.<br>Overrides DefaultHeartbeatInterval from parent<br>configuration. Controls how often heartbeat messages are<br>generated. Set to 0 to disable heartbeats for this node.<br>Ignored when HeartbeatIntervalTimespan is defined.|integer (int32)|
|**HeartbeatIntervalTimespan**  <br>*optional*|Node-specific heartbeat interval as TimeSpan. Takes<br>precedence over HeartbeatInterval if both defined.<br>Overrides DefaultHeartbeatIntervalTimespan setting.<br>Provides more precise control over timing. Example:<br>"00:00:10" for 10-second interval.|string (date-span)|
|**Id**  <br>*optional*|The OPC UA node identifier string in standard notation.<br>Format: ns={namespace};{type}={value} Required field that<br>uniquely identifies the node to monitor. Examples:<br>"ns=2;s=MyTag", "ns=0;i=2258" See OPC UA Part 3 for node<br>ID format specifications.|string|
|**IndexRange**  <br>*optional*|Range specification for array or string values. Format:<br>"start:end" or "index". Examples: "0:3" (first 4<br>elements), "7" (8th element) Allows monitoring specific<br>array elements. Default: null (entire value monitored)|string|
|**MethodMetadata**  <br>*optional*||[MethodMetadataModel](definitions.md#methodmetadatamodel)|
|**ModelChangeHandling**  <br>*optional*||[ModelChangeHandlingOptionsModel](definitions.md#modelchangehandlingoptionsmodel)|
|**OpcPublishingInterval**  <br>*optional*|Client-side publishing rate in milliseconds. Controls how<br>often the server sends notifications. Must be >=<br>OpcSamplingInterval for proper operation. Overrides<br>DataSetPublishingInterval when specified. Ignored if<br>OpcPublishingIntervalTimespan is defined.|integer (int32)|
|**OpcPublishingIntervalTimespan**  <br>*optional*|OpcPublishingInterval as TimeSpan.|string (date-span)|
|**OpcSamplingInterval**  <br>*optional*|Server-side sampling rate in milliseconds. Determines how<br>often the server checks for value changes. Default from<br>DataSetSamplingInterval if not specified. Should be less<br>than or equal to OpcPublishingInterval for effective<br>sampling. Ignored when OpcSamplingIntervalTimespan is<br>defined.|integer (int32)|
|**OpcSamplingIntervalTimespan**  <br>*optional*|Server-side sampling rate as a TimeSpan. Takes precedence<br>over OpcSamplingInterval if both are defined. Provides<br>more precise control over timing than milliseconds.<br>Example: "00:00:00.100" for 100ms sampling. Should be<br>less than or equal to OpcPublishingIntervalTimespan for<br>effective sampling.|string (date-span)|
|**QualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**QueueSize**  <br>*optional*|Size of the server-side queue for this monitored item.<br>Controls how many values can be buffered during slow<br>connections. Values are discarded according to DiscardNew<br>when queue is full. Default is 1 unless otherwise<br>configured. Larger queues help prevent data loss but use<br>more server memory.|integer (int64)|
|**RegisterNode**  <br>*optional*|Optimize node access using RegisterNodes service. True:<br>Register node for faster subsequent reads False: Use<br>direct node access (default) Can improve performance for<br>frequently accessed nodes. Server must support<br>RegisterNodes service.|boolean|
|**SkipFirst**  <br>*optional*|Controls handling of initial value notification. True:<br>Suppress first value from monitored item. False: Publish<br>initial value (default). Useful when only changes are<br>relevant. Server always sends initial value on creation.|boolean|
|**Topic**  <br>*optional*|Custom routing topic/queue for this node's messages.<br>Overrides writer and writer group queue settings. Enables<br>node-specific message routing patterns. Messages are split<br>into separate network messages when nodes have different<br>topics. Format depends on transport (e.g., MQTT topic<br>syntax).|string|
|**TriggeredNodes**  <br>*optional*|Collection of dependent nodes triggered by this node. Read<br>atomically when parent node changes. Limited to one level<br>of triggering (no cascading). Useful for maintaining data<br>consistency between related measurements. Changes to<br>triggered nodes must be made through parent node's API<br>calls.|< [OpcNodeModel](definitions.md#opcnodemodel) > array|
|**TypeDefinitionId**  <br>*optional*|A type definition id that references a well known opc ua<br>type definition node for the variable represented by this<br>node entry.|string|
|**UseCyclicRead**  <br>*optional*|Use periodic reads instead of monitored items. True: Sample<br>using CyclicRead service calls False: Use standard<br>subscription monitoring (default) Useful for nodes that<br>don't support monitoring or when consistent sampling<br>timing is required. Consider CyclicReadMaxAge when<br>enabled.|boolean|


<a name="operationcontextmodel"></a>
### OperationContextModel
Operation log model


|Name|Description|Schema|
|---|---|---|
|**AuthorityId**  <br>*optional*|User|string|
|**Time**  <br>*optional*|Operation time|string (date-time)|


<a name="operationlimitsmodel"></a>
### OperationLimitsModel
Server limits


|Name|Description|Schema|
|---|---|---|
|**maxArrayLength**  <br>*optional*|Max array length supported|integer (int64)|
|**maxBrowseContinuationPoints**  <br>*optional*|Max browse continuation points|integer (int32)|
|**maxByteStringLength**  <br>*optional*|Max byte buffer length supported|integer (int64)|
|**maxHistoryContinuationPoints**  <br>*optional*|Max history continuation points|integer (int32)|
|**maxMonitoredItemsPerCall**  <br>*optional*|Max monitored items that can be updated at once.|integer (int64)|
|**maxNodesPerBrowse**  <br>*optional*|Max nodes that can be part of a single browse call.|integer (int64)|
|**maxNodesPerHistoryReadData**  <br>*optional*|Number of nodes that can be in a History Read value call|integer (int64)|
|**maxNodesPerHistoryReadEvents**  <br>*optional*|Number of nodes that can be in a History Read events call|integer (int64)|
|**maxNodesPerHistoryUpdateData**  <br>*optional*|Number of nodes that can be in a History Update call|integer (int64)|
|**maxNodesPerHistoryUpdateEvents**  <br>*optional*|Number of nodes that can be in a History events update call|integer (int64)|
|**maxNodesPerMethodCall**  <br>*optional*|Max nodes that can be read in single method call|integer (int64)|
|**maxNodesPerNodeManagement**  <br>*optional*|Max nodes that can be added or removed in a single call.|integer (int64)|
|**maxNodesPerRead**  <br>*optional*|Max nodes that can be read in single read call|integer (int64)|
|**maxNodesPerRegisterNodes**  <br>*optional*|Max nodes that can be registered at once|integer (int64)|
|**maxNodesPerTranslatePathsToNodeIds**  <br>*optional*|Max nodes that can be part of a browse path|integer (int64)|
|**maxNodesPerWrite**  <br>*optional*|Max nodes that can be read in single write call|integer (int64)|
|**maxQueryContinuationPoints**  <br>*optional*|Max query continuation points|integer (int32)|
|**maxStringLength**  <br>*optional*|Max string length supported|integer (int64)|
|**minSupportedSampleRate**  <br>*optional*|Min supported sampling rate|number (double)|


<a name="problemdetails"></a>
### ProblemDetails

|Name|Schema|
|---|---|
|**detail**  <br>*optional*|string|
|**instance**  <br>*optional*|string|
|**status**  <br>*optional*|integer (int32)|
|**title**  <br>*optional*|string|
|**type**  <br>*optional*|string|


<a name="publishbulkrequestmodel"></a>
### PublishBulkRequestModel
Publish in bulk request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodesToAdd**  <br>*optional*|Node to add|< [PublishedItemModel](definitions.md#publisheditemmodel) > array|
|**nodesToRemove**  <br>*optional*|Node to remove|< string > array|


<a name="publishbulkrequestmodelrequestenvelope"></a>
### PublishBulkRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[PublishBulkRequestModel](definitions.md#publishbulkrequestmodel)|


<a name="publishbulkresponsemodel"></a>
### PublishBulkResponseModel
Result of bulk request


|Name|Description|Schema|
|---|---|---|
|**nodesToAdd**  <br>*optional*|Node to add|< [ServiceResultModel](definitions.md#serviceresultmodel) > array|
|**nodesToRemove**  <br>*optional*|Node to remove|< [ServiceResultModel](definitions.md#serviceresultmodel) > array|


<a name="publishdiagnosticinfomodel"></a>
### PublishDiagnosticInfoModel
Model for a diagnostic info.


|Name|Description|Schema|
|---|---|---|
|**connectionRetries**  <br>*optional*|ConnectionRetries|integer (int64)|
|**encoderAvgIoTChunkUsage**  <br>*optional*|EncoderAvgIoTChunkUsage|number (double)|
|**encoderAvgIoTMessageBodySize**  <br>*optional*|EncoderAvgIoTMessageBodySize|number (double)|
|**encoderAvgNotificationsMessage**  <br>*optional*|EncoderAvgNotificationsMessage|number (double)|
|**encoderIoTMessagesProcessed**  <br>*optional*|EncoderIoTMessagesProcessed|integer (int64)|
|**encoderMaxMessageSplitRatio**  <br>*optional*|Encoder max message split ratio|number (double)|
|**encoderNotificationsDropped**  <br>*optional*|EncoderNotificationsDropped|integer (int64)|
|**encoderNotificationsProcessed**  <br>*optional*|EncoderNotificationsProcessed|integer (int64)|
|**encodingBlockInputSize**  <br>*optional*|EncodingBlockInputSize|integer (int64)|
|**encodingBlockOutputSize**  <br>*optional*|EncodingBlockOutputSize|integer (int64)|
|**endpoints**  <br>*optional*|Endpoints covered by the diagnostics model.<br>The endpoints are all part of the same writer<br>group. Specify|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|
|**estimatedIoTChunksPerDay**  <br>*optional*|EstimatedIoTChunksPerDay|number (double)|
|**ingestionDuration**  <br>*optional*|IngestionDuration|string (date-span)|
|**ingressBatchBlockBufferSize**  <br>*optional*|IngressBatchBlockBufferSize|integer (int64)|
|**ingressCyclicReads**  <br>*optional*|Number of cyclic reads of the total value changes|integer (int64)|
|**ingressDataChanges**  <br>*optional*|IngressDataChanges|integer (int64)|
|**ingressDataChangesInLastMinute**  <br>*optional*|Data changes received in the last minute|integer (int64)|
|**ingressEventNotifications**  <br>*optional*|Number of incoming event notifications|integer (int64)|
|**ingressEvents**  <br>*optional*|Total incoming events so far.|integer (int64)|
|**ingressHeartbeats**  <br>*optional*|Number of heartbeats of the total value changes|integer (int64)|
|**ingressValueChanges**  <br>*optional*|IngressValueChanges|integer (int64)|
|**ingressValueChangesInLastMinute**  <br>*optional*|Value changes received in the last minute|integer (int64)|
|**monitoredOpcNodesFailedCount**  <br>*optional*|MonitoredOpcNodesFailedCount|integer (int64)|
|**monitoredOpcNodesSucceededCount**  <br>*optional*|MonitoredOpcNodesSucceededCount|integer (int64)|
|**opcEndpointConnected**  <br>*optional*|OpcEndpointConnected|boolean|
|**outgressInputBufferCount**  <br>*optional*|OutgressInputBufferCount|integer (int64)|
|**outgressInputBufferDropped**  <br>*optional*|OutgressInputBufferDropped|integer (int64)|
|**outgressIoTMessageCount**  <br>*optional*|OutgressIoTMessageCount|integer (int64)|
|**sentMessagesPerSec**  <br>*optional*|SentMessagesPerSec|number (double)|


<a name="publishstartrequestmodel"></a>
### PublishStartRequestModel
Publish request


|Name|Schema|
|---|---|
|**header**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|
|**item**  <br>*required*|[PublishedItemModel](definitions.md#publisheditemmodel)|


<a name="publishstartrequestmodelrequestenvelope"></a>
### PublishStartRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[PublishStartRequestModel](definitions.md#publishstartrequestmodel)|


<a name="publishstartresponsemodel"></a>
### PublishStartResponseModel
Result of publish request


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="publishstoprequestmodel"></a>
### PublishStopRequestModel
Unpublish request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*required*|Node of published item to unpublish  <br>**Minimum length** : `1`|string|


<a name="publishstoprequestmodelrequestenvelope"></a>
### PublishStopRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[PublishStopRequestModel](definitions.md#publishstoprequestmodel)|


<a name="publishstopresponsemodel"></a>
### PublishStopResponseModel
Result of publish stop request


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="publisheditemlistrequestmodel"></a>
### PublishedItemListRequestModel
Request list of published items


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token or null to start|string|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="publisheditemlistrequestmodelrequestenvelope"></a>
### PublishedItemListRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[PublishedItemListRequestModel](definitions.md#publisheditemlistrequestmodel)|


<a name="publisheditemlistresponsemodel"></a>
### PublishedItemListResponseModel
List of published nodes


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Monitored items|< [PublishedItemModel](definitions.md#publisheditemmodel) > array|


<a name="publisheditemmodel"></a>
### PublishedItemModel
A monitored and published item


|Name|Description|Schema|
|---|---|---|
|**displayName**  <br>*optional*|Display name of the variable node monitored|string|
|**heartbeatInterval**  <br>*optional*|Heartbeat interval to use|string (date-span)|
|**nodeId**  <br>*required*|Variable node monitored  <br>**Minimum length** : `1`|string|
|**publishingInterval**  <br>*optional*|Publishing interval to use|string (date-span)|
|**samplingInterval**  <br>*optional*|Sampling interval to use|string (date-span)|


<a name="publishednodedeleteassetrequestmodel"></a>
### PublishedNodeDeleteAssetRequestModel
Contains entry in the published nodes configuration representing
the asset as well as an optional request header.


|Name|Description|Schema|
|---|---|---|
|**entry**  <br>*required*||[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**force**  <br>*optional*|The asset on the server is deleted no matter whether<br>the removal in the publisher configuration was successful<br>or not.|boolean|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="publishednodeexpansionmodel"></a>
### PublishedNodeExpansionModel
Node expansion configuration. Configures how an entry should
            be expanded into configuration. If a node is an object it is
            expanded to all contained variables.

If a node is an object type, all objects of that type are
            searched from the object root node. These found objects are
            then expanded into their variables.

If the node is a variable, the variable is expanded to include
            all contained variables or properties. All entries will have
            the data set field id configured as data set class id.

If a node is a variable type, then all variables of this type
            are found and added to a single writer entry. Note: That by
            themselves these variables are no further expanded.


|Name|Description|Schema|
|---|---|---|
|**createSingleWriter**  <br>*optional*|By default the api will create a new distinct<br>writer per expanded object. Objects that cannot<br>be expanded are part of the originally provided<br>writer. The writer id is then post fixed with<br>the data set field id of the object node field.<br>If true, all variables of all expanded nodes are<br>added to the originally provided entry.|boolean|
|**discardErrors**  <br>*optional*|Errors are silently discarded and only<br>successfully expanded nodes are returned.|boolean|
|**excludeRootIfInstanceNode**  <br>*optional*|If the node is an object or variable instance do<br>not include it but only the instances underneath<br>them.|boolean|
|**flattenTypeInstance**  <br>*optional*|If false, treats instance nodes found just like<br>objects that need to be expanded. In case of a<br>companion spec object type this could be set to<br>true, flattening the structure into a single<br>writer that represents the object in its entirety.<br>However, when using generic interfaces that can<br>be implemented across objects in the address<br>space and only its variables are important, it<br>might be useful to set this to false.|boolean|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**includeMethods**  <br>*optional*|Include not just variables and events but also<br>methods when expanding an object.|boolean|
|**maxDepth**  <br>*optional*|Max browse depth for object search operation or<br>when searching for an instance of a type.<br>To only expand an object to its variables set<br>this value to 0. The depth of expansion of a<br>variable itself can be controlled via the<br>Azure.IIoT.OpcUa.Publisher.Models.PublishedNodeExpansionModel.MaxLevelsToExpand" property.<br>If the root object is excluded a value of 0 is<br>equivalent to a value of 1 to get the first level<br>of objects contained in the object but not the<br>object itself, e.g. a folder object.|integer (int64)|
|**maxLevelsToExpand**  <br>*optional*|Max number of levels to expand an instance node<br>such as an object or variable into resulting<br>variables.<br>If the node is a variable instance to start with<br>but the Azure.IIoT.OpcUa.Publisher.Models.PublishedNodeExpansionModel.ExcludeRootIfInstanceNode<br>property is set to excluded it, then setting this<br>value to 0 is equivalent to a value of 1 to get<br>the first level of variables contained in the<br>variable, but not the variable itself. Otherwise<br>only the variable itelf is returned. If the node<br>is an object instance, 0 is equivalent to<br>infinite and all levels are expanded.|integer (int64)|
|**noSubTypesOfTypeNodes**  <br>*optional*|Do not consider subtypes of an object type when<br>searching for instances of the type.|boolean|
|**useBrowseNameAsDisplayName**  <br>*optional*|Use the browse name as display name for nodes. The<br>display name is rooted in the parent node from<br>which browsing occurs, or just the browse name if<br>the browse root is not a parent.|boolean|


<a name="publishednodeexpansionmodelpublishednodesentryrequestmodel"></a>
### PublishedNodeExpansionModelPublishedNodesEntryRequestModel
Wraps a request and a published nodes entry to bind to a
body more easily for api that requires an entry and additional
configuration


|Name|Schema|
|---|---|
|**entry**  <br>*required*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**request**  <br>*optional*|[PublishedNodeExpansionModel](definitions.md#publishednodeexpansionmodel)|


<a name="publishednodesentrymodel"></a>
### PublishedNodesEntryModel
Configuration model for OPC UA Publisher that defines how
            OPC UA nodes are published to messaging systems. Used to
            configure connections to OPC UA servers, setup node
            monitoring, and control message publishing.

Key features:
            - Endpoint configuration and security settings
            - Writer group and dataset organization
            - Publishing intervals and sampling controls
            - Message batching and triggering
            - Subscription and monitoring options
            - Heartbeat and watchdog behaviors
            - Security modes and authentication

For detailed configuration options, see individual
            properties.


|Name|Description|Schema|
|---|---|---|
|**BatchSize**  <br>*optional*|The number of notifications that are queued before a<br>network message is generated. Controls message batching for<br>optimizing network traffic vs latency. For historic reasons<br>the default value is 50 unless otherwise configured via the<br>--bs command line option.|integer (int64)|
|**BatchTriggerInterval**  <br>*optional*|The interval at which batched network messages are<br>published, in milliseconds. Messages are published when<br>this interval elapses or when BatchSize is reached. For<br>historic reasons the default is 10 seconds unless<br>configured via --bi. Ignored when<br>BatchTriggerIntervalTimespan is specified. Used with<br>BatchSize to optimize network traffic vs latency.|integer (int32)|
|**BatchTriggerIntervalTimespan**  <br>*optional*|The interval at which batched network messages are<br>published, as a TimeSpan. Takes precedence over<br>BatchTriggerInterval if both are defined. Messages are<br>published when this interval elapses or when BatchSize is<br>reached. Provides more precise control over publishing<br>timing than millisecond interval. Used with BatchSize to<br>optimize network traffic vs latency.|string (date-span)|
|**DataSetClassId**  <br>*optional*|The optional dataset class id as it shall appear in dataset<br>messages and dataset metadata. Used to uniquely identify the<br>type of dataset being published. Default: Guid.Empty|string (uuid)|
|**DataSetDescription**  <br>*optional*|The optional description of the dataset.|string|
|**DataSetExtensionFields**  <br>*optional*|Optional key-value pairs inserted into key frame and<br>metadata messages in the same data set. Values are<br>formatted using OPC UA Variant JSON encoding. Used to add<br>contextual information to datasets.|< string, object > map|
|**DataSetFetchDisplayNames**  <br>*optional*|Controls whether to fetch display names of monitored<br>variable nodes and use those inside messages as field<br>names. When true, fetches display names for all nodes. If<br>false, uses DisplayName value if provided; if not provided,<br>uses the node id. Can be configured via --fd command line<br>option.|boolean|
|**DataSetKeyFrameCount**  <br>*optional*|Controls key frame insertion frequency in the message<br>stream. A key frame contains all current values, while<br>delta frames only contain changes. Setting this ensures<br>periodic complete state updates, useful for late-joining<br>consumers or state synchronization. Key frames can also<br>include configured DataSetExtensionFields for additional<br>context. Default: 0 (key frames disabled)|integer (int64)|
|**DataSetName**  <br>*optional*|The optional name of the dataset as it will appear in the<br>dataset metadata. Used for identification and organization<br>of datasets.|string|
|**DataSetPublishingInterval**  <br>*optional*|The publishing interval used for a grouped set of nodes<br>under a certain DataSetWriter, expressed in milliseconds.<br>When a specific node underneath DataSetWriter defines<br>OpcPublishingInterval (or Timespan), its value will<br>overwrite this interval and potentially split the data set<br>writer into more than one subscription. Ignored when<br>DataSetPublishingIntervalTimespan is present.|integer (int32)|
|**DataSetPublishingIntervalTimespan**  <br>*optional*|The publishing interval for a dataset writer, expressed as a<br>TimeSpan value. Takes precedence over<br>DataSetPublishingInterval if defined. Provides more precise<br>control over timing than milliseconds. When overridden by<br>node-specific intervals, the writer may split into multiple<br>subscriptions.|string (date-span)|
|**DataSetRootNodeId**  <br>*optional*|A root node that all nodes that use a non rooted browse paths in the<br>dataset should start from.|string|
|**DataSetRouting**  <br>*optional*||[DataSetRoutingMode](definitions.md#datasetroutingmode)|
|**DataSetSamplingInterval**  <br>*optional*|Default sampling interval in milliseconds for all monitored<br>items in the dataset. Used if individual nodes don't<br>specify their own sampling interval. Follows OPC UA<br>specification for sampling behavior. Ignored when<br>DataSetSamplingIntervalTimespan is present. Defaults to<br>value configured via --oi command line option.|integer (int32)|
|**DataSetSamplingIntervalTimespan**  <br>*optional*|Default sampling interval as TimeSpan for all monitored<br>items in the dataset. Takes precedence over<br>DataSetSamplingInterval if both are defined. Used if<br>individual nodes don't specify their own sampling interval.<br>Provides more precise control over sampling timing. Follows<br>OPC UA specification for sampling behavior.|string (date-span)|
|**DataSetSourceUri**  <br>*optional*|Contains an uri identifier that allows correlation of the writer<br>data set source into other systems. Will be used as part of<br>cloud events header if enabled.|string|
|**DataSetSubject**  <br>*optional*|Contains an identifier that allows correlation of the writer<br>group into other systems in the context of the source. Will be<br>used as part of cloud events header if enabled.|string|
|**DataSetType**  <br>*optional*|A type definition id that references a well known opc ua type<br>definition node for the dataset represented by this entry.<br>If set it is used in context of cloud events to specify a concrete<br>type of dataset message in the cloud events type header.|string|
|**DataSetWriterGroup**  <br>*optional*|The data set writer group collecting datasets defined for a<br>certain endpoint. This attribute is used to identify the<br>session opened into the server. The default value consists<br>of the EndpointUrl string, followed by a deterministic hash<br>composed of the EndpointUrl, UseSecurity,<br>OpcAuthenticationMode, UserName and Password attributes.|string|
|**DataSetWriterId**  <br>*optional*|The unique identifier for a data set writer used to collect<br>OPC UA nodes to be semantically grouped and published with<br>the same publishing interval. When not specified, uses a<br>string representing the common publishing interval of the<br>nodes in the data set collection. This attribute uniquely<br>identifies a data set within a DataSetWriterGroup. The<br>uniqueness is determined using the provided DataSetWriterId<br>and the publishing interval of the grouped OpcNodes.|string|
|**DataSetWriterWatchdogBehavior**  <br>*optional*||[SubscriptionWatchdogBehavior](definitions.md#subscriptionwatchdogbehavior)|
|**DefaultHeartbeatBehavior**  <br>*optional*||[HeartbeatBehavior](definitions.md#heartbeatbehavior)|
|**DefaultHeartbeatInterval**  <br>*optional*|The interval in milliseconds at which to publish heartbeat<br>messages. Heartbeat acts like a watchdog that fires after<br>this interval has passed and no new value has been<br>received. A value of 0 disables heartbeat. Ignored when<br>DefaultHeartbeatIntervalTimespan is defined. See<br>heartbeat.md for detailed behavior documentation.|integer (int32)|
|**DefaultHeartbeatIntervalTimespan**  <br>*optional*|The heartbeat interval as TimeSpan for all nodes in this<br>dataset. Takes precedence over DefaultHeartbeatInterval if<br>defined. Controls how often heartbeat messages are<br>published when no value changes occur.|string (date-span)|
|**DisableSubscriptionTransfer**  <br>*optional*|Controls whether subscription transfer is disabled during<br>reconnect. When false (default), attempts to transfer<br>subscriptions on reconnect to maintain data continuity. Set<br>to true to fix interoperability issues with servers that<br>don't support subscription transfer. Can be configured<br>globally via command line options.|boolean|
|**DumpConnectionDiagnostics**  <br>*optional*|Enables detailed server diagnostics logging for the<br>connection. When enabled, provides additional diagnostic<br>information useful for troubleshooting connectivity,<br>authentication, and subscription issues. The diagnostics<br>data is included in the publisher's logs. Default: false|boolean|
|**EncryptedAuthPassword**  <br>*optional*|The encrypted password for authentication when<br>OpcAuthenticationMode is UsernamePassword. For certificate<br>authentication, contains the password to access the private<br>key. Encrypted credentials at rest can be enforced using<br>the --fce command line option. Version 2.6+ stores<br>credentials in plain text by default, while 2.5 and below<br>always encrypted them.|string|
|**EncryptedAuthUsername**  <br>*optional*|The encrypted username for authentication when<br>OpcAuthenticationMode is UsernamePassword. Encrypted<br>credentials at rest can be enforced using the --fce command<br>line option. Version 2.6+ stores credentials in plain text<br>by default, while 2.5 and below always encrypted them.|string|
|**EndpointSecurityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**EndpointSecurityPolicy**  <br>*optional*|The security policy URI to use for the endpoint connection.<br>Overrides UseSecurity setting and refines<br>EndpointSecurityMode choice. Examples include<br>"http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256".<br>If the specified policy is not available with the chosen<br>security mode, connectivity will fail. This allows enforcing<br>specific security requirements.|string|
|**EndpointUrl**  <br>*required*|The required OPC UA server endpoint URL to connect to. This<br>is the only required field in the configuration. Format:<br>"opc.tcp://host:port/path"  <br>**Minimum length** : `1`|string|
|**LastChangeDateTime**  <br>*optional*|The time the Publisher configuration was last updated.<br>Read only and informational only.|string (date-time)|
|**MaxKeepAliveCount**  <br>*optional*|Defines how many publishing timer expirations to wait<br>before sending a keep-alive message when no notifications<br>are pending. Works with SendKeepAliveDataSetMessages to<br>maintain connection awareness. Keep-alive messages help<br>detect connection issues even when no data changes are<br>occurring.|integer (int64)|
|**MessageEncoding**  <br>*optional*||[MessageEncoding](definitions.md#messageencoding)|
|**MessageRetention**  <br>*optional*|Controls message retention for this specific writer.<br>Overrides WriterGroupMessageRetention at the individual<br>writer level. Only applied if the transport technology<br>supports retention. Together with QueueName, allows<br>splitting messages across different queues with different<br>retention policies.|boolean|
|**MessageTtlTimespan**  <br>*optional*|Time-to-live duration for messages sent by this specific<br>writer. Overrides WriterGroupMessageTtlTimespan at the<br>individual writer level. Only applied if the transport<br>technology supports message TTL. Allows different TTL<br>settings for different types of data.|string (date-span)|
|**MessagingMode**  <br>*optional*||[MessagingMode](definitions.md#messagingmode)|
|**MetaDataQueueName**  <br>*optional*|The queue name to use for metadata messages from this<br>writer. Overrides the default metadata topic template.<br>Allows routing metadata to specific destinations separate<br>from data messages.|string|
|**MetaDataRetention**  <br>*optional*|Metadata retention setting for the dataset writer.|boolean|
|**MetaDataTtlTimespan**  <br>*optional*|Metadata time-to-live duration for the dataset writer.|string (date-span)|
|**MetaDataUpdateTime**  <br>*optional*|The interval in milliseconds at which metadata messages are<br>sent, even when the metadata has not changed. Only applies<br>when metadata messaging is supported or explicitly enabled.<br>Ignored when MetaDataUpdateTimeTimespan is defined.|integer (int32)|
|**MetaDataUpdateTimeTimespan**  <br>*optional*|The interval as TimeSpan at which metadata messages are<br>sent, even when metadata has not changed. Takes precedence<br>over MetaDataUpdateTime if both are defined. Only applies<br>when metadata messaging is supported or explicitly enabled.|string (date-span)|
|**NodeId**  <br>*optional*||[NodeIdModel](definitions.md#nodeidmodel)|
|**OpcAuthenticationMode**  <br>*optional*||[OpcAuthenticationMode](definitions.md#opcauthenticationmode)|
|**OpcAuthenticationPassword**  <br>*optional*|The plaintext password for UsernamePassword authentication,<br>or the password protecting the private key for Certificate<br>authentication. For Certificate mode, this must match the<br>password used when adding the certificate to the PKI store.|string|
|**OpcAuthenticationUsername**  <br>*optional*|The plaintext username for UsernamePassword authentication,<br>or the subject name of the certificate for Certificate<br>authentication. When using Certificate mode, this refers to<br>a certificate in the User certificate store of the PKI<br>configuration.|string|
|**OpcNodeWatchdogCondition**  <br>*optional*||[MonitoredItemWatchdogCondition](definitions.md#monitoreditemwatchdogcondition)|
|**OpcNodeWatchdogTimespan**  <br>*optional*|The timeout duration used to monitor whether monitored<br>items in the subscription are continuously reporting fresh<br>data. This watchdog mechanism helps detect stale data or<br>connectivity issues. When this timeout expires, the<br>configured DataSetWriterWatchdogBehavior is triggered based<br>on OpcNodeWatchdogCondition. Expressed as a TimeSpan value.|string (date-span)|
|**OpcNodes**  <br>*optional*|The DataSet collection grouping the nodes to be published<br>for the specific DataSetWriter. Each node can specify<br>monitoring parameters including sampling intervals, deadband<br>settings, and event filtering options. Contains variable<br>nodes or event notifiers to monitor.|< [OpcNodeModel](definitions.md#opcnodemodel) > array|
|**Priority**  <br>*optional*|Priority of the writer subscription.|integer (int32)|
|**PublisherId**  <br>*optional*|Set a publisher id to use that is different form the<br>global publisher identity.|string|
|**QualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**QueueName**  <br>*optional*|Overrides the writer group queue name at the individual<br>writer level. When specified, causes network messages to be<br>split across different queues. The split also takes QoS<br>settings into account, allowing fine-grained control over<br>message routing and delivery guarantees.|string|
|**RepublishAfterTransfer**  <br>*optional*|Controls whether to republish missed values after a<br>subscription is transferred during reconnect handling. Only<br>applies when DisableSubscriptionTransfer is false. Helps<br>ensure no data is lost during connection interruptions.<br>Default: true|boolean|
|**SendKeepAliveAsKeyFrameMessages**  <br>*optional*|When sending of keep alive messages is enabled, this<br>flag controls whether the keep alive messages are sent<br>as key frames. Key frames contain all current values.|boolean|
|**SendKeepAliveDataSetMessages**  <br>*optional*|Controls whether to send keep alive messages for this<br>dataset when a subscription keep alive notification is<br>received. Keep alive messages help maintain connection<br>status awareness. Only valid if the messaging mode supports<br>keep alive messages. Default: false|boolean|
|**UseReverseConnect**  <br>*optional*|Use reverse connect to connect ot the endpoint|boolean|
|**UseSecurity**  <br>*optional*|Controls whether to use a secure OPC UA transport mode to<br>establish a session. When true, defaults to<br>SecurityMode.NotNone which requires signed or encrypted<br>communication. When false, uses SecurityMode.None with no<br>security. Can be overridden by EndpointSecurityMode and<br>EndpointSecurityPolicy settings. Use encrypted<br>communication whenever possible to protect credentials and<br>data.|boolean|
|**Version**  <br>*optional*|A monotonically increasing number identifying the change<br>version. At this point the version number is informational<br>only, but should be provided in API requests if available.<br>Not used inside file based configuration.|integer (int64)|
|**WriterGroupMessageRetention**  <br>*optional*|Controls whether messages should be retained by the<br>messaging system. Only applied if the transport technology<br>supports message retention. When true, messages are kept by<br>the broker even after delivery. Useful for late-joining<br>subscribers to receive the last known values.|boolean|
|**WriterGroupMessageTtlTimepan**  <br>*optional*|Time-to-live duration for messages sent through the writer<br>group. Only applied if the transport technology supports<br>message TTL. After this duration expires, messages may be<br>discarded by the messaging system. Used to prevent stale<br>data from being processed by consumers.|string (date-span)|
|**WriterGroupPartitions**  <br>*optional*|Specifies how many partitions to split the writer group<br>into when publishing to target topics. Used to distribute<br>message load and enable parallel processing by consumers.<br>Default is 1 partition. Particularly useful for<br>high-throughput scenarios or when using partitioned<br>queues/topics in the messaging system.|integer (int32)|
|**WriterGroupProperties**  <br>*optional*|Additional properties of the writer group that should be retained<br>with the configuration.|< string, object > map|
|**WriterGroupQualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**WriterGroupQueueName**  <br>*optional*|Overrides the default writer group topic template for<br>message routing. Used to customize where messages from this<br>writer group are published. Particularly useful when<br>publishing to MQTT topics or message queues where specific<br>routing patterns are needed.|string|
|**WriterGroupRootNodeId**  <br>*optional*|A node that represents the writer group in the server address space.<br>This is the instance id of the root node from which all datasets<br>originate. It is informational only and would not need to be<br>configured|string|
|**WriterGroupTransport**  <br>*optional*||[WriterGroupTransport](definitions.md#writergrouptransport)|
|**WriterGroupTransportConfiguration**  <br>*optional*|Pass connection string for the transport layer. This works<br>for transports that support connection strings such as<br>IoT Hub or Event Hubs. It enables overriding the default<br>connection string configured for the publisher. The transport<br>must be configured using the command line options first, and<br>can be overriden here. If it is not configured on the command<br>line first, the setting here is ignored.|string|
|**WriterGroupType**  <br>*optional*|A type that is attached to the writer group and explains the shape<br>of the writer group. It is the type definition id of the writer<br>group root node id. It is informational only and would not need<br>to be configured|string|


<a name="publishednodesentrymodelserviceresponse"></a>
### PublishedNodesEntryModelServiceResponse
Response envelope


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**result**  <br>*optional*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|


<a name="publishednodesentrymodelserviceresponseiasyncenumerable"></a>
### PublishedNodesEntryModelServiceResponseIAsyncEnumerable
*Type* : object


<a name="publishednodesresponsemodel"></a>
### PublishedNodesResponseModel
PublishNodes direct method response

*Type* : object


<a name="qos"></a>
### QoS
*Type* : enum (AtMostOnce, AtLeastOnce, ExactlyOnce)


<a name="querycompilationrequestmodel"></a>
### QueryCompilationRequestModel
Query compiler request model


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**query**  <br>*required*|The query to compile.  <br>**Minimum length** : `1`|string|
|**queryType**  <br>*optional*||[QueryType](definitions.md#querytype)|


<a name="querycompilationrequestmodelrequestenvelope"></a>
### QueryCompilationRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[QueryCompilationRequestModel](definitions.md#querycompilationrequestmodel)|


<a name="querycompilationresponsemodel"></a>
### QueryCompilationResponseModel
Query compiler response model


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|
|**eventFilter**  <br>*optional*|[EventFilterModel](definitions.md#eventfiltermodel)|


<a name="querytype"></a>
### QueryType
Query type

*Type* : enum (Event, Query)


<a name="readeventsdetailsmodel"></a>
### ReadEventsDetailsModel
Read event data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End time to read to|string (date-time)|
|**filter**  <br>*optional*||[EventFilterModel](definitions.md#eventfiltermodel)|
|**numEvents**  <br>*optional*|Number of events to read|integer (int64)|
|**startTime**  <br>*optional*|Start time to read from|string (date-time)|


<a name="readeventsdetailsmodelhistoryreadrequestmodel"></a>
### ReadEventsDetailsModelHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReadEventsDetailsModel](definitions.md#readeventsdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="readeventsdetailsmodelhistoryreadrequestmodelrequestenvelope"></a>
### ReadEventsDetailsModelHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadEventsDetailsModelHistoryReadRequestModel](definitions.md#readeventsdetailsmodelhistoryreadrequestmodel)|


<a name="readmodifiedvaluesdetailsmodel"></a>
### ReadModifiedValuesDetailsModel
Read modified data


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|The end time to read to|string (date-time)|
|**numValues**  <br>*optional*|The number of values to read|integer (int64)|
|**startTime**  <br>*optional*|The start time to read from|string (date-time)|


<a name="readmodifiedvaluesdetailsmodelhistoryreadrequestmodel"></a>
### ReadModifiedValuesDetailsModelHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReadModifiedValuesDetailsModel](definitions.md#readmodifiedvaluesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="readmodifiedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope"></a>
### ReadModifiedValuesDetailsModelHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadModifiedValuesDetailsModelHistoryReadRequestModel](definitions.md#readmodifiedvaluesdetailsmodelhistoryreadrequestmodel)|


<a name="readprocessedvaluesdetailsmodel"></a>
### ReadProcessedValuesDetailsModel
Read processed historic data


|Name|Description|Schema|
|---|---|---|
|**aggregateConfiguration**  <br>*optional*||[AggregateConfigurationModel](definitions.md#aggregateconfigurationmodel)|
|**aggregateType**  <br>*optional*|The aggregate type to apply. Can be the name of<br>the aggregate if available in the history server<br>capabilities, or otherwise will be used as a node<br>id referring to the aggregate.|string|
|**endTime**  <br>*optional*|End time to read until|string (date-time)|
|**processingInterval**  <br>*optional*|Interval to process|string (date-span)|
|**startTime**  <br>*optional*|Start time to read from.|string (date-time)|


<a name="readprocessedvaluesdetailsmodelhistoryreadrequestmodel"></a>
### ReadProcessedValuesDetailsModelHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReadProcessedValuesDetailsModel](definitions.md#readprocessedvaluesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="readprocessedvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope"></a>
### ReadProcessedValuesDetailsModelHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadProcessedValuesDetailsModelHistoryReadRequestModel](definitions.md#readprocessedvaluesdetailsmodelhistoryreadrequestmodel)|


<a name="readrequestmodel"></a>
### ReadRequestModel
Request node attribute read


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to read|< [AttributeReadRequestModel](definitions.md#attributereadrequestmodel) > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="readrequestmodelrequestenvelope"></a>
### ReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadRequestModel](definitions.md#readrequestmodel)|


<a name="readresponsemodel"></a>
### ReadResponseModel
Result of attribute reads


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**results**  <br>*required*|All results of attribute reads|< [AttributeReadResponseModel](definitions.md#attributereadresponsemodel) > array|


<a name="readvaluesattimesdetailsmodel"></a>
### ReadValuesAtTimesDetailsModel
Read data at specified times


|Name|Description|Schema|
|---|---|---|
|**reqTimes**  <br>*required*|Requested datums|< string (date-time) > array|
|**useSimpleBounds**  <br>*optional*|Whether to use simple bounds|boolean|


<a name="readvaluesattimesdetailsmodelhistoryreadrequestmodel"></a>
### ReadValuesAtTimesDetailsModelHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReadValuesAtTimesDetailsModel](definitions.md#readvaluesattimesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="readvaluesattimesdetailsmodelhistoryreadrequestmodelrequestenvelope"></a>
### ReadValuesAtTimesDetailsModelHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadValuesAtTimesDetailsModelHistoryReadRequestModel](definitions.md#readvaluesattimesdetailsmodelhistoryreadrequestmodel)|


<a name="readvaluesdetailsmodel"></a>
### ReadValuesDetailsModel
Read historic values


|Name|Description|Schema|
|---|---|---|
|**endTime**  <br>*optional*|End of period to read. Set to null if no<br>specific end time is specified.|string (date-time)|
|**numValues**  <br>*optional*|The maximum number of values returned for any Node<br>over the time range. If only one time is specified,<br>the time range shall extend to return this number<br>of values. 0 or null indicates that there is no<br>maximum.|integer (int64)|
|**returnBounds**  <br>*optional*|Whether to return the bounding values or not.|boolean|
|**startTime**  <br>*optional*|Beginning of period to read. Set to null<br>if no specific start time is specified.|string (date-time)|


<a name="readvaluesdetailsmodelhistoryreadrequestmodel"></a>
### ReadValuesDetailsModelHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[ReadValuesDetailsModel](definitions.md#readvaluesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="readvaluesdetailsmodelhistoryreadrequestmodelrequestenvelope"></a>
### ReadValuesDetailsModelHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ReadValuesDetailsModelHistoryReadRequestModel](definitions.md#readvaluesdetailsmodelhistoryreadrequestmodel)|


<a name="requestheadermodel"></a>
### RequestHeaderModel
Request header model


|Name|Description|Schema|
|---|---|---|
|**connectTimeout**  <br>*optional*|Connect timeout in ms. As opposed to the service call<br>timeout this terminates the entire transaction if<br>it takes longer than the timeout to connect a session<br>A connect and reconnect during the service call<br>resets the timeout therefore the overall time for<br>the call to complete can be longer than specified.|integer (int32)|
|**diagnostics**  <br>*optional*||[DiagnosticsModel](definitions.md#diagnosticsmodel)|
|**elevation**  <br>*optional*||[CredentialModel](definitions.md#credentialmodel)|
|**locales**  <br>*optional*|Optional list of preferred locales in preference<br>order to be used during connecting the session.<br>We suggest to use the connection object to set<br>the locales|< string > array|
|**namespaceFormat**  <br>*optional*||[NamespaceFormat](definitions.md#namespaceformat)|
|**operationTimeout**  <br>*optional*|Operation timeout in ms. This applies to every<br>operation that is invoked, not to the entire<br>transaction and overrides the configured operation<br>timeout.|integer (int32)|
|**serviceCallTimeout**  <br>*optional*|Service call timeout in ms. As opposed to the<br>operation timeout this terminates the entire<br>transaction if it takes longer than the timeout to<br>complete. Note that a connect and reconnect during<br>the service call is gated by the connect timeout<br>setting. If a connect timeout is not specified<br>this timeout is used also for connect timeout.|integer (int32)|


<a name="requestheadermodelpublishednodesentryrequestmodel"></a>
### RequestHeaderModelPublishedNodesEntryRequestModel
Wraps a request and a published nodes entry to bind to a
body more easily for api that requires an entry and additional
configuration


|Name|Schema|
|---|---|
|**entry**  <br>*required*|[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**request**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="requestheadermodelrequestenvelope"></a>
### RequestHeaderModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="rolepermissionmodel"></a>
### RolePermissionModel
Role permission model


|Name|Description|Schema|
|---|---|---|
|**permissions**  <br>*optional*||[RolePermissions](definitions.md#rolepermissions)|
|**roleId**  <br>*required*|Identifier of the role object.  <br>**Minimum length** : `1`|string|


<a name="rolepermissions"></a>
### RolePermissions
Individual permissions assigned to a role

*Type* : enum (None, Browse, ReadRolePermissions, WriteAttribute, WriteRolePermissions, WriteHistorizing, Read, Write, ReadHistory, InsertHistory, ModifyHistory, DeleteHistory, ReceiveEvents, Call, AddReference, RemoveReference, DeleteNode, AddNode)


<a name="securitymode"></a>
### SecurityMode
Specifies the security mode for OPC UA endpoint connections.
Determines how messages are protected during transmission between
the Publisher and OPC UA servers. Proper security mode selection
is crucial for protecting sensitive data and credentials.

*Type* : enum (Best, Sign, SignAndEncrypt, None, NotNone)


<a name="servercapabilitiesmodel"></a>
### ServerCapabilitiesModel
Server capabilities


|Name|Description|Schema|
|---|---|---|
|**MaxMonitoredItems**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxMonitoredItemsPerSubscription**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxMonitoredItemsQueueSize**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxSelectClauseParameters**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxSessions**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxSubscriptions**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxSubscriptionsPerSession**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**MaxWhereClauseParameters**  <br>*optional*|Supported aggregate functions|integer (int64)|
|**aggregateFunctions**  <br>*optional*|Supported aggregate functions|< string, string > map|
|**conformanceUnits**  <br>*optional*|Supported aggregate functions|< string > array|
|**modellingRules**  <br>*optional*|Supported modelling rules|< string, string > map|
|**operationLimits**  <br>*required*||[OperationLimitsModel](definitions.md#operationlimitsmodel)|
|**serverProfileArray**  <br>*optional*|Server profiles|< string > array|
|**supportedLocales**  <br>*optional*|Supported locales|< string > array|


<a name="serverendpointquerymodel"></a>
### ServerEndpointQueryModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**certificate**  <br>*optional*|Endpoint must match with this certificate thumbprint|string|
|**discoveryUrl**  <br>*optional*|Discovery url to use to query|string|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Endpoint must support this Security policy.|string|
|**url**  <br>*optional*|Endpoint url that should match the found endpoint|string|


<a name="serverregistrationrequestmodel"></a>
### ServerRegistrationRequestModel
Server registration request


|Name|Description|Schema|
|---|---|---|
|**context**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**discoveryUrl**  <br>*required*|Discovery url to use for registration  <br>**Minimum length** : `1`|string|
|**id**  <br>*optional*|User defined request id|string|


<a name="serviceresultmodel"></a>
### ServiceResultModel
Service result


|Name|Description|Schema|
|---|---|---|
|**additionalInfo**  <br>*optional*|Additional information if available|string|
|**errorMessage**  <br>*optional*|Error message in case of error or null.|string|
|**inner**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**locale**  <br>*optional*|Locale of the error message|string|
|**namespaceUri**  <br>*optional*|Namespace uri|string|
|**statusCode**  <br>*optional*|Error code - if null operation succeeded.|integer (int64)|
|**symbolicId**  <br>*optional*|Symbolic identifier|string|


<a name="setconfiguredendpointsrequestmodel"></a>
### SetConfiguredEndpointsRequestModel
Set configured endpoints request call


|Name|Description|Schema|
|---|---|---|
|**endpoints**  <br>*optional*|Endpoints and nodes that make up the configuration|< [PublishedNodesEntryModel](definitions.md#publishednodesentrymodel) > array|


<a name="simpleattributeoperandmodel"></a>
### SimpleAttributeOperandModel
Simple attribute operand model


|Name|Description|Schema|
|---|---|---|
|**attributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**browsePath**  <br>*optional*|Browse path of attribute operand|< string > array|
|**dataSetClassFieldId**  <br>*optional*|Optional data set class field id (Publisher extension)|string (uuid)|
|**displayName**  <br>*optional*|Optional display name|string|
|**indexRange**  <br>*optional*|Index range of attribute operand|string|
|**typeDefinitionId**  <br>*optional*|Type definition node id if operand is<br>simple or full attribute operand.|string|


<a name="subscriptionwatchdogbehavior"></a>
### SubscriptionWatchdogBehavior
Defines how the publisher responds when monitored items stop reporting data.
The watchdog triggers when items are late according to OpcNodeWatchdogTimespan
and OpcNodeWatchdogCondition settings. Can be configured globally via the
--dwb command line option.

*Type* : enum (Diagnostic, Reset, FailFast, ExitProcess)


<a name="testconnectionrequestmodel"></a>
### TestConnectionRequestModel
Test connection request


|Name|Schema|
|---|---|
|**header**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="testconnectionrequestmodelrequestenvelope"></a>
### TestConnectionRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[TestConnectionRequestModel](definitions.md#testconnectionrequestmodel)|


<a name="testconnectionresponsemodel"></a>
### TestConnectionResponseModel
Test connection response


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="timestampstoreturn"></a>
### TimestampsToReturn
Timestamps

*Type* : enum (Both, Source, Server, None)


<a name="typedefinitionmodel"></a>
### TypeDefinitionModel
Type definition


|Name|Description|Schema|
|---|---|---|
|**browseName**  <br>*optional*|Browse name|string|
|**description**  <br>*optional*|Description if any|string|
|**displayName**  <br>*optional*|Display name|string|
|**nodeType**  <br>*optional*||[NodeType](definitions.md#nodetype)|
|**typeDefinitionId**  <br>*required*|The node id of the type of the node  <br>**Minimum length** : `1`|string|
|**typeHierarchy**  <br>*optional*|Super types hierarchy starting from base type<br>up to Azure.IIoT.OpcUa.Publisher.Models.TypeDefinitionModel.TypeDefinitionId which is<br>not included.|< [NodeModel](definitions.md#nodemodel) > array|
|**typeMembers**  <br>*optional*|Fully inherited instance declarations of the type<br>of the node.|< [InstanceDeclarationModel](definitions.md#instancedeclarationmodel) > array|


<a name="updateeventsdetailsmodel"></a>
### UpdateEventsDetailsModel
Insert, upsert or replace historic events


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*required*|The new events to insert|< [HistoricEventModel](definitions.md#historiceventmodel) > array|
|**filter**  <br>*optional*||[EventFilterModel](definitions.md#eventfiltermodel)|


<a name="updateeventsdetailsmodelhistoryupdaterequestmodel"></a>
### UpdateEventsDetailsModelHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[UpdateEventsDetailsModel](definitions.md#updateeventsdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="updateeventsdetailsmodelhistoryupdaterequestmodelrequestenvelope"></a>
### UpdateEventsDetailsModelHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[UpdateEventsDetailsModelHistoryUpdateRequestModel](definitions.md#updateeventsdetailsmodelhistoryupdaterequestmodel)|


<a name="updatevaluesdetailsmodel"></a>
### UpdateValuesDetailsModel
Insert, upsert, or update historic values


|Name|Description|Schema|
|---|---|---|
|**values**  <br>*required*|Values to insert|< [HistoricValueModel](definitions.md#historicvaluemodel) > array|


<a name="updatevaluesdetailsmodelhistoryupdaterequestmodel"></a>
### UpdateValuesDetailsModelHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*||[UpdateValuesDetailsModel](definitions.md#updatevaluesdetailsmodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="updatevaluesdetailsmodelhistoryupdaterequestmodelrequestenvelope"></a>
### UpdateValuesDetailsModelHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[UpdateValuesDetailsModelHistoryUpdateRequestModel](definitions.md#updatevaluesdetailsmodelhistoryupdaterequestmodel)|


<a name="useridentitymodel"></a>
### UserIdentityModel
User identity model


|Name|Description|Schema|
|---|---|---|
|**password**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.UserName authentication<br>            this is the password of the user.<br><br>For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the passcode to export the configured certificate's<br>            private key.<br><br>Not used for the other authentication types.|string|
|**thumbprint**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the thumbprint of the configured certificate to use.<br>            Either Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.User or Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.Thumbprint must be<br>            used to select the certificate in the user certificate store.<br><br>Not used for the other authentication types.|string|
|**user**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.UserName authentication<br>            this is the name of the user.<br><br>For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the subject name of the certificate that has been<br>            configured.<br>            Either Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.User or Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.Thumbprint must be<br>            used to select the certificate in the user certificate store.<br><br>Not used for the other authentication types.|string|


<a name="valuereadrequestmodel"></a>
### ValueReadRequestModel
Request node value read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>an actual node.|< string > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**maxAge**  <br>*optional*|Maximum age of the value to be read in milliseconds.<br>The age of the value is based on the difference<br>between the ServerTimestamp and the time when<br>the Server starts processing the request.<br>If not supplied, the Server shall attempt to read<br>a new value from the data source.|string (date-span)|
|**nodeId**  <br>*optional*|Node to read from (mandatory)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="valuereadrequestmodelrequestenvelope"></a>
### ValueReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ValueReadRequestModel](definitions.md#valuereadrequestmodel)|


<a name="valuereadresponsemodel"></a>
### ValueReadResponseModel
Value read response model


|Name|Description|Schema|
|---|---|---|
|**dataType**  <br>*optional*|Built in data type of the value read.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**serverPicoseconds**  <br>*optional*|Pico seconds part of when value was read at server.|integer (int32)|
|**serverTimestamp**  <br>*optional*|Timestamp of when value was read at server.|string (date-time)|
|**sourcePicoseconds**  <br>*optional*|Pico seconds part of when value was read at source.|integer (int32)|
|**sourceTimestamp**  <br>*optional*|Timestamp of when value was read at source.|string (date-time)|
|**value**  <br>*optional*|Value read|object|


<a name="valuewriterequestmodel"></a>
### ValueWriteRequestModel
Value write request model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**dataType**  <br>*optional*|A built in datatype for the value. This can<br>be a data type from browse, or a built in<br>type.<br>(default: best effort)|string|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to write|string|
|**nodeId**  <br>*optional*|Node id to write value to.|string|
|**value**  <br>*required*|Value to write. The system tries to convert<br>the value according to the data type value,<br>e.g. convert comma seperated value strings<br>into arrays.  (Mandatory)|object|


<a name="valuewriterequestmodelrequestenvelope"></a>
### ValueWriteRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[ValueWriteRequestModel](definitions.md#valuewriterequestmodel)|


<a name="valuewriteresponsemodel"></a>
### ValueWriteResponseModel
Value write response model


|Name|Schema|
|---|---|
|**errorInfo**  <br>*optional*|[ServiceResultModel](definitions.md#serviceresultmodel)|


<a name="variablemetadatamodel"></a>
### VariableMetadataModel
Variable metadata model


|Name|Description|Schema|
|---|---|---|
|**arrayDimensions**  <br>*optional*|Array dimensions of the variable.|integer (int64)|
|**dataType**  <br>*optional*||[DataTypeMetadataModel](definitions.md#datatypemetadatamodel)|
|**valueRank**  <br>*optional*||[NodeValueRank](definitions.md#nodevaluerank)|


<a name="variantvaluehistoryreadnextresponsemodel"></a>
### VariantValueHistoryReadNextResponseModel
History read continuation result


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|object|


<a name="variantvaluehistoryreadrequestmodel"></a>
### VariantValueHistoryReadRequestModel
Request node history read


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*|The HistoryReadDetailsType extension object<br>encoded in json and containing the tunneled<br>Historian reader request.|object|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**indexRange**  <br>*optional*|Index range to read, e.g. 1:2,0:1 for 2 slices<br>out of a matrix or 0:1 for the first item in<br>an array, string or bytestring.<br>See 7.22 of part 4: NumericRange.|string|
|**nodeId**  <br>*optional*|Node to read from (mandatory without browse path)|string|
|**timestampsToReturn**  <br>*optional*||[TimestampsToReturn](definitions.md#timestampstoreturn)|


<a name="variantvaluehistoryreadrequestmodelrequestenvelope"></a>
### VariantValueHistoryReadRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[VariantValueHistoryReadRequestModel](definitions.md#variantvaluehistoryreadrequestmodel)|


<a name="variantvaluehistoryreadresponsemodel"></a>
### VariantValueHistoryReadResponseModel
History read results


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation token if more results pending.|string|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**history**  <br>*required*|History as json encoded extension object|object|


<a name="variantvaluehistoryupdaterequestmodel"></a>
### VariantValueHistoryUpdateRequestModel
Request node history update


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional path from NodeId instance to<br>the actual node.|< string > array|
|**details**  <br>*required*|The HistoryUpdateDetailsType extension object<br>encoded as json Variant and containing the tunneled<br>update request for the Historian server. The value<br>is updated at edge using above node address.|object|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node to update (mandatory without browse path)|string|


<a name="variantvaluehistoryupdaterequestmodelrequestenvelope"></a>
### VariantValueHistoryUpdateRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[VariantValueHistoryUpdateRequestModel](definitions.md#variantvaluehistoryupdaterequestmodel)|


<a name="variantvaluepublishednodecreateassetrequestmodel"></a>
### VariantValuePublishedNodeCreateAssetRequestModel
Request to create an asset in the configuration api


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*required*|The asset configuration to use when creating the asset.|object|
|**entry**  <br>*required*||[PublishedNodesEntryModel](definitions.md#publishednodesentrymodel)|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**waitTime**  <br>*optional*|Time to wait after the configuration is applied to perform<br>the configuration of the asset in the configuration api.<br>This is to let the server settle.|string (date-span)|


<a name="writerequestmodel"></a>
### WriteRequestModel
Request node attribute write


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to update|< [AttributeWriteRequestModel](definitions.md#attributewriterequestmodel) > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="writerequestmodelrequestenvelope"></a>
### WriteRequestModelRequestEnvelope
Wraps a request and a connection to bind to a
body more easily for api that requires a
connection endpoint


|Name|Schema|
|---|---|
|**connection**  <br>*required*|[ConnectionModel](definitions.md#connectionmodel)|
|**request**  <br>*optional*|[WriteRequestModel](definitions.md#writerequestmodel)|


<a name="writeresponsemodel"></a>
### WriteResponseModel
Result of attribute write


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**results**  <br>*required*|All results of attribute writes|< [AttributeWriteResponseModel](definitions.md#attributewriteresponsemodel) > array|


<a name="writergrouptransport"></a>
### WriterGroupTransport
Specifies the transport technology used to publish messages from OPC Publisher.
Each transport offers different capabilities for message delivery, routing,
and quality of service. The transport choice affects how messages are delivered
and what features are available.

*Type* : enum (IoTHub, Mqtt, EventHub, Dapr, Http, FileSystem, AioMqtt, AioDss, Null)


<a name="x509certificatechainmodel"></a>
### X509CertificateChainModel
Certificate chain


|Name|Description|Schema|
|---|---|---|
|**chain**  <br>*optional*|Chain|< [X509CertificateModel](definitions.md#x509certificatemodel) > array|
|**status**  <br>*optional*|Chain validation status if validated|enum (NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)|


<a name="x509certificatemodel"></a>
### X509CertificateModel
Certificate model


|Name|Description|Schema|
|---|---|---|
|**hasPrivateKey**  <br>*optional*|Contains private key|boolean|
|**notAfterUtc**  <br>*optional*|Not after validity|string (date-time)|
|**notBeforeUtc**  <br>*optional*|Not before validity|string (date-time)|
|**pfx**  <br>*optional*|Certificate as Pkcs12|< integer (int32) > array|
|**selfSigned**  <br>*optional*|Self signed certificate|boolean|
|**serialNumber**  <br>*optional*|Serial number|string|
|**subject**  <br>*optional*|Subject|string|
|**thumbprint**  <br>*optional*|Thumbprint|string|


<a name="x509chainstatus"></a>
### X509ChainStatus
Status of x509 chain

*Type* : enum (NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)



