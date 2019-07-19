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
        $this->_GetSetOfUniqueNodes_operation = $_client->createOperation('GetSetOfUniqueNodes');
        $this->_Browse_operation = $_client->createOperation('Browse');
        $this->_GetNextSetOfUniqueNodes_operation = $_client->createOperation('GetNextSetOfUniqueNodes');
        $this->_BrowseNext_operation = $_client->createOperation('BrowseNext');
        $this->_BrowseUsingPath_operation = $_client->createOperation('BrowseUsingPath');
        $this->_GetCallMetadata_operation = $_client->createOperation('GetCallMetadata');
        $this->_CallMethod_operation = $_client->createOperation('CallMethod');
        $this->_StartPublishingValues_operation = $_client->createOperation('StartPublishingValues');
        $this->_StopPublishingValues_operation = $_client->createOperation('StopPublishingValues');
        $this->_GetNextListOfPublishedNodes_operation = $_client->createOperation('GetNextListOfPublishedNodes');
        $this->_GetFirstListOfPublishedNodes_operation = $_client->createOperation('GetFirstListOfPublishedNodes');
        $this->_GetValue_operation = $_client->createOperation('GetValue');
        $this->_ReadValue_operation = $_client->createOperation('ReadValue');
        $this->_ReadAttributes_operation = $_client->createOperation('ReadAttributes');
        $this->_GetStatus_operation = $_client->createOperation('GetStatus');
        $this->_WriteValue_operation = $_client->createOperation('WriteValue');
        $this->_WriteAttributes_operation = $_client->createOperation('WriteAttributes');
    }
    /**
     * Browse the set of unique hierarchically referenced target nodes on the endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
The root node id to browse from can be provided as part of the query
parameters.
If it is not provided, the RootFolder node is browsed. Note that this
is the same as the POST method with the model containing the node id
and the targetNodesOnly flag set to true.
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
     * Browse a node on the specified endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function browse(
        $endpointId,
        array $request
    )
    {
        return $this->_Browse_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Browse the next set of unique hierarchically referenced target nodes on the
endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
Note that this is the same as the POST method with the model containing
the continuation token and the targetNodesOnly flag set to true.
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
     * Browse next set of references on the endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function browseNext(
        $endpointId,
        array $request
    )
    {
        return $this->_BrowseNext_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Browse using a path from the specified node id.
This call uses TranslateBrowsePathsToNodeIds service under the hood.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function browseUsingPath(
        $endpointId,
        array $request
    )
    {
        return $this->_BrowseUsingPath_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Return method meta data to support a user interface displaying forms to
input and output arguments.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function getCallMetadata(
        $endpointId,
        array $request
    )
    {
        return $this->_GetCallMetadata_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Invoke method node with specified input arguments.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function callMethod(
        $endpointId,
        array $request
    )
    {
        return $this->_CallMethod_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Start publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function startPublishingValues(
        $endpointId,
        array $request
    )
    {
        return $this->_StartPublishingValues_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Stop publishing variable node values to IoT Hub.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function stopPublishingValues(
        $endpointId,
        array $request
    )
    {
        return $this->_StopPublishingValues_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Returns next set of currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param string $continuationToken
     * @return array
     */
    public function getNextListOfPublishedNodes(
        $endpointId,
        $continuationToken
    )
    {
        return $this->_GetNextListOfPublishedNodes_operation->call([
            'endpointId' => $endpointId,
            'continuationToken' => $continuationToken
        ]);
    }
    /**
     * Returns currently published node ids for an endpoint.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function getFirstListOfPublishedNodes(
        $endpointId,
        array $request
    )
    {
        return $this->_GetFirstListOfPublishedNodes_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Get a variable node's value using its node id.
The endpoint must be activated and connected and the module client
and server must trust each other.
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
     * Read a variable node's value.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function readValue(
        $endpointId,
        array $request
    )
    {
        return $this->_ReadValue_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Read attributes of a node.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function readAttributes(
        $endpointId,
        array $request
    )
    {
        return $this->_ReadAttributes_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
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
     * Write variable node's value.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function writeValue(
        $endpointId,
        array $request
    )
    {
        return $this->_WriteValue_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Write any attribute of a node.
The endpoint must be activated and connected and the module client
and server must trust each other.
     * @param string $endpointId
     * @param array $request
     * @return array
     */
    public function writeAttributes(
        $endpointId,
        array $request
    )
    {
        return $this->_WriteAttributes_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetSetOfUniqueNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Browse_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetNextSetOfUniqueNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_BrowseNext_operation;
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
    private $_StartPublishingValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_StopPublishingValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetNextListOfPublishedNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFirstListOfPublishedNodes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetValue_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ReadValue_operation;
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
                ],
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
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/BrowseRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/BrowseResponseApiModel']]]
                ]
            ],
            '/v2/browse/{endpointId}/next' => [
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
                ],
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
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/BrowseNextRequestApiModel']
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
                        'name' => 'request',
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
                        'name' => 'request',
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
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/MethodCallRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/MethodCallResponseApiModel']]]
            ]],
            '/v2/publish/{endpointId}/start' => ['post' => [
                'operationId' => 'StartPublishingValues',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/PublishStartRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublishStartResponseApiModel']]]
            ]],
            '/v2/publish/{endpointId}/stop' => ['post' => [
                'operationId' => 'StopPublishingValues',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/PublishStopRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublishStopResponseApiModel']]]
            ]],
            '/v2/publish/{endpointId}' => [
                'get' => [
                    'operationId' => 'GetNextListOfPublishedNodes',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublishedItemListResponseApiModel']]]
                ],
                'post' => [
                    'operationId' => 'GetFirstListOfPublishedNodes',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/PublishedItemListRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublishedItemListResponseApiModel']]]
                ]
            ],
            '/v2/read/{endpointId}' => [
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
                ],
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
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ValueReadRequestApiModel']
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
                        'name' => 'request',
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
                        'name' => 'request',
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
                        'name' => 'request',
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
                            'ScalarOrOneDimension',
                            'Any',
                            'Scalar',
                            'OneOrMoreDimensions',
                            'OneDimension',
                            'TwoDimensions'
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
                'required' => ['methodId']
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
                            'ScalarOrOneDimension',
                            'Any',
                            'Scalar',
                            'OneOrMoreDimensions',
                            'OneDimension',
                            'TwoDimensions'
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
            'PublishedItemApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'nodeAttribute' => [
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
                    'publishingInterval' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'samplingInterval' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['nodeId']
            ],
            'PublishStartRequestApiModel' => [
                'properties' => [
                    'item' => ['$ref' => '#/definitions/PublishedItemApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['item']
            ],
            'PublishStartResponseApiModel' => [
                'properties' => ['errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublishStopRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'nodeAttribute' => [
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
                    'diagnostics' => ['$ref' => '#/definitions/DiagnosticsApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['nodeId']
            ],
            'PublishStopResponseApiModel' => [
                'properties' => ['errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublishedItemListRequestApiModel' => [
                'properties' => ['continuationToken' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublishedItemListResponseApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/PublishedItemApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
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
                'required' => ['nodeId']
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
                'required' => [
                    'nodeId',
                    'value'
                ]
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
