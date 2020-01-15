<?php
namespace Microsoft\Azure\IIoT\Api;
final class AzureOpcHistoryClient
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
        $this->_HistoryDeleteValuesAtTimes_operation = $_client->createOperation('HistoryDeleteValuesAtTimes');
        $this->_HistoryDeleteValues_operation = $_client->createOperation('HistoryDeleteValues');
        $this->_HistoryDeleteModifiedValues_operation = $_client->createOperation('HistoryDeleteModifiedValues');
        $this->_HistoryDeleteEvents_operation = $_client->createOperation('HistoryDeleteEvents');
        $this->_HistoryReadRaw_operation = $_client->createOperation('HistoryReadRaw');
        $this->_HistoryReadRawNext_operation = $_client->createOperation('HistoryReadRawNext');
        $this->_HistoryUpdateRaw_operation = $_client->createOperation('HistoryUpdateRaw');
        $this->_HistoryInsertValues_operation = $_client->createOperation('HistoryInsertValues');
        $this->_HistoryInsertEvents_operation = $_client->createOperation('HistoryInsertEvents');
        $this->_HistoryReadEvents_operation = $_client->createOperation('HistoryReadEvents');
        $this->_HistoryReadEventsNext_operation = $_client->createOperation('HistoryReadEventsNext');
        $this->_HistoryReadValues_operation = $_client->createOperation('HistoryReadValues');
        $this->_HistoryReadValuesAtTimes_operation = $_client->createOperation('HistoryReadValuesAtTimes');
        $this->_HistoryReadProcessedValues_operation = $_client->createOperation('HistoryReadProcessedValues');
        $this->_HistoryReadModifiedValues_operation = $_client->createOperation('HistoryReadModifiedValues');
        $this->_HistoryReadValueNext_operation = $_client->createOperation('HistoryReadValueNext');
        $this->_HistoryReplaceValues_operation = $_client->createOperation('HistoryReplaceValues');
        $this->_HistoryReplaceEvents_operation = $_client->createOperation('HistoryReplaceEvents');
    }
    /**
     * Delete value history using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyDeleteValuesAtTimes(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryDeleteValuesAtTimes_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Delete historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyDeleteValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryDeleteValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Delete historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyDeleteModifiedValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryDeleteModifiedValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Delete historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyDeleteEvents(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryDeleteEvents_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read node history if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadRaw(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadRaw_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read next batch of node history values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadRawNext(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadRawNext_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Update node history using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyUpdateRaw(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryUpdateRaw_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Insert historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyInsertValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryInsertValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Insert historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyInsertEvents(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryInsertEvents_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read historic events of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadEvents(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadEvents_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read next batch of historic events of a node using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadEventsNext(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadEventsNext_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read historic values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadValuesAtTimes(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadValuesAtTimes_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadProcessedValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadProcessedValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read processed history values of a node if available using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadModifiedValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadModifiedValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Read next batch of historic values of a node using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReadValueNext(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReadValueNext_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Replace historic values using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReplaceValues(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReplaceValues_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * Replace historic events using historic access. The endpoint must be activated and connected and the module client and server must trust each other.
     * @param string $endpointId
     * @param array $body
     * @return array
     */
    public function historyReplaceEvents(
        $endpointId,
        array $body
    )
    {
        return $this->_HistoryReplaceEvents_operation->call([
            'endpointId' => $endpointId,
            'body' => $body
        ]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryDeleteValuesAtTimes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryDeleteValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryDeleteModifiedValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryDeleteEvents_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadRaw_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadRawNext_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryUpdateRaw_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryInsertValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryInsertEvents_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadEvents_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadEventsNext_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadValuesAtTimes_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadProcessedValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadModifiedValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReadValueNext_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReplaceValues_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_HistoryReplaceEvents_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/delete/{endpointId}/values/pick' => ['post' => [
                'operationId' => 'HistoryDeleteValuesAtTimes',
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
                        'schema' => ['$ref' => '#/definitions/DeleteValuesAtTimesDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/delete/{endpointId}/values' => ['post' => [
                'operationId' => 'HistoryDeleteValues',
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
                        'schema' => ['$ref' => '#/definitions/DeleteValuesDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/delete/{endpointId}/values/modified' => ['post' => [
                'operationId' => 'HistoryDeleteModifiedValues',
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
                        'schema' => ['$ref' => '#/definitions/DeleteModifiedValuesDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/delete/{endpointId}/events' => ['post' => [
                'operationId' => 'HistoryDeleteEvents',
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
                        'schema' => ['$ref' => '#/definitions/DeleteEventsDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/history/read/{endpointId}' => ['post' => [
                'operationId' => 'HistoryReadRaw',
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
                        'schema' => ['$ref' => '#/definitions/JTokenHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/JTokenHistoryReadResponseApiModel']]]
            ]],
            '/v2/history/read/{endpointId}/next' => ['post' => [
                'operationId' => 'HistoryReadRawNext',
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
                        'schema' => ['$ref' => '#/definitions/HistoryReadNextRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/JTokenHistoryReadNextResponseApiModel']]]
            ]],
            '/v2/history/update/{endpointId}' => ['post' => [
                'operationId' => 'HistoryUpdateRaw',
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
                        'schema' => ['$ref' => '#/definitions/JTokenHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/insert/{endpointId}/values' => ['post' => [
                'operationId' => 'HistoryInsertValues',
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
                        'schema' => ['$ref' => '#/definitions/InsertValuesDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/insert/{endpointId}/events' => ['post' => [
                'operationId' => 'HistoryInsertEvents',
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
                        'schema' => ['$ref' => '#/definitions/InsertEventsDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/events' => ['post' => [
                'operationId' => 'HistoryReadEvents',
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
                        'schema' => ['$ref' => '#/definitions/ReadEventsDetailsApiModelHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricEventApiModel[]HistoryReadResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/events/next' => ['post' => [
                'operationId' => 'HistoryReadEventsNext',
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
                        'schema' => ['$ref' => '#/definitions/HistoryReadNextRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricEventApiModel[]HistoryReadNextResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/values' => ['post' => [
                'operationId' => 'HistoryReadValues',
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
                        'schema' => ['$ref' => '#/definitions/ReadValuesDetailsApiModelHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricValueApiModel[]HistoryReadResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/values/pick' => ['post' => [
                'operationId' => 'HistoryReadValuesAtTimes',
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
                        'schema' => ['$ref' => '#/definitions/ReadValuesAtTimesDetailsApiModelHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricValueApiModel[]HistoryReadResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/values/processed' => ['post' => [
                'operationId' => 'HistoryReadProcessedValues',
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
                        'schema' => ['$ref' => '#/definitions/ReadProcessedValuesDetailsApiModelHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricValueApiModel[]HistoryReadResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/values/modified' => ['post' => [
                'operationId' => 'HistoryReadModifiedValues',
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
                        'schema' => ['$ref' => '#/definitions/ReadModifiedValuesDetailsApiModelHistoryReadRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricValueApiModel[]HistoryReadResponseApiModel']]]
            ]],
            '/v2/read/{endpointId}/values/next' => ['post' => [
                'operationId' => 'HistoryReadValueNext',
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
                        'schema' => ['$ref' => '#/definitions/HistoryReadNextRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoricValueApiModel[]HistoryReadNextResponseApiModel']]]
            ]],
            '/v2/replace/{endpointId}/values' => ['post' => [
                'operationId' => 'HistoryReplaceValues',
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
                        'schema' => ['$ref' => '#/definitions/ReplaceValuesDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]],
            '/v2/replace/{endpointId}/events' => ['post' => [
                'operationId' => 'HistoryReplaceEvents',
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
                        'schema' => ['$ref' => '#/definitions/ReplaceEventsDetailsApiModelHistoryUpdateRequestApiModel']
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/HistoryUpdateResponseApiModel']]]
            ]]
        ],
        'definitions' => [
            'DeleteValuesAtTimesDetailsApiModel' => [
                'properties' => ['reqTimes' => [
                    'type' => 'array',
                    'items' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ]],
                'additionalProperties' => FALSE,
                'required' => ['reqTimes']
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
            'DeleteValuesAtTimesDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/DeleteValuesAtTimesDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
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
            'HistoryUpdateResponseApiModel' => [
                'properties' => [
                    'results' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/ServiceResultApiModel']
                    ],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DeleteValuesDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DeleteValuesDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/DeleteValuesDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'DeleteModifiedValuesDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DeleteModifiedValuesDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/DeleteModifiedValuesDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'DeleteEventsDetailsApiModel' => [
                'properties' => ['eventIds' => [
                    'type' => 'array',
                    'items' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ]
                ]],
                'additionalProperties' => FALSE,
                'required' => ['eventIds']
            ],
            'DeleteEventsDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/DeleteEventsDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'JTokenHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['type' => 'object'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'JTokenHistoryReadResponseApiModel' => [
                'properties' => [
                    'history' => ['type' => 'object'],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoryReadNextRequestApiModel' => [
                'properties' => [
                    'continuationToken' => ['type' => 'string'],
                    'abort' => ['type' => 'boolean'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['continuationToken']
            ],
            'JTokenHistoryReadNextResponseApiModel' => [
                'properties' => [
                    'history' => ['type' => 'object'],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'JTokenHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['type' => 'object'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'ModificationInfoApiModel' => [
                'properties' => [
                    'modificationTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'updateType' => [
                        'type' => 'string',
                        'enum' => [
                            'Insert',
                            'Replace',
                            'Update',
                            'Delete'
                        ]
                    ],
                    'userName' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricValueApiModel' => [
                'properties' => [
                    'value' => ['type' => 'object'],
                    'statusCode' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'sourceTimestamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'sourcePicoseconds' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'serverTimestamp' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'serverPicoseconds' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'modificationInfo' => ['$ref' => '#/definitions/ModificationInfoApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'InsertValuesDetailsApiModel' => [
                'properties' => ['values' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/HistoricValueApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => ['values']
            ],
            'InsertValuesDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/InsertValuesDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'SimpleAttributeOperandApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'attributeId' => [
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
                    'indexRange' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'FilterOperandApiModel' => [
                'properties' => [
                    'index' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'value' => ['type' => 'object'],
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'attributeId' => [
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
                    'indexRange' => ['type' => 'string'],
                    'alias' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ContentFilterElementApiModel' => [
                'properties' => [
                    'filterOperator' => [
                        'type' => 'string',
                        'enum' => [
                            'Equals',
                            'IsNull',
                            'GreaterThan',
                            'LessThan',
                            'GreaterThanOrEqual',
                            'LessThanOrEqual',
                            'Like',
                            'Not',
                            'Between',
                            'InList',
                            'And',
                            'Or',
                            'Cast',
                            'InView',
                            'OfType',
                            'RelatedTo',
                            'BitwiseAnd',
                            'BitwiseOr'
                        ]
                    ],
                    'filterOperands' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/FilterOperandApiModel']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ContentFilterApiModel' => [
                'properties' => ['elements' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/ContentFilterElementApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'EventFilterApiModel' => [
                'properties' => [
                    'selectClauses' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/SimpleAttributeOperandApiModel']
                    ],
                    'whereClause' => ['$ref' => '#/definitions/ContentFilterApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricEventApiModel' => [
                'properties' => ['eventFields' => [
                    'type' => 'array',
                    'items' => ['type' => 'object']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'InsertEventsDetailsApiModel' => [
                'properties' => [
                    'filter' => ['$ref' => '#/definitions/EventFilterApiModel'],
                    'events' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricEventApiModel']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['events']
            ],
            'InsertEventsDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/InsertEventsDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'ReadEventsDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'numEvents' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'filter' => ['$ref' => '#/definitions/EventFilterApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadEventsDetailsApiModelHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReadEventsDetailsApiModel'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricEventApiModel[]HistoryReadResponseApiModel' => [
                'properties' => [
                    'history' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricEventApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricEventApiModel[]HistoryReadNextResponseApiModel' => [
                'properties' => [
                    'history' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricEventApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadValuesDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'numValues' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'returnBounds' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadValuesDetailsApiModelHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReadValuesDetailsApiModel'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricValueApiModel[]HistoryReadResponseApiModel' => [
                'properties' => [
                    'history' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricValueApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadValuesAtTimesDetailsApiModel' => [
                'properties' => [
                    'reqTimes' => [
                        'type' => 'array',
                        'items' => [
                            'type' => 'string',
                            'format' => 'date-time'
                        ]
                    ],
                    'useSimpleBounds' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => ['reqTimes']
            ],
            'ReadValuesAtTimesDetailsApiModelHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReadValuesAtTimesDetailsApiModel'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'AggregateConfigurationApiModel' => [
                'properties' => [
                    'useServerCapabilitiesDefaults' => ['type' => 'boolean'],
                    'treatUncertainAsBad' => ['type' => 'boolean'],
                    'percentDataBad' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'percentDataGood' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'useSlopedExtrapolation' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadProcessedValuesDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'processingInterval' => [
                        'type' => 'number',
                        'format' => 'double'
                    ],
                    'aggregateTypeId' => ['type' => 'string'],
                    'aggregateConfiguration' => ['$ref' => '#/definitions/AggregateConfigurationApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadProcessedValuesDetailsApiModelHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReadProcessedValuesDetailsApiModel'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadModifiedValuesDetailsApiModel' => [
                'properties' => [
                    'startTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'endTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'numValues' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReadModifiedValuesDetailsApiModelHistoryReadRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReadModifiedValuesDetailsApiModel'],
                    'indexRange' => ['type' => 'string'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'HistoricValueApiModel[]HistoryReadNextResponseApiModel' => [
                'properties' => [
                    'history' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricValueApiModel']
                    ],
                    'continuationToken' => ['type' => 'string'],
                    'errorInfo' => ['$ref' => '#/definitions/ServiceResultApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ReplaceValuesDetailsApiModel' => [
                'properties' => ['values' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/HistoricValueApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => ['values']
            ],
            'ReplaceValuesDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReplaceValuesDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ],
            'ReplaceEventsDetailsApiModel' => [
                'properties' => [
                    'filter' => ['$ref' => '#/definitions/EventFilterApiModel'],
                    'events' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/HistoricEventApiModel']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['events']
            ],
            'ReplaceEventsDetailsApiModelHistoryUpdateRequestApiModel' => [
                'properties' => [
                    'nodeId' => ['type' => 'string'],
                    'browsePath' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'details' => ['$ref' => '#/definitions/ReplaceEventsDetailsApiModel'],
                    'header' => ['$ref' => '#/definitions/RequestHeaderApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['details']
            ]
        ]
    ];
}
