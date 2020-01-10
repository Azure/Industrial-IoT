<?php
namespace Microsoft\Azure\IIoT\Api;
final class AzureOpcTwinClient
{
    /**
     * @param \Microsoft\Rest\RunTimeInterface $_runTime
     * @param string $subscriptionId
     */
    public function __construct(
        \Microsoft\Rest\RunTimeInterface $_runTime,
        $subscriptionId
    )
    {
        $_client = $_runTime->createClientFromData(
            self::_SWAGGER_OBJECT_DATA,
            ['subscriptionId' => $subscriptionId]
        );
        $this->_Browse_operation = $_client->createOperation('Browse');
        $this->_GetSetOfUniqueNodes_operation = $_client->createOperation('GetSetOfUniqueNodes');
        $this->_BrowseNext_operation = $_client->createOperation('BrowseNext');
        $this->_GetNextSetOfUniqueNodes_operation = $_client->createOperation('GetNextSetOfUniqueNodes');
        $this->_BrowseUsingPath_operation = $_client->createOperation('BrowseUsingPath');
        $this->_GetCallMetadata_operation = $_client->createOperation('GetCallMetadata');
        $this->_CallMethod_operation = $_client->createOperation('CallMethod');
        $this->_ReadValue_operation = $_client->createOperation('ReadValue');
        $this->_GetValue_operation = $_client->createOperation('GetValue');
        $this->_ReadAttributes_operation = $_client->createOperation('ReadAttributes');
        $this->_GetStatus_operation = $_client->createOperation('GetStatus');
        $this->_WriteValue_operation = $_client->createOperation('WriteValue');
        $this->_WriteAttributes_operation = $_client->createOperation('WriteAttributes');
    }
    /**
     * Browse a node on the specified endpoint. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function browse(
        $endpointId,
        array $body
    )
    {
        return $this->_Browse_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Browse the set of unique hierarchically referenced target nodes on the endpoint. The endpoint must be activated and connected and the module client and server must trust each other. The root node id to browse from can be provided as part of the query parameters. If it is not provided, the RootFolder node is browsed. Note that this is the same as the POST method with the model containing the node id and the targetNodesOnly flag set to true.
     * @param string $endpointId
     * @param string|null $nodeId
     * @return array
     */
    public function getSetOfUniqueNodes(
        $endpointId,
        $nodeId = null
    )
    {
        return $this->_GetSetOfUniqueNodes_operation->call([
            'endpointId' => $endpointId,
            'nodeId' => $nodeId
        ]);
    }
    /**
     * Browse next set of references on the endpoint. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function browseNext(
        $endpointId,
        array $body
    )
    {
        return $this->_BrowseNext_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Browse the next set of unique hierarchically referenced target nodes on the endpoint. The endpoint must be activated and connected and the module client and server must trust each other. Note that this is the same as the POST method with the model containing the continuation token and the targetNodesOnly flag set to true.
     * @param string $endpointId
     * @param string $continuationToken
     * @return array
     */
    public function getNextSetOfUniqueNodes(
        $endpointId,
        $continuationToken
    )
    {
        return $this->_GetNextSetOfUniqueNodes_operation->call([
            'endpointId' => $endpointId,
            'continuationToken' => $continuationToken
        ]);
    }
    /**
     * Browse using a path from the specified node id. This call uses TranslateBrowsePathsToNodeIds service under the hood. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function browseUsingPath(
        $endpointId,
        array $body
    )
    {
        return $this->_BrowseUsingPath_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Return method meta data to support a user interface displaying forms to input and output arguments. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function getCallMetadata(
        $endpointId,
        array $body
    )
    {
        return $this->_GetCallMetadata_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Invoke method node with specified input arguments. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function callMethod(
        $endpointId,
        array $body
    )
    {
        return $this->_CallMethod_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read a variable node's value. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function readValue(
        $endpointId,
        array $body
    )
    {
        return $this->_ReadValue_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Get a variable node's value using its node id. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param string $nodeId
     * @return array
     */
    public function getValue(
        $endpointId,
        $nodeId
    )
    {
        return $this->_GetValue_operation->call([
            'endpointId' => $endpointId,
            'nodeId' => $nodeId
        ]);
    }
    /**
     * Read attributes of a node. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function readAttributes(
        $endpointId,
        array $body
    )
    {
        return $this->_ReadAttributes_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * @return array
     */
    public function getStatus()
    {
        return $this->_GetStatus_operation->call([]);
    }
    /**
     * Write variable node's value. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function writeValue(
        $endpointId,
        array $body
    )
    {
        return $this->_WriteValue_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Write any attribute of a node. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function writeAttributes(
        $endpointId,
        array $body
    )
    {
        return $this->_WriteAttributes_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Browse_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetSetOfUniqueNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_BrowseNext_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetNextSetOfUniqueNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_BrowseUsingPath_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetCallMetadata_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_CallMethod_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ReadValue_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetValue_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ReadAttributes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetStatus_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_WriteValue_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_WriteAttributes_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/browse/{endpointId}' => [
                'post' => [
                    'operationId' => 'Browse',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/BrowseRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowseResponseApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetSetOfUniqueNodes',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'nodeId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowseResponseApiModel']]]
                ]
            ],
            '/v2/browse/{endpointId}/next' => [
                'post' => [
                    'operationId' => 'BrowseNext',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/BrowseNextRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowseNextResponseApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetNextSetOfUniqueNodes',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'continuationToken',
                            'in' => 'query',
                            'required' => TRUE,
                            'type' => 'string'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowseNextResponseApiModel']]]
                ]
            ],
            '/v2/browse/{endpointId}/path' => ['post' => [
                'operationId' => 'BrowseUsingPath',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/BrowsePathRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowsePathResponseApiModel']]]
            ]],
            '/v2/call/{endpointId}/metadata' => ['post' => [
                'operationId' => 'GetCallMetadata',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/MethodMetadataRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/MethodMetadataResponseApiModel']]]
            ]],
            '/v2/call/{endpointId}' => ['post' => [
                'operationId' => 'CallMethod',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/MethodCallRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/MethodCallResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}' => [
                'post' => [
                    'operationId' => 'ReadValue',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ValueReadRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ValueReadResponseApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetValue',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'nodeId',
                            'in' => 'query',
                            'required' => TRUE,
                            'type' => 'string'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ValueReadResponseApiModel']]]
                ]
            ],
            '/v2/read/{endpointId}/attributes' => ['post' => [
                'operationId' => 'ReadAttributes',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ReadResponseApiModel']]]
            ]],
            '/v2/status' => ['get' => [
                'operationId' => 'GetStatus',
                'parameters' => [],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StatusResponseApiModel']]]
            ]],
            '/v2/write/{endpointId}' => ['post' => [
                'operationId' => 'WriteValue',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ValueWriteRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ValueWriteResponseApiModel']]]
            ]],
            '/v2/write/{endpointId}/attributes' => ['post' => [
                'operationId' => 'WriteAttributes',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/WriteRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/WriteResponseApiModel']]]
            ]]
        ],
        'definitions' => [
            'BrowseViewApiModel' => [
                'properties' => [
                    'viewId' => ['type' => 'string'],
                    'version' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'timestamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['viewId']
            ],
            'CredentialApiModel' => [
                'properties' => [
                    'type' => [
                        'type' => 'string',
                        'enum' => [
                            'None',
                            'UserName',
                            'X509Certificate',
                            'JwtToken'
                        ]
                    ],
                    'value' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DiagnosticsApiModel' => [
                'properties' => [
                    'level' => [
                        'type' => 'string',
                        'enum' => [
                            'None',
                            'Status',
                            'Operations',
                            'Diagnostics',
                            'Verbose'
                        ]
                    ],
                    'auditId' => ['type' => 'string'],
                    'timeStamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'RequestHeaderApiModel' => [
                'properties' => [
                    'elevation' => ['$ref' => '#/definitions/CredentialApiModel'],
                    'locales' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'diagnostics' => ['$ref' => '#/definitions/DiagnosticsApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'BrowseRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'direction' => [
                        'type' => 'string',
                        'enum' => [
                            'Forward',
                            'Backward',
                            'Both'
                        ]
                    ],
                    'view' => ['$ref' => '#/definitions/BrowseViewApiModel'],
                    'referenceTypeId' => ['type' => 'string'],
                    'noSubtypes' => ['type' => 'boolean'],
                    'maxReferencesToReturn' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'targetNodesOnly' => ['type' => 'boolean'],
                    'readVariableValues' => ['type' => 'boolean'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'RolePermissionApiModel' => [
                'properties' => [
                    'roleId' => ['type' => 'string'],
                    'permissions' => [
                        'type' => 'string',
                        'enum' => [
                            'Browse',
                            'ReadRolePermissions',
                            'WriteAttribute',
                            'WriteRolePermissions',
                            'WriteHistorizing',
                            'Read',
                            'Write',
                            'ReadHistory',
                            'InsertHistory',
                            'ModifyHistory',
                            'DeleteHistory',
                            'ReceiveEvents',
                            'Call',
                            'AddReference',
                            'RemoveReference',
                            'DeleteNode',
                            'AddNode'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['roleId']
            ],
            'NodeApiModel' => [
                'properties' => [
                    'nodeClass' => [
                        'type' => 'string',
                        'enum' => [
                            'Object',
                            'Variable',
                            'Method',
                            'ObjectType',
                            'VariableType',
                            'ReferenceType',
                            'DataType',
                            'View'
                        ]
                    ],
                    'displayName' => ['type' => 'string'],
                    'nodeId' => ['type' => 'string'],
                    'description' => ['type' => 'string'],
                    'browseName' => ['type' => 'string'],
                    'accessRestrictions' => [
                        'type' => 'string',
                        'enum' => [
                            'SigningRequired',
                            'EncryptionRequired',
                            'SessionRequired'
                        ]
                    ],
                    'writeMask' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'userWriteMask' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'isAbstract' => ['type' => 'boolean'],
                    'containsNoLoops' => ['type' => 'boolean'],
                    'eventNotifier' => [
                        'type' => 'string',
                        'enum' => [
                            'SubscribeToEvents',
                            'HistoryRead',
                            'HistoryWrite'
                        ]
                    ],
                    'executable' => ['type' => 'boolean'],
                    'userExecutable' => ['type' => 'boolean'],
                    'dataTypeDefinition' => ['type' => 'object'],
                    'accessLevel' => [
                        'type' => 'string',
                        'enum' => [
                            'CurrentRead',
                            'CurrentWrite',
                            'HistoryRead',
                            'HistoryWrite',
                            'SemanticChange',
                            'StatusWrite',
                            'TimestampWrite',
                            'NonatomicRead',
                            'NonatomicWrite',
                            'WriteFullArrayOnly'
                        ]
                    ],
                    'userAccessLevel' => [
                        'type' => 'string',
                        'enum' => [
                            'CurrentRead',
                            'CurrentWrite',
                            'HistoryRead',
                            'HistoryWrite',
                            'SemanticChange',
                            'StatusWrite',
                            'TimestampWrite',
                            'NonatomicRead',
                            'NonatomicWrite',
                            'WriteFullArrayOnly'
                        ]
                    ],
                    'dataType' => ['type' => 'string'],
                    'valueRank' => [
                        'type' => 'string',
                        'enum' => [
                            'OneOrMoreDimensions',
                            'OneDimension',
                            'TwoDimensions',
                            'ScalarOrOneDimension',
                            'Any',
                            'Scalar'
                        ]
                    ],
                    'arrayDimensions' => [
                        'type' => 'array',
                        'items' => [
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'historizing' => ['type' => 'boolean'],
                    'minimumSamplingInterval' => [
                        'type' => 'number',
                        'format' => 'double'
                    ],
                    'value' => ['type' => 'object'],
                    'inverseName' => ['type' => 'string'],
                    'symmetric' => ['type' => 'boolean'],
                    'rolePermissions' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/RolePermissionApiModel']
                    ],
                    'userRolePermissions' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/RolePermissionApiModel']
                    ],
                    'typeDefinitionId' => ['type' => 'string'],
                    'children' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => ['nodeId']
            ],
            'NodeReferenceApiModel' => [
                'properties' => [
                    'referenceTypeId' => ['type' => 'string'],
                    'direction' => [
                        'type' => 'string',
                        'enum' => [
                            'Forward',
                            'Backward',
                            'Both'
                        ]
                    ],
                    'target' => ['$ref' => '#/definitions/NodeApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['target']
            ],
            'ServiceResultApiModel' => [
                'properties' => [
                    'statusCode' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'errorMessage' => ['type' => 'string'],
                    'diagnostics' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'BrowseResponseApiModel' => [
                'properties' => [
                    'node' => ['$ref' => '#/definitions/NodeApiModel'],
                    'references' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/NodeReferenceApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'BrowseNextRequestApiModel' => [
                'properties' => [
                    'continuationToken' => ['type' => 'string'],
                    'abort' => ['type' => 'boolean'],
                    'targetNodesOnly' => ['type' => 'boolean'],
                    'readVariableValues' => ['type' => 'boolean'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['continuationToken']
            ],
            'BrowseNextResponseApiModel' => [
                'properties' => [
                    'references' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/NodeReferenceApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'BrowsePathRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePaths' => [
                        'type' => 'array',
                        'items' => [
                            'type' => 'array',
                            'items' => ['type' => 'string']
                        ]
                    ],
                    'readVariableValues' => ['type' => 'boolean'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['browsePaths']
            ],
            'NodePathTargetApiModel' => [
                'properties' => [
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'target' => ['$ref' => '#/definitions/NodeApiModel'],
                    'remainingPathIndex' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'BrowsePathResponseApiModel' => [
                'properties' => [
                    'targets' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/NodePathTargetApiModel']
                    ],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodMetadataRequestApiModel' => [
                'properties' => [
                    'methodId' => ['type' => 'string'],
                    'methodBrowsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodMetadataArgumentApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'description' => ['type' => 'string'],
                    'type' => ['$ref' => '#/definitions/NodeApiModel'],
                    'defaultValue' => ['type' => 'object'],
                    'valueRank' => [
                        'type' => 'string',
                        'enum' => [
                            'OneOrMoreDimensions',
                            'OneDimension',
                            'TwoDimensions',
                            'ScalarOrOneDimension',
                            'Any',
                            'Scalar'
                        ]
                    ],
                    'arrayDimensions' => [
                        'type' => 'array',
                        'items' => [
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodMetadataResponseApiModel' => [
                'properties' => [
                    'objectId' => ['type' => 'string'],
                    'inputArguments' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/MethodMetadataArgumentApiModel']
                    ],
                    'outputArguments' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/MethodMetadataArgumentApiModel']
                    ],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodCallArgumentApiModel' => [
                'properties' => [
                    'value' => ['type' => 'object'],
                    'dataType' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodCallRequestApiModel' => [
                'properties' => [
                    'methodId' => ['type' => 'string'],
                    'objectId' => ['type' => 'string'],
                    'arguments' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/MethodCallArgumentApiModel']
                    ],
                    'methodBrowsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'objectBrowsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'MethodCallResponseApiModel' => [
                'properties' => [
                    'results' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/MethodCallArgumentApiModel']
                    ],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ValueReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ValueReadResponseApiModel' => [
                'properties' => [
                    'value' => ['type' => 'object'],
                    'dataType' => ['type' => 'string'],
                    'sourcePicoseconds' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'sourceTimestamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'serverPicoseconds' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'serverTimestamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'AttributeReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'attribute' => [
                        'type' => 'string',
                        'enum' => [
                            'NodeClass',
                            'BrowseName',
                            'DisplayName',
                            'Description',
                            'WriteMask',
                            'UserWriteMask',
                            'IsAbstract',
                            'Symmetric',
                            'InverseName',
                            'ContainsNoLoops',
                            'EventNotifier',
                            'Value',
                            'DataType',
                            'ValueRank',
                            'ArrayDimensions',
                            'AccessLevel',
                            'UserAccessLevel',
                            'MinimumSamplingInterval',
                            'Historizing',
                            'Executable',
                            'UserExecutable',
                            'DataTypeDefinition',
                            'RolePermissions',
                            'UserRolePermissions',
                            'AccessRestrictions'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'nodeId',
                    'attribute'
                ]
            ],
            'ReadRequestApiModel' => [
                'properties' => [
                    'attributes' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/AttributeReadRequestApiModel']
                    ],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['attributes']
            ],
            'AttributeReadResponseApiModel' => [
                'properties' => [
                    'value' => ['type' => 'object'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadResponseApiModel' => [
                'properties' => ['results' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/AttributeReadResponseApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'StatusResponseApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'status' => ['type' => 'string'],
                    'currentTime' => ['type' => 'string'],
                    'startTime' => ['type' => 'string'],
                    'upTime' => [
                        'type' => 'integer',
                        'format' => 'int64'
                    ],
                    'uid' => ['type' => 'string'],
                    'properties' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    'dependencies' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    '$metadata' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ValueWriteRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'value' => ['type' => 'object'],
                    'dataType' => ['type' => 'string'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['value']
            ],
            'ValueWriteResponseApiModel' => [
                'properties' => ['errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'AttributeWriteRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'attribute' => [
                        'type' => 'string',
                        'enum' => [
                            'NodeClass',
                            'BrowseName',
                            'DisplayName',
                            'Description',
                            'WriteMask',
                            'UserWriteMask',
                            'IsAbstract',
                            'Symmetric',
                            'InverseName',
                            'ContainsNoLoops',
                            'EventNotifier',
                            'Value',
                            'DataType',
                            'ValueRank',
                            'ArrayDimensions',
                            'AccessLevel',
                            'UserAccessLevel',
                            'MinimumSamplingInterval',
                            'Historizing',
                            'Executable',
                            'UserExecutable',
                            'DataTypeDefinition',
                            'RolePermissions',
                            'UserRolePermissions',
                            'AccessRestrictions'
                        ]
                    ],
                    'value' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'nodeId',
                    'attribute',
                    'value'
                ]
            ],
            'WriteRequestApiModel' => [
                'properties' => [
                    'attributes' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/AttributeWriteRequestApiModel']
                    ],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['attributes']
            ],
            'AttributeWriteResponseApiModel' => [
                'properties' => ['errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'WriteResponseApiModel' => [
                'properties' => ['results' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/AttributeWriteResponseApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ]
        ]
    ];
}
