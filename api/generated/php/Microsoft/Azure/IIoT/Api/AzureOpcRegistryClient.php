<?php
namespace Microsoft\Azure\IIoT\Api;
final class AzureOpcRegistryClient
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
        $this->_GetListOfApplications_operation = $_client->createOperation('GetListOfApplications');
        $this->_CreateApplication_operation = $_client->createOperation('CreateApplication');
        $this->_RegisterServer_operation = $_client->createOperation('RegisterServer');
        $this->_DeleteAllDisabledApplications_operation = $_client->createOperation('DeleteAllDisabledApplications');
        $this->_DisableApplication_operation = $_client->createOperation('DisableApplication');
        $this->_EnableApplication_operation = $_client->createOperation('EnableApplication');
        $this->_DiscoverServer_operation = $_client->createOperation('DiscoverServer');
        $this->_GetApplicationRegistration_operation = $_client->createOperation('GetApplicationRegistration');
        $this->_DeleteApplication_operation = $_client->createOperation('DeleteApplication');
        $this->_UpdateApplicationRegistration_operation = $_client->createOperation('UpdateApplicationRegistration');
        $this->_GetListOfSites_operation = $_client->createOperation('GetListOfSites');
        $this->_GetFilteredListOfApplications_operation = $_client->createOperation('GetFilteredListOfApplications');
        $this->_QueryApplications_operation = $_client->createOperation('QueryApplications');
        $this->_QueryApplicationsById_operation = $_client->createOperation('QueryApplicationsById');
        $this->_ActivateEndpoint_operation = $_client->createOperation('ActivateEndpoint');
        $this->_GetEndpoint_operation = $_client->createOperation('GetEndpoint');
        $this->_UpdateEndpoint_operation = $_client->createOperation('UpdateEndpoint');
        $this->_GetListOfEndpoints_operation = $_client->createOperation('GetListOfEndpoints');
        $this->_GetFilteredListOfEndpoints_operation = $_client->createOperation('GetFilteredListOfEndpoints');
        $this->_QueryEndpoints_operation = $_client->createOperation('QueryEndpoints');
        $this->_DeactivateEndpoint_operation = $_client->createOperation('DeactivateEndpoint');
        $this->_GetStatus_operation = $_client->createOperation('GetStatus');
        $this->_GetSupervisor_operation = $_client->createOperation('GetSupervisor');
        $this->_UpdateSupervisor_operation = $_client->createOperation('UpdateSupervisor');
        $this->_GetSupervisorStatus_operation = $_client->createOperation('GetSupervisorStatus');
        $this->_ResetSupervisor_operation = $_client->createOperation('ResetSupervisor');
        $this->_GetListOfSupervisors_operation = $_client->createOperation('GetListOfSupervisors');
        $this->_GetFilteredListOfSupervisors_operation = $_client->createOperation('GetFilteredListOfSupervisors');
        $this->_QuerySupervisors_operation = $_client->createOperation('QuerySupervisors');
    }
    /**
     * Get all registered applications in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfApplications(
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfApplications_operation->call([
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * The application is registered using the provided information, but it
is not associated with a supervisor.  This is useful for when you need
to register clients or you want to register a server that is located
in a network not reachable through a Twin module.
     * @param array $request
     * @return array
     */
    public function createApplication(array $request)
    {
        return $this->_CreateApplication_operation->call(['request' => $request]);
    }
    /**
     * Registers a server solely using a discovery url. Requires that
the onboarding agent service is running and the server can be
located by a supervisor in its network using the discovery url.
     * @param array $request
     */
    public function registerServer(array $request)
    {
        return $this->_RegisterServer_operation->call(['request' => $request]);
    }
    /**
     * Purges all applications that have not been seen for a specified amount of time.
     * @param string|null $notSeenFor
     */
    public function deleteAllDisabledApplications($notSeenFor = null)
    {
        return $this->_DeleteAllDisabledApplications_operation->call(['notSeenFor' => $notSeenFor]);
    }
    /**
     * A manager can disable an application.
     * @param string $applicationId
     */
    public function disableApplication($applicationId)
    {
        return $this->_DisableApplication_operation->call(['applicationId' => $applicationId]);
    }
    /**
     * A manager can enable an application.
     * @param string $applicationId
     */
    public function enableApplication($applicationId)
    {
        return $this->_EnableApplication_operation->call(['applicationId' => $applicationId]);
    }
    /**
     * Registers servers by running a discovery scan in a supervisor's
network. Requires that the onboarding agent service is running.
     * @param array $request
     */
    public function discoverServer(array $request)
    {
        return $this->_DiscoverServer_operation->call(['request' => $request]);
    }
    /**
     * @param string $applicationId
     * @return array
     */
    public function getApplicationRegistration($applicationId)
    {
        return $this->_GetApplicationRegistration_operation->call(['applicationId' => $applicationId]);
    }
    /**
     * Unregisters and deletes application and all its associated endpoints.
     * @param string $applicationId
     */
    public function deleteApplication($applicationId)
    {
        return $this->_DeleteApplication_operation->call(['applicationId' => $applicationId]);
    }
    /**
     * The application information is updated with new properties.  Note that
this information might be overridden if the application is re-discovered
during a discovery run (recurring or one-time).
     * @param string $applicationId
     * @param array $request
     */
    public function updateApplicationRegistration(
        $applicationId,
        array $request
    )
    {
        return $this->_UpdateApplicationRegistration_operation->call([
            'applicationId' => $applicationId,
            'request' => $request
        ]);
    }
    /**
     * List all sites applications are registered in.
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfSites(
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfSites_operation->call([
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get a list of applications filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfApplications operation using the token to retrieve
more results.
     * @param array $query
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfApplications(
        array $query,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfApplications_operation->call([
            'query' => $query,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * List applications that match a query model.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfApplications operation using the token to retrieve
more results.
     * @param array $query
     * @param integer|null $pageSize
     * @return array
     */
    public function queryApplications(
        array $query,
        $pageSize = null
    )
    {
        return $this->_QueryApplications_operation->call([
            'query' => $query,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * A query model which supports the OPC UA Global Discovery Server query.
     * @param array|null $query
     * @return array
     */
    public function queryApplicationsById(array $query = null)
    {
        return $this->_QueryApplicationsById_operation->call(['query' => $query]);
    }
    /**
     * Activates an endpoint for subsequent use in twin service.
All endpoints must be activated using this API or through a
activation filter during application registration or discovery.
     * @param string $endpointId
     */
    public function activateEndpoint($endpointId)
    {
        return $this->_ActivateEndpoint_operation->call(['endpointId' => $endpointId]);
    }
    /**
     * Gets information about an endpoint.
     * @param string $endpointId
     * @param boolean|null $onlyServerState
     * @return array
     */
    public function getEndpoint(
        $endpointId,
        $onlyServerState = null
    )
    {
        return $this->_GetEndpoint_operation->call([
            'endpointId' => $endpointId,
            'onlyServerState' => $onlyServerState
        ]);
    }
    /**
     * @param string $endpointId
     * @param array $request
     */
    public function updateEndpoint(
        $endpointId,
        array $request
    )
    {
        return $this->_UpdateEndpoint_operation->call([
            'endpointId' => $endpointId,
            'request' => $request
        ]);
    }
    /**
     * Get all registered endpoints in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfEndpoints(
        $onlyServerState = null,
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfEndpoints_operation->call([
            'onlyServerState' => $onlyServerState,
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get a list of endpoints filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfEndpoints operation using the token to retrieve
more results.
     * @param string|null $url
     * @param string|null $userAuthentication
     * @param string|null $certificate
     * @param string|null $securityMode
     * @param string|null $securityPolicy
     * @param boolean|null $activated
     * @param boolean|null $connected
     * @param string|null $endpointState
     * @param boolean|null $includeNotSeenSince
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfEndpoints(
        $url = null,
        $userAuthentication = null,
        $certificate = null,
        $securityMode = null,
        $securityPolicy = null,
        $activated = null,
        $connected = null,
        $endpointState = null,
        $includeNotSeenSince = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfEndpoints_operation->call([
            'Url' => $url,
            'UserAuthentication' => $userAuthentication,
            'Certificate' => $certificate,
            'SecurityMode' => $securityMode,
            'SecurityPolicy' => $securityPolicy,
            'Activated' => $activated,
            'Connected' => $connected,
            'EndpointState' => $endpointState,
            'IncludeNotSeenSince' => $includeNotSeenSince,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Return endpoints that match the specified query.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfEndpoints operation using the token to retrieve
more results.
     * @param array $query
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function queryEndpoints(
        array $query,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_QueryEndpoints_operation->call([
            'query' => $query,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Deactivates the endpoint and disable access through twin service.
     * @param string $endpointId
     */
    public function deactivateEndpoint($endpointId)
    {
        return $this->_DeactivateEndpoint_operation->call(['endpointId' => $endpointId]);
    }
    /**
     * @return array
     */
    public function getStatus()
    {
        return $this->_GetStatus_operation->call([]);
    }
    /**
     * Returns a supervisor's registration and connectivity information.
A supervisor id corresponds to the twin modules module identity.
     * @param string $supervisorId
     * @param boolean|null $onlyServerState
     * @return array
     */
    public function getSupervisor(
        $supervisorId,
        $onlyServerState = null
    )
    {
        return $this->_GetSupervisor_operation->call([
            'supervisorId' => $supervisorId,
            'onlyServerState' => $onlyServerState
        ]);
    }
    /**
     * Allows a caller to configure recurring discovery runs on the twin module
identified by the supervisor id or update site information.
     * @param string $supervisorId
     * @param array $request
     */
    public function updateSupervisor(
        $supervisorId,
        array $request
    )
    {
        return $this->_UpdateSupervisor_operation->call([
            'supervisorId' => $supervisorId,
            'request' => $request
        ]);
    }
    /**
     * Allows a caller to get runtime status for a supervisor.
     * @param string $supervisorId
     * @return array
     */
    public function getSupervisorStatus($supervisorId)
    {
        return $this->_GetSupervisorStatus_operation->call(['supervisorId' => $supervisorId]);
    }
    /**
     * Allows a caller to reset the twin module using its supervisor
identity identifier.
     * @param string $supervisorId
     */
    public function resetSupervisor($supervisorId)
    {
        return $this->_ResetSupervisor_operation->call(['supervisorId' => $supervisorId]);
    }
    /**
     * Get all registered supervisors and therefore twin modules in paged form.
The returned model can contain a continuation token if more results are
available.
Call this operation again using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfSupervisors(
        $onlyServerState = null,
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfSupervisors_operation->call([
            'onlyServerState' => $onlyServerState,
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get a list of supervisors filtered using the specified query parameters.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfSupervisors operation using the token to retrieve
more results.
     * @param string|null $siteId
     * @param string|null $discovery
     * @param boolean|null $connected
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfSupervisors(
        $siteId = null,
        $discovery = null,
        $connected = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfSupervisors_operation->call([
            'SiteId' => $siteId,
            'Discovery' => $discovery,
            'Connected' => $connected,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get all supervisors that match a specified query.
The returned model can contain a continuation token if more results are
available.
Call the GetListOfSupervisors operation using the token to retrieve
more results.
     * @param array $query
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function querySupervisors(
        array $query,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_QuerySupervisors_operation->call([
            'query' => $query,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_CreateApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_RegisterServer_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteAllDisabledApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DisableApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_EnableApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DiscoverServer_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetApplicationRegistration_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateApplicationRegistration_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfSites_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryApplicationsById_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ActivateEndpoint_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetEndpoint_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateEndpoint_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeactivateEndpoint_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetStatus_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetSupervisor_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateSupervisor_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetSupervisorStatus_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ResetSupervisor_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfSupervisors_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfSupervisors_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QuerySupervisors_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/applications' => [
                'get' => [
                    'operationId' => 'GetListOfApplications',
                    'parameters' => [
                        [
                            'name' => 'continuationToken',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationInfoListApiModel']]]
                ],
                'put' => [
                    'operationId' => 'CreateApplication',
                    'parameters' => [[
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ApplicationRegistrationRequestApiModel']
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationRegistrationResponseApiModel']]]
                ],
                'post' => [
                    'operationId' => 'RegisterServer',
                    'parameters' => [[
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ServerRegistrationRequestApiModel']
                    ]],
                    'responses' => ['200' => []]
                ],
                'delete' => [
                    'operationId' => 'DeleteAllDisabledApplications',
                    'parameters' => [[
                        'name' => 'notSeenFor',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/applications/{applicationId}/disable' => ['post' => [
                'operationId' => 'DisableApplication',
                'parameters' => [[
                    'name' => 'applicationId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/applications/{applicationId}/enable' => ['post' => [
                'operationId' => 'EnableApplication',
                'parameters' => [[
                    'name' => 'applicationId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/applications/discover' => ['post' => [
                'operationId' => 'DiscoverServer',
                'parameters' => [[
                    'name' => 'request',
                    'in' => 'body',
                    'required' => TRUE,
                    'schema' => ['$ref' => '#/definitions/DiscoveryRequestApiModel']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/applications/{applicationId}' => [
                'get' => [
                    'operationId' => 'GetApplicationRegistration',
                    'parameters' => [[
                        'name' => 'applicationId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationRegistrationApiModel']]]
                ],
                'delete' => [
                    'operationId' => 'DeleteApplication',
                    'parameters' => [[
                        'name' => 'applicationId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => []]
                ],
                'patch' => [
                    'operationId' => 'UpdateApplicationRegistration',
                    'parameters' => [
                        [
                            'name' => 'applicationId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/applications/sites' => ['get' => [
                'operationId' => 'GetListOfSites',
                'parameters' => [
                    [
                        'name' => 'continuationToken',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'pageSize',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationSiteListApiModel']]]
            ]],
            '/v2/applications/query' => [
                'get' => [
                    'operationId' => 'GetFilteredListOfApplications',
                    'parameters' => [
                        [
                            'name' => 'query',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationQueryApiModel']
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationInfoListApiModel']]]
                ],
                'post' => [
                    'operationId' => 'QueryApplications',
                    'parameters' => [
                        [
                            'name' => 'query',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationQueryApiModel']
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationInfoListApiModel']]]
                ]
            ],
            '/v2/applications/querybyid' => ['post' => [
                'operationId' => 'QueryApplicationsById',
                'parameters' => [[
                    'name' => 'query',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['$ref' => '#/definitions/ApplicationRecordQueryApiModel']
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationRecordListApiModel']]]
            ]],
            '/v2/endpoints/{endpointId}/activate' => ['post' => [
                'operationId' => 'ActivateEndpoint',
                'parameters' => [[
                    'name' => 'endpointId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/endpoints/{endpointId}' => [
                'get' => [
                    'operationId' => 'GetEndpoint',
                    'parameters' => [
                        [
                            'name' => 'endpointId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/EndpointInfoApiModel']]]
                ],
                'patch' => [
                    'operationId' => 'UpdateEndpoint',
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
                            'schema' => ['$ref' => '#/definitions/EndpointRegistrationUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/endpoints' => ['get' => [
                'operationId' => 'GetListOfEndpoints',
                'parameters' => [
                    [
                        'name' => 'onlyServerState',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'boolean'
                    ],
                    [
                        'name' => 'continuationToken',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'pageSize',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/EndpointInfoListApiModel']]]
            ]],
            '/v2/endpoints/query' => [
                'get' => [
                    'operationId' => 'GetFilteredListOfEndpoints',
                    'parameters' => [
                        [
                            'name' => 'Url',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'UserAuthentication',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'enum' => [
                                'None',
                                'UserName',
                                'X509Certificate',
                                'JwtToken'
                            ]
                        ],
                        [
                            'name' => 'Certificate',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'format' => 'byte'
                        ],
                        [
                            'name' => 'SecurityMode',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'enum' => [
                                'Best',
                                'Sign',
                                'SignAndEncrypt',
                                'None'
                            ]
                        ],
                        [
                            'name' => 'SecurityPolicy',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'Activated',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'Connected',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'EndpointState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'enum' => [
                                'Connecting',
                                'NotReachable',
                                'Busy',
                                'NoTrust',
                                'CertificateInvalid',
                                'Ready',
                                'Error'
                            ]
                        ],
                        [
                            'name' => 'IncludeNotSeenSince',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/EndpointInfoListApiModel']]]
                ],
                'post' => [
                    'operationId' => 'QueryEndpoints',
                    'parameters' => [
                        [
                            'name' => 'query',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/EndpointRegistrationQueryApiModel']
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/EndpointInfoListApiModel']]]
                ]
            ],
            '/v2/endpoints/{endpointId}/deactivate' => ['post' => [
                'operationId' => 'DeactivateEndpoint',
                'parameters' => [[
                    'name' => 'endpointId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/status' => ['get' => [
                'operationId' => 'GetStatus',
                'parameters' => [],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StatusResponseApiModel']]]
            ]],
            '/v2/supervisors/{supervisorId}' => [
                'get' => [
                    'operationId' => 'GetSupervisor',
                    'parameters' => [
                        [
                            'name' => 'supervisorId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorApiModel']]]
                ],
                'patch' => [
                    'operationId' => 'UpdateSupervisor',
                    'parameters' => [
                        [
                            'name' => 'supervisorId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/SupervisorUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/supervisors/{supervisorId}/status' => ['get' => [
                'operationId' => 'GetSupervisorStatus',
                'parameters' => [[
                    'name' => 'supervisorId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorStatusApiModel']]]
            ]],
            '/v2/supervisors/{supervisorId}/reset' => ['post' => [
                'operationId' => 'ResetSupervisor',
                'parameters' => [[
                    'name' => 'supervisorId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/supervisors' => ['get' => [
                'operationId' => 'GetListOfSupervisors',
                'parameters' => [
                    [
                        'name' => 'onlyServerState',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'boolean'
                    ],
                    [
                        'name' => 'continuationToken',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'pageSize',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorListApiModel']]]
            ]],
            '/v2/supervisors/query' => [
                'get' => [
                    'operationId' => 'GetFilteredListOfSupervisors',
                    'parameters' => [
                        [
                            'name' => 'SiteId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'Discovery',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'enum' => [
                                'Off',
                                'Local',
                                'Network',
                                'Fast',
                                'Scan'
                            ]
                        ],
                        [
                            'name' => 'Connected',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorListApiModel']]]
                ],
                'post' => [
                    'operationId' => 'QuerySupervisors',
                    'parameters' => [
                        [
                            'name' => 'query',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/SupervisorQueryApiModel']
                        ],
                        [
                            'name' => 'onlyServerState',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorListApiModel']]]
                ]
            ]
        ],
        'definitions' => [
            'CallbackApiModel' => [
                'properties' => [
                    'uri' => ['type' => 'string'],
                    'method' => [
                        'type' => 'string',
                        'enum' => [
                            'Get',
                            'Post',
                            'Put',
                            'Delete'
                        ]
                    ],
                    'authenticationHeader' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'EndpointActivationFilterApiModel' => [
                'properties' => [
                    'trustLists' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'securityPolicies' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'securityMode' => [
                        'type' => 'string',
                        'enum' => [
                            'Best',
                            'Sign',
                            'SignAndEncrypt',
                            'None'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ServerRegistrationRequestApiModel' => [
                'properties' => [
                    'discoveryUrl' => ['type' => 'string'],
                    'id' => ['type' => 'string'],
                    'callback' => ['$ref' => '#/definitions/CallbackApiModel'],
                    'activationFilter' => ['$ref' => '#/definitions/EndpointActivationFilterApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['discoveryUrl']
            ],
            'ApplicationRegistrationRequestApiModel' => [
                'properties' => [
                    'applicationUri' => ['type' => 'string'],
                    'applicationType' => [
                        'type' => 'string',
                        'enum' => [
                            'Server',
                            'Client',
                            'ClientAndServer',
                            'DiscoveryServer'
                        ]
                    ],
                    'productUri' => ['type' => 'string'],
                    'applicationName' => ['type' => 'string'],
                    'locale' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
                    'localizedNames' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    'capabilities' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryUrls' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryProfileUri' => ['type' => 'string'],
                    'gatewayServerUri' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => ['applicationUri']
            ],
            'ApplicationRegistrationResponseApiModel' => [
                'properties' => ['id' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'RegistryOperationApiModel' => [
                'properties' => [
                    'authorityId' => ['type' => 'string'],
                    'time' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'authorityId',
                    'time'
                ]
            ],
            'ApplicationInfoApiModel' => [
                'properties' => [
                    'applicationId' => ['type' => 'string'],
                    'applicationType' => [
                        'type' => 'string',
                        'enum' => [
                            'Server',
                            'Client',
                            'ClientAndServer',
                            'DiscoveryServer'
                        ]
                    ],
                    'applicationUri' => ['type' => 'string'],
                    'productUri' => ['type' => 'string'],
                    'applicationName' => ['type' => 'string'],
                    'locale' => ['type' => 'string'],
                    'localizedNames' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    'certificate' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'capabilities' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryUrls' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryProfileUri' => ['type' => 'string'],
                    'gatewayServerUri' => ['type' => 'string'],
                    'hostAddresses' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'siteId' => ['type' => 'string'],
                    'supervisorId' => ['type' => 'string'],
                    'notSeenSince' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'created' => ['$ref' => '#/definitions/RegistryOperationApiModel'],
                    'updated' => ['$ref' => '#/definitions/RegistryOperationApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ApplicationInfoListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/ApplicationInfoApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DiscoveryConfigApiModel' => [
                'properties' => [
                    'addressRangesToScan' => ['type' => 'string'],
                    'networkProbeTimeoutMs' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'maxNetworkProbes' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'portRangesToScan' => ['type' => 'string'],
                    'portProbeTimeoutMs' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'maxPortProbes' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'minPortProbesPercent' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'idleTimeBetweenScansSec' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'discoveryUrls' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'locales' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'callbacks' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/CallbackApiModel']
                    ],
                    'activationFilter' => ['$ref' => '#/definitions/EndpointActivationFilterApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DiscoveryRequestApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'discovery' => [
                        'type' => 'string',
                        'enum' => [
                            'Off',
                            'Local',
                            'Network',
                            'Fast',
                            'Scan'
                        ]
                    ],
                    'configuration' => ['$ref' => '#/definitions/DiscoveryConfigApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
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
            'EndpointApiModel' => [
                'properties' => [
                    'url' => ['type' => 'string'],
                    'alternativeUrls' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'user' => ['$ref' => '#/definitions/CredentialApiModel'],
                    'securityMode' => [
                        'type' => 'string',
                        'enum' => [
                            'Best',
                            'Sign',
                            'SignAndEncrypt',
                            'None'
                        ]
                    ],
                    'securityPolicy' => ['type' => 'string'],
                    'certificate' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['url']
            ],
            'AuthenticationMethodApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'credentialType' => [
                        'type' => 'string',
                        'enum' => [
                            'None',
                            'UserName',
                            'X509Certificate',
                            'JwtToken'
                        ]
                    ],
                    'securityPolicy' => ['type' => 'string'],
                    'configuration' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'EndpointRegistrationApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'endpointUrl' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
                    'endpoint' => ['$ref' => '#/definitions/EndpointApiModel'],
                    'securityLevel' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'authenticationMethods' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/AuthenticationMethodApiModel']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'id',
                    'endpoint'
                ]
            ],
            'ApplicationRegistrationApiModel' => [
                'properties' => [
                    'application' => ['$ref' => '#/definitions/ApplicationInfoApiModel'],
                    'endpoints' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/EndpointRegistrationApiModel']
                    ],
                    'securityAssessment' => [
                        'type' => 'string',
                        'enum' => [
                            'Unknown',
                            'Low',
                            'Medium',
                            'High'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['application']
            ],
            'ApplicationRegistrationUpdateApiModel' => [
                'properties' => [
                    'productUri' => ['type' => 'string'],
                    'applicationName' => ['type' => 'string'],
                    'locale' => ['type' => 'string'],
                    'localizedNames' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    'certificate' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'capabilities' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryUrls' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'discoveryProfileUri' => ['type' => 'string'],
                    'gatewayServerUri' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ApplicationSiteListApiModel' => [
                'properties' => [
                    'sites' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ApplicationRegistrationQueryApiModel' => [
                'properties' => [
                    'applicationType' => [
                        'type' => 'string',
                        'enum' => [
                            'Server',
                            'Client',
                            'ClientAndServer',
                            'DiscoveryServer'
                        ]
                    ],
                    'applicationUri' => ['type' => 'string'],
                    'productUri' => ['type' => 'string'],
                    'applicationName' => ['type' => 'string'],
                    'locale' => ['type' => 'string'],
                    'capability' => ['type' => 'string'],
                    'discoveryProfileUri' => ['type' => 'string'],
                    'gatewayServerUri' => ['type' => 'string'],
                    'siteOrSupervisorId' => ['type' => 'string'],
                    'includeNotSeenSince' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ApplicationRecordQueryApiModel' => [
                'properties' => [
                    'startingRecordId' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'maxRecordsToReturn' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'applicationName' => ['type' => 'string'],
                    'applicationUri' => ['type' => 'string'],
                    'applicationType' => [
                        'type' => 'string',
                        'enum' => [
                            'Server',
                            'Client',
                            'ClientAndServer',
                            'DiscoveryServer'
                        ]
                    ],
                    'productUri' => ['type' => 'string'],
                    'serverCapabilities' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'ApplicationRecordApiModel' => [
                'properties' => [
                    'recordId' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'application' => ['$ref' => '#/definitions/ApplicationInfoApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'recordId',
                    'application'
                ]
            ],
            'ApplicationRecordListApiModel' => [
                'properties' => [
                    'applications' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/ApplicationRecordApiModel']
                    ],
                    'lastCounterResetTime' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'nextRecordId' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'lastCounterResetTime',
                    'nextRecordId'
                ]
            ],
            'EndpointRegistrationUpdateApiModel' => [
                'properties' => ['user' => ['$ref' => '#/definitions/CredentialApiModel']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'EndpointInfoApiModel' => [
                'properties' => [
                    'registration' => ['$ref' => '#/definitions/EndpointRegistrationApiModel'],
                    'applicationId' => ['type' => 'string'],
                    'activationState' => [
                        'type' => 'string',
                        'enum' => [
                            'Deactivated',
                            'Activated',
                            'ActivatedAndConnected'
                        ]
                    ],
                    'endpointState' => [
                        'type' => 'string',
                        'enum' => [
                            'Connecting',
                            'NotReachable',
                            'Busy',
                            'NoTrust',
                            'CertificateInvalid',
                            'Ready',
                            'Error'
                        ]
                    ],
                    'outOfSync' => ['type' => 'boolean'],
                    'notSeenSince' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'registration',
                    'applicationId'
                ]
            ],
            'EndpointInfoListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/EndpointInfoApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'EndpointRegistrationQueryApiModel' => [
                'properties' => [
                    'url' => ['type' => 'string'],
                    'userAuthentication' => [
                        'type' => 'string',
                        'enum' => [
                            'None',
                            'UserName',
                            'X509Certificate',
                            'JwtToken'
                        ]
                    ],
                    'certificate' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'securityMode' => [
                        'type' => 'string',
                        'enum' => [
                            'Best',
                            'Sign',
                            'SignAndEncrypt',
                            'None'
                        ]
                    ],
                    'securityPolicy' => ['type' => 'string'],
                    'activated' => ['type' => 'boolean'],
                    'connected' => ['type' => 'boolean'],
                    'endpointState' => [
                        'type' => 'string',
                        'enum' => [
                            'Connecting',
                            'NotReachable',
                            'Busy',
                            'NoTrust',
                            'CertificateInvalid',
                            'Ready',
                            'Error'
                        ]
                    ],
                    'includeNotSeenSince' => ['type' => 'boolean']
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
            ],
            'SupervisorApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
                    'discovery' => [
                        'type' => 'string',
                        'enum' => [
                            'Off',
                            'Local',
                            'Network',
                            'Fast',
                            'Scan'
                        ]
                    ],
                    'discoveryConfig' => ['$ref' => '#/definitions/DiscoveryConfigApiModel'],
                    'certificate' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'logLevel' => [
                        'type' => 'string',
                        'enum' => [
                            'Error',
                            'Information',
                            'Debug',
                            'Verbose'
                        ]
                    ],
                    'outOfSync' => ['type' => 'boolean'],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'SupervisorUpdateApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
                    'discovery' => [
                        'type' => 'string',
                        'enum' => [
                            'Off',
                            'Local',
                            'Network',
                            'Fast',
                            'Scan'
                        ]
                    ],
                    'discoveryConfig' => ['$ref' => '#/definitions/DiscoveryConfigApiModel'],
                    'discoveryCallbacks' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/CallbackApiModel']
                    ],
                    'removeDiscoveryCallbacks' => ['type' => 'boolean'],
                    'logLevel' => [
                        'type' => 'string',
                        'enum' => [
                            'Error',
                            'Information',
                            'Debug',
                            'Verbose'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'EndpointActivationStatusApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'activationState' => [
                        'type' => 'string',
                        'enum' => [
                            'Deactivated',
                            'Activated',
                            'ActivatedAndConnected'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'SupervisorStatusApiModel' => [
                'properties' => [
                    'deviceId' => ['type' => 'string'],
                    'moduleId' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
                    'endpoints' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/EndpointActivationStatusApiModel']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['deviceId']
            ],
            'SupervisorListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/SupervisorApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'SupervisorQueryApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
                    'discovery' => [
                        'type' => 'string',
                        'enum' => [
                            'Off',
                            'Local',
                            'Network',
                            'Fast',
                            'Scan'
                        ]
                    ],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ]
        ]
    ];
}
