<?php
namespace Microsoft\Azure\IIoT\Api;
final class AzureOpcPublisherClient
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
        $this->_Subscribe_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe_operation = $_client->createOperation('Unsubscribe');
        $this->_StartPublishingValues_operation = $_client->createOperation('StartPublishingValues');
        $this->_StopPublishingValues_operation = $_client->createOperation('StopPublishingValues');
        $this->_GetNextListOfPublishedNodes_operation = $_client->createOperation('GetNextListOfPublishedNodes');
        $this->_GetFirstListOfPublishedNodes_operation = $_client->createOperation('GetFirstListOfPublishedNodes');
        $this->_GetStatus_operation = $_client->createOperation('GetStatus');
    }
    /**
     * Register a client to receive publisher samples through SignalR.
     * @param string $endpointId
     * @param string|null $userId
     */
    public function subscribe(
        $endpointId,
        $userId = null
    )
    {
        return $this->_Subscribe_operation->call([
            'endpointId' => $endpointId,
            'userId' => $userId
        ]);
    }
    /**
     * Unregister a client and stop it from receiving samples.
     * @param string $endpointId
     * @param string $userId
     */
    public function unsubscribe(
        $endpointId,
        $userId
    )
    {
        return $this->_Unsubscribe_operation->call([
            'endpointId' => $endpointId,
            'userId' => $userId
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
     * @return array
     */
    public function getStatus()
    {
        return $this->_GetStatus_operation->call([]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe_operation;
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
    private $_GetStatus_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/monitor/{endpointId}/samples' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'userId',
                        'in' => 'body',
                        'required' => FALSE,
                        'schema' => ['type' => 'string']
                    ]
                ],
                'responses' => ['200' => []]
            ]],
            '/v2/monitor/{endpointId}/samples/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [
                    [
                        'name' => 'endpointId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'userId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]
                ],
                'responses' => ['200' => []]
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
            '/v2/status' => ['get' => [
                'operationId' => 'GetStatus',
                'parameters' => [],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StatusResponseApiModel']]]
            ]]
        ],
        'definitions' => [
            'PublishedItemApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'publishingInterval' => ['type' => 'string'],
                    'samplingInterval' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => ['nodeId']
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
            'PublishStartRequestApiModel' => [
                'properties' => [
                    'item' => ['$ref' => '#/definitions/PublishedItemApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['item']
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
            'PublishStartResponseApiModel' => [
                'properties' => ['errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublishStopRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
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
            ]
        ]
    ];
}
