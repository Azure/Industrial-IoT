# Opc-Twin


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Twin Service


### Version information
*Version* : v1


### URI scheme
*Schemes* : HTTPS, HTTP


### Tags

* Browse : Browse nodes services
* Call : Call node method services
* Publish : Value and Event publishing services
* Read : Node read services
* Status : Status checks
* Write : Node writing services




<a name="paths"></a>
## Resources

<a name="browse_resource"></a>
### Browse
Browse nodes services


<a name="browse"></a>
#### Browse node references
```
POST /v1/browse/{endpointId}
```


##### Description
Browse a node on the specified endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The browse request|[BrowseRequestApiModel](definitions.md#browserequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseResponseApiModel](definitions.md#browseresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/browse/string
```


###### Request body
```json
{
  "nodeId" : "string",
  "direction" : "string",
  "view" : {
    "viewId" : "string",
    "version" : 0,
    "timestamp" : "string"
  },
  "referenceTypeId" : "string",
  "noSubtypes" : true,
  "maxReferencesToReturn" : 0,
  "targetNodesOnly" : true,
  "readVariableValues" : true,
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "node" : {
    "nodeClass" : "string",
    "displayName" : "string",
    "id" : "string",
    "description" : "string",
    "children" : true,
    "name" : "string",
    "accessRestrictions" : "string",
    "writeMask" : 0,
    "userWriteMask" : 0,
    "isAbstract" : true,
    "containsNoLoops" : true,
    "eventNotifier" : "string",
    "executable" : true,
    "userExecutable" : true,
    "dataTypeDefinition" : "object",
    "accessLevel" : "string",
    "userAccessLevel" : "string",
    "dataType" : "string",
    "valueRank" : "string",
    "arrayDimensions" : [ 0 ],
    "historizing" : true,
    "minimumSamplingInterval" : 0.0,
    "value" : "object",
    "inverseName" : "string",
    "symmetric" : true,
    "rolePermissions" : [ {
      "roleId" : "string",
      "permissions" : "string"
    } ],
    "userRolePermissions" : [ {
      "roleId" : "string",
      "permissions" : "string"
    } ]
  },
  "references" : [ {
    "typeId" : "string",
    "browseName" : "string",
    "direction" : "string",
    "displayName" : "string",
    "target" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "typeDefinition" : "string"
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="getsetofuniquenodes"></a>
#### Browse set of unique target nodes
```
GET /v1/browse/{endpointId}
```


##### Description
Browse the set of unique hierarchically referenced target nodes on the endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
The root node id to browse from can be provided as part of the query
parameters.
If it is not provided, the RootFolder node is browsed. Note that this
is the same as the POST method with the model containing the node id
and the targetNodesOnly flag set to true.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**nodeId**  <br>*optional*|The node to browse or omit to browse object root|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseResponseApiModel](definitions.md#browseresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/browse/string
```


##### Example HTTP response

###### Response 200
```json
{
  "node" : {
    "nodeClass" : "string",
    "displayName" : "string",
    "id" : "string",
    "description" : "string",
    "children" : true,
    "name" : "string",
    "accessRestrictions" : "string",
    "writeMask" : 0,
    "userWriteMask" : 0,
    "isAbstract" : true,
    "containsNoLoops" : true,
    "eventNotifier" : "string",
    "executable" : true,
    "userExecutable" : true,
    "dataTypeDefinition" : "object",
    "accessLevel" : "string",
    "userAccessLevel" : "string",
    "dataType" : "string",
    "valueRank" : "string",
    "arrayDimensions" : [ 0 ],
    "historizing" : true,
    "minimumSamplingInterval" : 0.0,
    "value" : "object",
    "inverseName" : "string",
    "symmetric" : true,
    "rolePermissions" : [ {
      "roleId" : "string",
      "permissions" : "string"
    } ],
    "userRolePermissions" : [ {
      "roleId" : "string",
      "permissions" : "string"
    } ]
  },
  "references" : [ {
    "typeId" : "string",
    "browseName" : "string",
    "direction" : "string",
    "displayName" : "string",
    "target" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "typeDefinition" : "string"
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="browsenext"></a>
#### Browse next set of references
```
POST /v1/browse/{endpointId}/next
```


##### Description
Browse next set of references on the endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The request body with continuation token.|[BrowseNextRequestApiModel](definitions.md#browsenextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseApiModel](definitions.md#browsenextresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/browse/string/next
```


###### Request body
```json
{
  "continuationToken" : "string",
  "abort" : true,
  "targetNodesOnly" : true,
  "readVariableValues" : true,
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "references" : [ {
    "typeId" : "string",
    "browseName" : "string",
    "direction" : "string",
    "displayName" : "string",
    "target" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "typeDefinition" : "string"
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="getnextsetofuniquenodes"></a>
#### Browse next set of unique target nodes
```
GET /v1/browse/{endpointId}/next
```


##### Description
Browse the next set of unique hierarchically referenced target nodes on the
endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
Note that this is the same as the POST method with the model containing
the continuation token and the targetNodesOnly flag set to true.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|Continuation token from GetSetOfUniqueNodes operation|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowseNextResponseApiModel](definitions.md#browsenextresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/browse/string/next?continuationToken=string
```


##### Example HTTP response

###### Response 200
```json
{
  "references" : [ {
    "typeId" : "string",
    "browseName" : "string",
    "direction" : "string",
    "displayName" : "string",
    "target" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "typeDefinition" : "string"
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="browseusingpath"></a>
#### Browse using a browse path
```
POST /v1/browse/{endpointId}/path
```


##### Description
Browse using a path from the specified node id.
This call uses TranslateBrowsePathsToNodeIds service under the hood.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The browse path request|[BrowsePathRequestApiModel](definitions.md#browsepathrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BrowsePathResponseApiModel](definitions.md#browsepathresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/browse/string/path
```


###### Request body
```json
{
  "nodeId" : "string",
  "pathElements" : [ "string" ],
  "readVariableValues" : true,
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "targets" : [ {
    "target" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "remainingPathIndex" : 0
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="call_resource"></a>
### Call
Call node method services


<a name="callmethod"></a>
#### Call a method
```
POST /v1/call/{endpointId}
```


##### Description
Invoke method node with specified input arguments.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The method call request|[MethodCallRequestApiModel](definitions.md#methodcallrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodCallResponseApiModel](definitions.md#methodcallresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/call/string
```


###### Request body
```json
{
  "methodId" : "string",
  "objectId" : "string",
  "arguments" : [ {
    "value" : "object",
    "dataType" : "string"
  } ],
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "value" : "object",
    "dataType" : "string"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="getcallmetadata"></a>
#### Get method meta data
```
POST /v1/call/{endpointId}/metadata
```


##### Description
Return method meta data to support a user interface displaying forms to
input and output arguments.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The method metadata request|[MethodMetadataRequestApiModel](definitions.md#methodmetadatarequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MethodMetadataResponseApiModel](definitions.md#methodmetadataresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/call/string/metadata
```


###### Request body
```json
{
  "methodId" : "string",
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "objectId" : "string",
  "inputArguments" : [ {
    "name" : "string",
    "description" : "string",
    "type" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "defaultValue" : "object",
    "valueRank" : "string",
    "arrayDimensions" : [ 0 ]
  } ],
  "outputArguments" : [ {
    "name" : "string",
    "description" : "string",
    "type" : {
      "nodeClass" : "string",
      "displayName" : "string",
      "id" : "string",
      "description" : "string",
      "children" : true,
      "name" : "string",
      "accessRestrictions" : "string",
      "writeMask" : 0,
      "userWriteMask" : 0,
      "isAbstract" : true,
      "containsNoLoops" : true,
      "eventNotifier" : "string",
      "executable" : true,
      "userExecutable" : true,
      "dataTypeDefinition" : "object",
      "accessLevel" : "string",
      "userAccessLevel" : "string",
      "dataType" : "string",
      "valueRank" : "string",
      "arrayDimensions" : [ 0 ],
      "historizing" : true,
      "minimumSamplingInterval" : 0.0,
      "value" : "object",
      "inverseName" : "string",
      "symmetric" : true,
      "rolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ],
      "userRolePermissions" : [ {
        "roleId" : "string",
        "permissions" : "string"
      } ]
    },
    "defaultValue" : "object",
    "valueRank" : "string",
    "arrayDimensions" : [ 0 ]
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="publish_resource"></a>
### Publish
Value and Event publishing services


<a name="getfirstlistofpublishednodes"></a>
#### Get currently published nodes
```
POST /v1/publish/{endpointId}
```


##### Description
Returns currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The list request|[PublishedItemListRequestApiModel](definitions.md#publisheditemlistrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/publish/string
```


###### Request body
```json
{
  "continuationToken" : "string"
}
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "nodeId" : "string",
    "nodeAttribute" : "string",
    "publishingInterval" : 0,
    "samplingInterval" : 0
  } ],
  "continuationToken" : "string"
}
```


<a name="getnextlistofpublishednodes"></a>
#### Get next set of published nodes
```
GET /v1/publish/{endpointId}
```


##### Description
Returns next set of currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|The continuation token to continue with|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/publish/string?continuationToken=string
```


##### Example HTTP response

###### Response 200
```json
{
  "items" : [ {
    "nodeId" : "string",
    "nodeAttribute" : "string",
    "publishingInterval" : 0,
    "samplingInterval" : 0
  } ],
  "continuationToken" : "string"
}
```


<a name="startpublishingvalues"></a>
#### Start publishing node values
```
POST /v1/publish/{endpointId}/start
```


##### Description
Start publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The publish request|[PublishStartRequestApiModel](definitions.md#publishstartrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseApiModel](definitions.md#publishstartresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/publish/string/start
```


###### Request body
```json
{
  "item" : {
    "nodeId" : "string",
    "nodeAttribute" : "string",
    "publishingInterval" : 0,
    "samplingInterval" : 0
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="stoppublishingvalues"></a>
#### Stop publishing node values
```
POST /v1/publish/{endpointId}/stop
```


##### Description
Stop publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The unpublish request|[PublishStopRequestApiModel](definitions.md#publishstoprequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseApiModel](definitions.md#publishstopresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/publish/string/stop
```


###### Request body
```json
{
  "nodeId" : "string",
  "nodeAttribute" : "string",
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="read_resource"></a>
### Read
Node read services


<a name="readvalue"></a>
#### Read variable value
```
POST /v1/read/{endpointId}
```


##### Description
Read a variable node's value.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The read value request|[ValueReadRequestApiModel](definitions.md#valuereadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseApiModel](definitions.md#valuereadresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/read/string
```


###### Request body
```json
{
  "nodeId" : "string",
  "indexRange" : "string",
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "value" : "object",
  "dataType" : "string",
  "sourcePicoseconds" : 0,
  "sourceTimestamp" : "string",
  "serverPicoseconds" : 0,
  "serverTimestamp" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="getvalue"></a>
#### Get variable value
```
GET /v1/read/{endpointId}
```


##### Description
Get a variable node's value using its node id.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**nodeId**  <br>*required*|The node to read|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueReadResponseApiModel](definitions.md#valuereadresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


##### Produces

* `application/json`


##### Security

|Type|Name|Scopes|
|---|---|---|
|**oauth2**|**[oauth2](security.md#oauth2)**|http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication|


##### Example HTTP request

###### Request path
```
/v1/read/string?nodeId=string
```


##### Example HTTP response

###### Response 200
```json
{
  "value" : "object",
  "dataType" : "string",
  "sourcePicoseconds" : 0,
  "sourceTimestamp" : "string",
  "serverPicoseconds" : 0,
  "serverTimestamp" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="readattributes"></a>
#### Read node attributes
```
POST /v1/read/{endpointId}/attributes
```


##### Description
Read attributes of a node.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The read request|[ReadRequestApiModel](definitions.md#readrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ReadResponseApiModel](definitions.md#readresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/read/string/attributes
```


###### Request body
```json
{
  "attributes" : [ {
    "nodeId" : "string",
    "attribute" : "string"
  } ],
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "value" : "object",
    "errorInfo" : {
      "statusCode" : 0,
      "errorMessage" : "string",
      "diagnostics" : "object"
    }
  } ]
}
```


<a name="readhistory"></a>
#### Read node history
```
POST /v1/read/{endpointId}/history
```


##### Description
Read node history if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel](definitions.md#historyreadrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel](definitions.md#historyreadresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/read/string/history
```


###### Request body
```json
{
  "nodeId" : "string",
  "request" : "object",
  "indexRange" : "string",
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : "object",
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="readhistorynext"></a>
#### Read next batch of node history
```
POST /v1/read/{endpointId}/history/next
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
|**200**|Success|[HistoryReadNextResponseApiModel](definitions.md#historyreadnextresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/read/string/history/next
```


###### Request body
```json
{
  "continuationToken" : "string",
  "abort" : true,
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : "object",
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="status_resource"></a>
### Status
Status checks


<a name="getstatus"></a>
#### Return the service status in the form of the service status api model.
```
GET /v1/status
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StatusResponseApiModel](definitions.md#statusresponseapimodel)|


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/status
```


##### Example HTTP response

###### Response 200
```json
{
  "name" : "string",
  "status" : "string",
  "currentTime" : "string",
  "startTime" : "string",
  "upTime" : 0,
  "uid" : "string",
  "properties" : {
    "string" : "string"
  },
  "dependencies" : {
    "string" : "string"
  },
  "$metadata" : {
    "string" : "string"
  }
}
```


<a name="write_resource"></a>
### Write
Node writing services


<a name="writevalue"></a>
#### Write variable value
```
POST /v1/write/{endpointId}
```


##### Description
Write variable node's value.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The write value request|[ValueWriteRequestApiModel](definitions.md#valuewriterequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ValueWriteResponseApiModel](definitions.md#valuewriteresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/write/string
```


###### Request body
```json
{
  "nodeId" : "string",
  "value" : "object",
  "dataType" : "string",
  "indexRange" : "string",
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="writeattributes"></a>
#### Write node attributes
```
POST /v1/write/{endpointId}/attributes
```


##### Description
Write any attribute of a node.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The batch write request|[WriteRequestApiModel](definitions.md#writerequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WriteResponseApiModel](definitions.md#writeresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/write/string/attributes
```


###### Request body
```json
{
  "attributes" : [ {
    "nodeId" : "string",
    "attribute" : "string",
    "value" : "object"
  } ],
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "errorInfo" : {
      "statusCode" : 0,
      "errorMessage" : "string",
      "diagnostics" : "object"
    }
  } ]
}
```


<a name="writehistory"></a>
#### Update node history
```
POST /v1/write/{endpointId}/history
```


##### Description
Update node history using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryUpdateRequestApiModel](definitions.md#historyupdaterequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|
|**401**|Unauthorized|No Content|
|**403**|Forbidden|No Content|


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


##### Example HTTP request

###### Request path
```
/v1/write/string/history
```


###### Request body
```json
{
  "request" : "object",
  "elevation" : {
    "type" : "string",
    "value" : "object"
  },
  "diagnostics" : {
    "level" : "string",
    "auditId" : "string",
    "timeStamp" : "string"
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```



