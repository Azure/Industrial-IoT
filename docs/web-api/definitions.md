
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


<a name="applicationinfolistmodel"></a>
### ApplicationInfoListModel
List of registered applications


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*required*|Application infos|< [ApplicationInfoModel](definitions.md#applicationinfomodel) > array|


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


<a name="applicationregistrationquerymodel"></a>
### ApplicationRegistrationQueryModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Name of application|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*optional*|Application uri|string|
|**capability**  <br>*optional*|Application capability to query with|string|
|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**includeNotSeenSince**  <br>*optional*|Whether to include apps that were soft deleted|boolean|
|**locale**  <br>*optional*|Locale of application name - default is "en"|string|
|**productUri**  <br>*optional*|Product uri|string|
|**siteOrGatewayId**  <br>*optional*|Supervisor or site the application belongs to.|string|


<a name="applicationregistrationrequestmodel"></a>
### ApplicationRegistrationRequestModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Default name of the server or client.|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*required*|Unique application uri  <br>**Minimum length** : `1`|string|
|**capabilities**  <br>*optional*|The OPC UA defined capabilities of the server.|< string > array|
|**context**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**discoveryProfileUri**  <br>*optional*|The discovery profile uri of the server.|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server.|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**locale**  <br>*optional*|Locale of default name|string|
|**localizedNames**  <br>*optional*|Localized names key off locale id.|< string, string > map|
|**productUri**  <br>*optional*|Product uri of the application.  <br>**Example** : `"http://contoso.com/fridge/1.0"`|string|
|**siteId**  <br>*optional*|Site of the application|string|


<a name="applicationregistrationresponsemodel"></a>
### ApplicationRegistrationResponseModel
Result of an application registration


|Name|Description|Schema|
|---|---|---|
|**id**  <br>*required*|New id application was registered under|string|


<a name="applicationregistrationupdatemodel"></a>
### ApplicationRegistrationUpdateModel
Application registration update request


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Default name of the server or client.|string|
|**capabilities**  <br>*optional*|Capabilities of the application|< string > array|
|**context**  <br>*optional*||[OperationContextModel](definitions.md#operationcontextmodel)|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the application|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**locale**  <br>*optional*|Locale of default name - defaults to "en"|string|
|**localizedNames**  <br>*optional*|Localized names keyed off locale id.<br>To remove entry, set value for locale id to null.|< string, string > map|
|**productUri**  <br>*optional*|Product uri|string|


<a name="applicationsitelistmodel"></a>
### ApplicationSiteListModel
List of application sites


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**sites**  <br>*optional*|Sites|< string > array|


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


<a name="browsepathresponsemodel"></a>
### BrowsePathResponseModel
Result of node browse continuation


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**targets**  <br>*optional*|Targets|< [NodePathTargetModel](definitions.md#nodepathtargetmodel) > array|


<a name="browseviewmodel"></a>
### BrowseViewModel
View to browse


|Name|Description|Schema|
|---|---|---|
|**timestamp**  <br>*optional*|Browses at or before this timestamp.|string (date-time)|
|**version**  <br>*optional*|Browses specific version of the view.|integer (int64)|
|**viewId**  <br>*required*|Node of the view to browse  <br>**Minimum length** : `1`|string|


<a name="conditionhandlingoptionsmodel"></a>
### ConditionHandlingOptionsModel
Condition handling options model


|Name|Description|Schema|
|---|---|---|
|**snapshotInterval**  <br>*optional*|Time interval for sending pending interval snapshot in seconds.|integer (int32)|
|**updateInterval**  <br>*optional*|Time interval for sending pending interval updates in seconds.|integer (int32)|


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
Data set routing

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


<a name="discovererlistmodel"></a>
### DiscovererListModel
Discoverer registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [DiscovererModel](definitions.md#discoverermodel) > array|


<a name="discoverermodel"></a>
### DiscovererModel
Discoverer registration


|Name|Description|Schema|
|---|---|---|
|**apiKey**  <br>*optional*|Current api key|string|
|**connected**  <br>*optional*|Whether discoverer is connected on this registration|boolean|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigModel](definitions.md#discoveryconfigmodel)|
|**id**  <br>*required*|Discoverer id  <br>**Minimum length** : `1`|string|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**requestedConfig**  <br>*optional*||[DiscoveryConfigModel](definitions.md#discoveryconfigmodel)|
|**requestedMode**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**siteId**  <br>*optional*|Site of the discoverer|string|
|**version**  <br>*optional*|The reported version of the discovery module|string|


<a name="discovererquerymodel"></a>
### DiscovererQueryModel
Discoverer registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**siteId**  <br>*optional*|Site of the discoverer|string|


<a name="discovererupdatemodel"></a>
### DiscovererUpdateModel
Discoverer update request


|Name|Description|Schema|
|---|---|---|
|**siteId**  <br>*optional*|Site the discoverer is part of|string|


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


<a name="endpointconnectivitystate"></a>
### EndpointConnectivityState
State of the endpoint after activation

*Type* : enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error, Disconnected, Unauthorized)


<a name="endpointinfolistmodel"></a>
### EndpointInfoListModel
Endpoint info list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*required*|Endpoint infos|< [EndpointInfoModel](definitions.md#endpointinfomodel) > array|


<a name="endpointinfomodel"></a>
### EndpointInfoModel
Endpoint info


|Name|Description|Schema|
|---|---|---|
|**applicationId**  <br>*required*|Application id endpoint is registered under.  <br>**Minimum length** : `1`|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityState](definitions.md#endpointconnectivitystate)|
|**notSeenSince**  <br>*optional*|Last time endpoint was seen|string (date-time)|
|**registration**  <br>*required*||[EndpointRegistrationModel](definitions.md#endpointregistrationmodel)|


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


<a name="endpointregistrationquerymodel"></a>
### EndpointRegistrationQueryModel
Endpoint query


|Name|Description|Schema|
|---|---|---|
|**applicationId**  <br>*optional*|Application id to filter|string|
|**certificate**  <br>*optional*|Certificate thumbprint of the endpoint|string|
|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityState](definitions.md#endpointconnectivitystate)|
|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Endpoint security policy to use - null = Best.|string|
|**siteOrGatewayId**  <br>*optional*|Site or gateway id to filter with|string|
|**url**  <br>*optional*|Endoint url for direct server access|string|


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


<a name="gatewayinfomodel"></a>
### GatewayInfoModel
Gateway info model


|Name|Schema|
|---|---|
|**gateway**  <br>*required*|[GatewayModel](definitions.md#gatewaymodel)|
|**modules**  <br>*optional*|[GatewayModulesModel](definitions.md#gatewaymodulesmodel)|


<a name="gatewaylistmodel"></a>
### GatewayListModel
Gateway registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [GatewayModel](definitions.md#gatewaymodel) > array|


<a name="gatewaymodel"></a>
### GatewayModel
Gateway registration model


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Whether gateway is connected|boolean|
|**id**  <br>*required*|Gateway id  <br>**Minimum length** : `1`|string|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="gatewaymodulesmodel"></a>
### GatewayModulesModel
Gateway modules


|Name|Schema|
|---|---|
|**discoverer**  <br>*optional*|[DiscovererModel](definitions.md#discoverermodel)|
|**publisher**  <br>*optional*|[PublisherModel](definitions.md#publishermodel)|
|**supervisor**  <br>*optional*|[SupervisorModel](definitions.md#supervisormodel)|


<a name="gatewayquerymodel"></a>
### GatewayQueryModel
Gateway registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="gatewayupdatemodel"></a>
### GatewayUpdateModel
Gateway registration update request


|Name|Description|Schema|
|---|---|---|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="heartbeatbehavior"></a>
### HeartbeatBehavior
Heartbeat behavior

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
Message encoding

*Type* : enum (Uadp, Json, Xml, Avro, IsReversible, JsonReversible, IsGzipCompressed, JsonGzip, AvroGzip, JsonReversibleGzip)


<a name="messagingmode"></a>
### MessagingMode
Message modes

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
Monitored item watchdog condition

*Type* : enum (WhenAllAreLate, WhenAnyIsLate)


<a name="namespaceformat"></a>
### NamespaceFormat
Namespace serialization format for node ids
and qualified names.

*Type* : enum (Uri, Index, Expanded)


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
Node id serialized as object


|Name|Description|Schema|
|---|---|---|
|**Identifier**  <br>*optional*|Identifier|string|


<a name="nodemetadatarequestmodel"></a>
### NodeMetadataRequestModel
Node metadata request model


|Name|Description|Schema|
|---|---|---|
|**browsePath**  <br>*optional*|An optional component path from the node identified by<br>NodeId to the actual node.|< string > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodeId**  <br>*optional*|Node id of the type.<br>(Required)|string|


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
Enum that defines the authentication method

*Type* : enum (Anonymous, UsernamePassword, Certificate)


<a name="opcnodemodel"></a>
### OpcNodeModel
Describing an entry in the node list


|Name|Description|Schema|
|---|---|---|
|**AttributeId**  <br>*optional*||[NodeAttribute](definitions.md#nodeattribute)|
|**BrowsePath**  <br>*optional*|Browse path from the node to reach the actual node<br>to monitor.|< string > array|
|**ConditionHandling**  <br>*optional*||[ConditionHandlingOptionsModel](definitions.md#conditionhandlingoptionsmodel)|
|**CyclicReadMaxAgeTimespan**  <br>*optional*|The max cache age to use for cyclic reads.<br>Default is 0 (uncached reads).|string (date-span)|
|**DataChangeTrigger**  <br>*optional*||[DataChangeTriggerType](definitions.md#datachangetriggertype)|
|**DataSetClassFieldId**  <br>*optional*|The identifier of the field in the dataset class.<br>Allows correlation to the data set class.|string (uuid)|
|**DataSetFieldId**  <br>*optional*|The identifier of the field in the dataset message.<br>If not provided Azure.IIoT.OpcUa.Publisher.Models.OpcNodeModel.DisplayName is used.|string|
|**DeadbandType**  <br>*optional*||[DeadbandType](definitions.md#deadbandtype)|
|**DeadbandValue**  <br>*optional*|Deadband value of the data change filter to apply.<br>Does not apply to events|number (double)|
|**DiscardNew**  <br>*optional*|Discard new values in the server queue instead of<br>old values when no more room in queue.|boolean|
|**DisplayName**  <br>*optional*|Display name|string|
|**EventFilter**  <br>*optional*||[EventFilterModel](definitions.md#eventfiltermodel)|
|**ExpandedNodeId**  <br>*optional*|Expanded Node identifier (same as Azure.IIoT.OpcUa.Publisher.Models.OpcNodeModel.Id)|string|
|**FetchDisplayName**  <br>*optional*|Fetch display name from the node|boolean|
|**HeartbeatBehavior**  <br>*optional*||[HeartbeatBehavior](definitions.md#heartbeatbehavior)|
|**HeartbeatInterval**  <br>*optional*|Heartbeat interval in seconds|integer (int32)|
|**HeartbeatIntervalTimespan**  <br>*optional*|Heartbeat interval as TimeSpan.|string (date-span)|
|**Id**  <br>*optional*|Node Identifier|string|
|**IndexRange**  <br>*optional*|Index range to read, default to null.|string|
|**ModelChangeHandling**  <br>*optional*||[ModelChangeHandlingOptionsModel](definitions.md#modelchangehandlingoptionsmodel)|
|**OpcPublishingInterval**  <br>*optional*|Publishing interval in milliseconds|integer (int32)|
|**OpcPublishingIntervalTimespan**  <br>*optional*|OpcPublishingInterval as TimeSpan.|string (date-span)|
|**OpcSamplingInterval**  <br>*optional*|Sampling interval in milliseconds|integer (int32)|
|**OpcSamplingIntervalTimespan**  <br>*optional*|OpcSamplingInterval as TimeSpan.|string (date-span)|
|**QualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**QueueSize**  <br>*optional*|Queue Size for the monitored item on the server.<br>Specifies how many values are queued on the server<br>before undelivered ones are discarded.|integer (int64)|
|**RegisterNode**  <br>*optional*|Register node for reading before sampling.|boolean|
|**SkipFirst**  <br>*optional*|Do not send the first value that is always provided<br>by the server when the monitored item is created.|boolean|
|**Topic**  <br>*optional*|Topic to publish to - splits network messages<br>along the lines of topic name and overrides<br>the queue name of the writer and writer group.|string|
|**TriggeredNodes**  <br>*optional*|Nodes that are triggered by the parent node.<br>Nodes cannot themselves trigger other nodes, any<br>such setting is silently discarded. Triggered nodes<br>can only be updated as an atomic unit using API.|< [OpcNodeModel](definitions.md#opcnodemodel) > array|
|**UseCyclicRead**  <br>*optional*|Use cyclic read to sample.|boolean|


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
|**Detail**  <br>*optional*|string|
|**Extensions**  <br>*optional*|object|
|**Instance**  <br>*optional*|string|
|**Status**  <br>*optional*|integer (int32)|
|**Title**  <br>*optional*|string|
|**Type**  <br>*optional*|string|


<a name="publishbulkrequestmodel"></a>
### PublishBulkRequestModel
Publish in bulk request


|Name|Description|Schema|
|---|---|---|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|
|**nodesToAdd**  <br>*optional*|Node to add|< [PublishedItemModel](definitions.md#publisheditemmodel) > array|
|**nodesToRemove**  <br>*optional*|Node to remove|< string > array|


<a name="publishbulkresponsemodel"></a>
### PublishBulkResponseModel
Result of bulk request


|Name|Description|Schema|
|---|---|---|
|**nodesToAdd**  <br>*optional*|Node to add|< [ServiceResultModel](definitions.md#serviceresultmodel) > array|
|**nodesToRemove**  <br>*optional*|Node to remove|< [ServiceResultModel](definitions.md#serviceresultmodel) > array|


<a name="publishstartrequestmodel"></a>
### PublishStartRequestModel
Publish request


|Name|Schema|
|---|---|
|**header**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|
|**item**  <br>*required*|[PublishedItemModel](definitions.md#publisheditemmodel)|


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


<a name="publishednodesentrymodel"></a>
### PublishedNodesEntryModel
Contains the nodes which should be published


|Name|Description|Schema|
|---|---|---|
|**BatchSize**  <br>*optional*|Send network messages when the notification queue<br>exceeds this number. Causes this many notifications<br>to be added to network messages|integer (int64)|
|**BatchTriggerInterval**  <br>*optional*|Send network messages at the specified publishing<br>interval.|integer (int32)|
|**BatchTriggerIntervalTimespan**  <br>*optional*|Send network messages at the specified publishing<br>interval.<br>Takes precedence over Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.BatchTriggerInterval<br>if defined.|string (date-span)|
|**DataSetClassId**  <br>*optional*|A dataset class id.|string (uuid)|
|**DataSetDescription**  <br>*optional*|The optional description of the dataset.|string|
|**DataSetExtensionFields**  <br>*optional*|Optional field and value pairs to insert into the<br>data sets emitted by data set writer.|< string, object > map|
|**DataSetFetchDisplayNames**  <br>*optional*|Whether to fetch the display name and use it as<br>data set id for all opc node items in the data set|boolean|
|**DataSetKeyFrameCount**  <br>*optional*|Insert a key frame every x messages|integer (int64)|
|**DataSetName**  <br>*optional*|The optional short name of the dataset.|string|
|**DataSetPublishingInterval**  <br>*optional*|The Publishing interval for a dataset writer<br>in miliseconds.|integer (int32)|
|**DataSetPublishingIntervalTimespan**  <br>*optional*|The Publishing interval for a dataset writer<br>in timespan format. Takes precedence over<br>Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.DataSetPublishingInterval if defined.|string (date-span)|
|**DataSetRouting**  <br>*optional*||[DataSetRoutingMode](definitions.md#datasetroutingmode)|
|**DataSetSamplingInterval**  <br>*optional*|The default sampling interval for all items in a dataset writer<br>in miliseconds if the nodes do not specify a sampling rate.|integer (int32)|
|**DataSetSamplingIntervalTimespan**  <br>*optional*|The Sampling interval for the nodes in the dataset writer<br>in timespan format. Takes precedence over<br>Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.DataSetSamplingInterval if defined.|string (date-span)|
|**DataSetWriterGroup**  <br>*optional*|The Group the writer belongs to.|string|
|**DataSetWriterId**  <br>*optional*|Name of the data set writer.|string|
|**DataSetWriterWatchdogBehavior**  <br>*optional*||[SubscriptionWatchdogBehavior](definitions.md#subscriptionwatchdogbehavior)|
|**DefaultHeartbeatBehavior**  <br>*optional*||[HeartbeatBehavior](definitions.md#heartbeatbehavior)|
|**DefaultHeartbeatInterval**  <br>*optional*|Default heartbeat interval in milliseconds|integer (int32)|
|**DefaultHeartbeatIntervalTimespan**  <br>*optional*|Default heartbeat interval for all nodes as duration. Takes<br>precedence over Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.DefaultHeartbeatInterval if<br>defined.|string (date-span)|
|**DisableSubscriptionTransfer**  <br>*optional*|Disable subscription transfer on reconnect|boolean|
|**DumpConnectionDiagnostics**  <br>*optional*|Dump server diagnostics for the connection to enable<br>advanced troubleshooting scenarios.|boolean|
|**EncryptedAuthPassword**  <br>*optional*|encrypted password|string|
|**EncryptedAuthUsername**  <br>*optional*|encrypted username|string|
|**EndpointSecurityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**EndpointSecurityPolicy**  <br>*optional*|The specific security policy to use for the specified<br>endpoint. Overrides Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.UseSecurity setting.<br>If the security policy is not available with the<br>specified security mode connectivity will fail.|string|
|**EndpointUrl**  <br>*required*|The endpoint URL of the OPC UA server.  <br>**Minimum length** : `1`|string|
|**LastChangeDateTime**  <br>*optional*|Last change to the entry|string (date-time)|
|**MaxKeepAliveCount**  <br>*optional*|When the publishing timer has expired this number of<br>times without requiring any Notification to be sent,<br>to the writer a keep-alive message is sent.|integer (int64)|
|**MessageEncoding**  <br>*optional*||[MessageEncoding](definitions.md#messageencoding)|
|**MessageRetention**  <br>*optional*|Message retention setting for messages sent by<br>the writer if the transport supports it.|boolean|
|**MessageTtlTimespan**  <br>*optional*|Message time to live for messages sent by the<br>writer if the transport supports it.|string (date-span)|
|**MessagingMode**  <br>*optional*||[MessagingMode](definitions.md#messagingmode)|
|**MetaDataQueueName**  <br>*optional*|Meta data queue name to use for the writer. Overrides<br>the default metadata topic template.|string|
|**MetaDataUpdateTime**  <br>*optional*|Send metadata at the configured interval<br>even when not changing expressed in milliseconds.|integer (int32)|
|**MetaDataUpdateTimeTimespan**  <br>*optional*|Send metadata at the configured interval even when not<br>changing expressed as duration. Takes precedence over<br>Azure.IIoT.OpcUa.Publisher.Models.PublishedNodesEntryModel.MetaDataUpdateTimeif defined.|string (date-span)|
|**NodeId**  <br>*optional*||[NodeIdModel](definitions.md#nodeidmodel)|
|**OpcAuthenticationMode**  <br>*optional*||[OpcAuthenticationMode](definitions.md#opcauthenticationmode)|
|**OpcAuthenticationPassword**  <br>*optional*|plain password|string|
|**OpcAuthenticationUsername**  <br>*optional*|plain username|string|
|**OpcNodeWatchdogCondition**  <br>*optional*||[MonitoredItemWatchdogCondition](definitions.md#monitoreditemwatchdogcondition)|
|**OpcNodeWatchdogTimespan**  <br>*optional*|The timeout to use to monitor the monitored items in the<br>subscription are continously reporting fresh data.|string (date-span)|
|**OpcNodes**  <br>*optional*|Nodes defined in the collection.|< [OpcNodeModel](definitions.md#opcnodemodel) > array|
|**Priority**  <br>*optional*|Priority of the writer subscription.|integer (int32)|
|**QualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**QueueName**  <br>*optional*|Writer queue overrides the writer group queue name.<br>Network messages are then split across queues with<br>Qos also accounted for.|string|
|**RepublishAfterTransfer**  <br>*optional*|Republish after transferring the subscription during<br>reconnect handling unless subscription transfer was disabled.|boolean|
|**SendKeepAliveDataSetMessages**  <br>*optional*|Send a keep alive message when a subscription keep<br>alive notification is received inside the writer. If keep<br>alive messages are not supported by the messaging<br>profile chosen this value is ignored.|boolean|
|**UseReverseConnect**  <br>*optional*|Use reverse connect to connect ot the endpoint|boolean|
|**UseSecurity**  <br>*optional*|Secure transport should be used to connect to<br>the opc server.|boolean|
|**Version**  <br>*optional*|Version number of the entry|integer (int64)|
|**WriterGroupMessageRetention**  <br>*optional*|Default message retention setting for messages sent<br>through the writer group if the transport supports it.|boolean|
|**WriterGroupMessageTtlTimepan**  <br>*optional*|Default time to live for messages sent through<br>the writer group if the transport supports it.|string (date-span)|
|**WriterGroupPartitions**  <br>*optional*|Number of partitions to split the writer group into<br>when publishing to target topics.|integer (int32)|
|**WriterGroupQualityOfService**  <br>*optional*||[QoS](definitions.md#qos)|
|**WriterGroupQueueName**  <br>*optional*|Writer group queue overrides the default writer group<br>topic template to use.|string|
|**WriterGroupTransport**  <br>*optional*||[WriterGroupTransport](definitions.md#writergrouptransport)|


<a name="publishednodesentrymodeliasyncenumerable"></a>
### PublishedNodesEntryModelIAsyncEnumerable
*Type* : object


<a name="publisherlistmodel"></a>
### PublisherListModel
Publisher list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [PublisherModel](definitions.md#publishermodel) > array|


<a name="publishermodel"></a>
### PublisherModel
Publisher registration


|Name|Description|Schema|
|---|---|---|
|**apiKey**  <br>*optional*|Current api key|string|
|**connected**  <br>*optional*|Whether publisher is connected|boolean|
|**id**  <br>*required*|Identifier of the publisher  <br>**Minimum length** : `1`|string|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**siteId**  <br>*optional*|Site of the publisher|string|
|**version**  <br>*optional*|The reported version of the publisher|string|


<a name="publisherquerymodel"></a>
### PublisherQueryModel
Publisher registration query request


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**siteId**  <br>*optional*|Site for the supervisors|string|


<a name="publisherupdatemodel"></a>
### PublisherUpdateModel
Publisher registration update request


|Name|Description|Schema|
|---|---|---|
|**apiKey**  <br>*optional*|New api key|string|
|**siteId**  <br>*optional*|Site of the publisher|string|


<a name="qos"></a>
### QoS
*Type* : enum (AtMostOnce, AtLeastOnce, ExactlyOnce)


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


<a name="readrequestmodel"></a>
### ReadRequestModel
Request node attribute read


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to read|< [AttributeReadRequestModel](definitions.md#attributereadrequestmodel) > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


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
Security mode of endpoint

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
Subscription watchdog behavior

*Type* : enum (Diagnostic, Reset, FailFast, ExitProcess)


<a name="supervisorlistmodel"></a>
### SupervisorListModel
Supervisor list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [SupervisorModel](definitions.md#supervisormodel) > array|


<a name="supervisormodel"></a>
### SupervisorModel
Supervisor registration


|Name|Description|Schema|
|---|---|---|
|**apiKey**  <br>*optional*|Api key of the module|string|
|**connected**  <br>*optional*|Whether supervisor is connected|boolean|
|**id**  <br>*required*|Identifier of the supervisor  <br>**Minimum length** : `1`|string|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**siteId**  <br>*optional*|Site of the supervisor|string|
|**version**  <br>*optional*|The reported version of the supervisor|string|


<a name="supervisorquerymodel"></a>
### SupervisorQueryModel
Supervisor registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**endpointId**  <br>*optional*|Managing provided endpoint twin|string|
|**siteId**  <br>*optional*|Site for the supervisors|string|


<a name="supervisorupdatemodel"></a>
### SupervisorUpdateModel
Supervisor update request


|Name|Description|Schema|
|---|---|---|
|**siteId**  <br>*optional*|Site the supervisor is part of|string|


<a name="testconnectionrequestmodel"></a>
### TestConnectionRequestModel
Test connection request


|Name|Schema|
|---|---|
|**header**  <br>*optional*|[RequestHeaderModel](definitions.md#requestheadermodel)|


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


<a name="useridentitymodel"></a>
### UserIdentityModel
User identity model


|Name|Description|Schema|
|---|---|---|
|**password**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.UserName authentication<br>            this is the password of the user.<br>            <br><br><br>            For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the passcode to export the configured certificate's<br>            private key.<br>            <br><br><br>            Not used for the other authentication types.|string|
|**thumbprint**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the thumbprint of the configured certificate to use.<br>            Either Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.User or Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.Thumbprint must be<br>            used to select the certificate in the user certificate store.<br>            <br><br><br>            Not used for the other authentication types.|string|
|**user**  <br>*optional*|For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.UserName authentication<br>            this is the name of the user.<br>            <br><br><br>            For Azure.IIoT.OpcUa.Publisher.Models.CredentialType.X509Certificate authentication<br>            this is the subject name of the certificate that has been<br>            configured.<br>            Either Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.User or Azure.IIoT.OpcUa.Publisher.Models.UserIdentityModel.Thumbprint must be<br>            used to select the certificate in the user certificate store.<br>            <br><br><br>            Not used for the other authentication types.|string|


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


<a name="writerequestmodel"></a>
### WriteRequestModel
Request node attribute write


|Name|Description|Schema|
|---|---|---|
|**attributes**  <br>*required*|Attributes to update|< [AttributeWriteRequestModel](definitions.md#attributewriterequestmodel) > array|
|**header**  <br>*optional*||[RequestHeaderModel](definitions.md#requestheadermodel)|


<a name="writeresponsemodel"></a>
### WriteResponseModel
Result of attribute write


|Name|Description|Schema|
|---|---|---|
|**errorInfo**  <br>*optional*||[ServiceResultModel](definitions.md#serviceresultmodel)|
|**results**  <br>*required*|All results of attribute writes|< [AttributeWriteResponseModel](definitions.md#attributewriteresponsemodel) > array|


<a name="writergrouptransport"></a>
### WriterGroupTransport
Desired writer group transport

*Type* : enum (IoTHub, Mqtt, EventHub, Dapr, Http, FileSystem, Null)


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



