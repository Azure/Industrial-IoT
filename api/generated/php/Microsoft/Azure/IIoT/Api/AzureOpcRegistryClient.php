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
        $this->_RegisterServer_operation = $_client->createOperation('RegisterServer');
        $this->_CreateApplication_operation = $_client->createOperation('CreateApplication');
        $this->_DeleteAllDisabledApplications_operation = $_client->createOperation('DeleteAllDisabledApplications');
        $this->_GetListOfApplications_operation = $_client->createOperation('GetListOfApplications');
        $this->_DisableApplication_operation = $_client->createOperation('DisableApplication');
        $this->_EnableApplication_operation = $_client->createOperation('EnableApplication');
        $this->_DiscoverServer_operation = $_client->createOperation('DiscoverServer');
        $this->_Cancel_operation = $_client->createOperation('Cancel');
        $this->_GetApplicationRegistration_operation = $_client->createOperation('GetApplicationRegistration');
        $this->_UpdateApplicationRegistration_operation = $_client->createOperation('UpdateApplicationRegistration');
        $this->_DeleteApplication_operation = $_client->createOperation('DeleteApplication');
        $this->_GetListOfSites_operation = $_client->createOperation('GetListOfSites');
        $this->_QueryApplications_operation = $_client->createOperation('QueryApplications');
        $this->_GetFilteredListOfApplications_operation = $_client->createOperation('GetFilteredListOfApplications');
        $this->_Subscribe_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe_operation = $_client->createOperation('Unsubscribe');
        $this->_GetDiscoverer_operation = $_client->createOperation('GetDiscoverer');
        $this->_UpdateDiscoverer_operation = $_client->createOperation('UpdateDiscoverer');
        $this->_SetDiscoveryMode_operation = $_client->createOperation('SetDiscoveryMode');
        $this->_GetListOfDiscoverers_operation = $_client->createOperation('GetListOfDiscoverers');
        $this->_QueryDiscoverers_operation = $_client->createOperation('QueryDiscoverers');
        $this->_GetFilteredListOfDiscoverers_operation = $_client->createOperation('GetFilteredListOfDiscoverers');
        $this->_Subscribe1_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe1_operation = $_client->createOperation('Unsubscribe');
        $this->_SubscribeByDiscovererId_operation = $_client->createOperation('SubscribeByDiscovererId');
        $this->_SubscribeByRequestId_operation = $_client->createOperation('SubscribeByRequestId');
        $this->_UnsubscribeByRequestId_operation = $_client->createOperation('UnsubscribeByRequestId');
        $this->_UnsubscribeByDiscovererId_operation = $_client->createOperation('UnsubscribeByDiscovererId');
        $this->_ActivateEndpoint_operation = $_client->createOperation('ActivateEndpoint');
        $this->_GetEndpoint_operation = $_client->createOperation('GetEndpoint');
        $this->_GetListOfEndpoints_operation = $_client->createOperation('GetListOfEndpoints');
        $this->_QueryEndpoints_operation = $_client->createOperation('QueryEndpoints');
        $this->_GetFilteredListOfEndpoints_operation = $_client->createOperation('GetFilteredListOfEndpoints');
        $this->_DeactivateEndpoint_operation = $_client->createOperation('DeactivateEndpoint');
        $this->_Subscribe2_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe2_operation = $_client->createOperation('Unsubscribe');
        $this->_GetGateway_operation = $_client->createOperation('GetGateway');
        $this->_UpdateGateway_operation = $_client->createOperation('UpdateGateway');
        $this->_GetListOfGateway_operation = $_client->createOperation('GetListOfGateway');
        $this->_QueryGateway_operation = $_client->createOperation('QueryGateway');
        $this->_GetFilteredListOfGateway_operation = $_client->createOperation('GetFilteredListOfGateway');
        $this->_Subscribe3_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe3_operation = $_client->createOperation('Unsubscribe');
        $this->_GetPublisher_operation = $_client->createOperation('GetPublisher');
        $this->_UpdatePublisher_operation = $_client->createOperation('UpdatePublisher');
        $this->_GetListOfPublisher_operation = $_client->createOperation('GetListOfPublisher');
        $this->_QueryPublisher_operation = $_client->createOperation('QueryPublisher');
        $this->_GetFilteredListOfPublisher_operation = $_client->createOperation('GetFilteredListOfPublisher');
        $this->_Subscribe4_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe4_operation = $_client->createOperation('Unsubscribe');
        $this->_GetSupervisor_operation = $_client->createOperation('GetSupervisor');
        $this->_UpdateSupervisor_operation = $_client->createOperation('UpdateSupervisor');
        $this->_GetSupervisorStatus_operation = $_client->createOperation('GetSupervisorStatus');
        $this->_ResetSupervisor_operation = $_client->createOperation('ResetSupervisor');
        $this->_GetListOfSupervisors_operation = $_client->createOperation('GetListOfSupervisors');
        $this->_QuerySupervisors_operation = $_client->createOperation('QuerySupervisors');
        $this->_GetFilteredListOfSupervisors_operation = $_client->createOperation('GetFilteredListOfSupervisors');
        $this->_Subscribe5_operation = $_client->createOperation('Subscribe');
        $this->_Unsubscribe5_operation = $_client->createOperation('Unsubscribe');
    }
    /**
     * Registers a server solely using a discovery url. Requires that the onboarding agent service is running and the server can be located by a supervisor in its network using the discovery url.
     * @param array $body
     */
    public function registerServer(array $body)
    {
        return $this->_RegisterServer_operation->call(['body' => $body]);
    }
    /**
     * The application is registered using the provided information, but it is not associated with a supervisor. This is useful for when you need to register clients or you want to register a server that is located in a network not reachable through a Twin module.
     * @param array $body
     * @return array
     */
    public function createApplication(array $body)
    {
        return $this->_CreateApplication_operation->call(['body' => $body]);
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
     * Get all registered applications in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
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
     * Registers servers by running a discovery scan in a supervisor's network. Requires that the onboarding agent service is running.
     * @param array $body
     */
    public function discoverServer(array $body)
    {
        return $this->_DiscoverServer_operation->call(['body' => $body]);
    }
    /**
     * Cancels a discovery request using the request identifier.
     * @param string $requestId
     */
    public function cancel($requestId)
    {
        return $this->_Cancel_operation->call(['requestId' => $requestId]);
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
     * The application information is updated with new properties. Note that this information might be overridden if the application is re-discovered during a discovery run (recurring or one-time).
     * @param string $applicationId
     * @param array $body
     */
    public function updateApplicationRegistration(
        $applicationId,
        array $body
    )
    {
        return $this->_UpdateApplicationRegistration_operation->call([
            'applicationId' => $applicationId,
            'body' => $body
        ]);
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
     * List applications that match a query model. The returned model can contain a continuation token if more results are available. Call the GetListOfApplications operation using the token to retrieve more results.
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function queryApplications(
        $pageSize = null,
        array $body
    )
    {
        return $this->_QueryApplications_operation->call([
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of applications filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfApplications operation using the token to retrieve more results.
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function getFilteredListOfApplications(
        $pageSize = null,
        array $body
    )
    {
        return $this->_GetFilteredListOfApplications_operation->call([
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Register a client to receive application events through SignalR.
     * @param string|null $body
     */
    public function subscribe($body = null)
    {
        return $this->_Subscribe_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving events.
     * @param string $userId
     */
    public function unsubscribe($userId)
    {
        return $this->_Unsubscribe_operation->call(['userId' => $userId]);
    }
    /**
     * Returns a discoverer's registration and connectivity information. A discoverer id corresponds to the twin modules module identity.
     * @param string $discovererId
     * @param boolean|null $onlyServerState
     * @return array
     */
    public function getDiscoverer(
        $discovererId,
        $onlyServerState = null
    )
    {
        return $this->_GetDiscoverer_operation->call([
            'discovererId' => $discovererId,
            'onlyServerState' => $onlyServerState
        ]);
    }
    /**
     * Allows a caller to configure recurring discovery runs on the twin module identified by the discoverer id or update site information.
     * @param string $discovererId
     * @param array $body
     */
    public function updateDiscoverer(
        $discovererId,
        array $body
    )
    {
        return $this->_UpdateDiscoverer_operation->call([
            'discovererId' => $discovererId,
            'body' => $body
        ]);
    }
    /**
     * Allows a caller to configure recurring discovery runs on the discovery module identified by the module id.
     * @param string $discovererId
     * @param string $mode
     * @param array|null $body
     */
    public function setDiscoveryMode(
        $discovererId,
        $mode,
        array $body = null
    )
    {
        return $this->_SetDiscoveryMode_operation->call([
            'discovererId' => $discovererId,
            'mode' => $mode,
            'body' => $body
        ]);
    }
    /**
     * Get all registered discoverers and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfDiscoverers(
        $onlyServerState = null,
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfDiscoverers_operation->call([
            'onlyServerState' => $onlyServerState,
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get all discoverers that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function queryDiscoverers(
        $onlyServerState = null,
        $pageSize = null,
        array $body
    )
    {
        return $this->_QueryDiscoverers_operation->call([
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of discoverers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfDiscoverers operation using the token to retrieve more results.
     * @param string|null $siteId
     * @param string|null $discovery
     * @param boolean|null $connected
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfDiscoverers(
        $siteId = null,
        $discovery = null,
        $connected = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfDiscoverers_operation->call([
            'siteId' => $siteId,
            'discovery' => $discovery,
            'connected' => $connected,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Register a user to receive discoverer events through SignalR.
     * @param string|null $body
     */
    public function subscribe1($body = null)
    {
        return $this->_Subscribe1_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving discoverer events.
     * @param string $userId
     */
    public function unsubscribe1($userId)
    {
        return $this->_Unsubscribe1_operation->call(['userId' => $userId]);
    }
    /**
     * Register a client to receive discovery progress events through SignalR from a particular discoverer.
     * @param string $discovererId
     * @param string|null $body
     */
    public function subscribeByDiscovererId(
        $discovererId,
        $body = null
    )
    {
        return $this->_SubscribeByDiscovererId_operation->call([
            'discovererId' => $discovererId,
            'body' => $body
        ]);
    }
    /**
     * Register a client to receive discovery progress events through SignalR for a particular request.
     * @param string $requestId
     * @param string|null $body
     */
    public function subscribeByRequestId(
        $requestId,
        $body = null
    )
    {
        return $this->_SubscribeByRequestId_operation->call([
            'requestId' => $requestId,
            'body' => $body
        ]);
    }
    /**
     * Unregister a client and stop it from receiving discovery events for a particular request.
     * @param string $requestId
     * @param string $userId
     */
    public function unsubscribeByRequestId(
        $requestId,
        $userId
    )
    {
        return $this->_UnsubscribeByRequestId_operation->call([
            'requestId' => $requestId,
            'userId' => $userId
        ]);
    }
    /**
     * Unregister a client and stop it from receiving discovery events.
     * @param string $discovererId
     * @param string $userId
     */
    public function unsubscribeByDiscovererId(
        $discovererId,
        $userId
    )
    {
        return $this->_UnsubscribeByDiscovererId_operation->call([
            'discovererId' => $discovererId,
            'userId' => $userId
        ]);
    }
    /**
     * Activates an endpoint for subsequent use in twin service. All endpoints must be activated using this API or through a activation filter during application registration or discovery.
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
     * Get all registered endpoints in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
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
     * Return endpoints that match the specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfEndpoints operation using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function queryEndpoints(
        $onlyServerState = null,
        $pageSize = null,
        array $body
    )
    {
        return $this->_QueryEndpoints_operation->call([
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of endpoints filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfEndpoints operation using the token to retrieve more results.
     * @param string|null $url
     * @param string|null $certificate
     * @param string|null $securityMode
     * @param string|null $securityPolicy
     * @param boolean|null $activated
     * @param boolean|null $connected
     * @param string|null $endpointState
     * @param boolean|null $includeNotSeenSince
     * @param string|null $discovererId
     * @param string|null $applicationId
     * @param string|null $supervisorId
     * @param string|null $siteOrGatewayId
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfEndpoints(
        $url = null,
        $certificate = null,
        $securityMode = null,
        $securityPolicy = null,
        $activated = null,
        $connected = null,
        $endpointState = null,
        $includeNotSeenSince = null,
        $discovererId = null,
        $applicationId = null,
        $supervisorId = null,
        $siteOrGatewayId = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfEndpoints_operation->call([
            'url' => $url,
            'certificate' => $certificate,
            'securityMode' => $securityMode,
            'securityPolicy' => $securityPolicy,
            'activated' => $activated,
            'connected' => $connected,
            'endpointState' => $endpointState,
            'includeNotSeenSince' => $includeNotSeenSince,
            'discovererId' => $discovererId,
            'applicationId' => $applicationId,
            'supervisorId' => $supervisorId,
            'siteOrGatewayId' => $siteOrGatewayId,
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
     * Register a user to receive endpoint events through SignalR.
     * @param string|null $body
     */
    public function subscribe2($body = null)
    {
        return $this->_Subscribe2_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving endpoint events.
     * @param string $userId
     */
    public function unsubscribe2($userId)
    {
        return $this->_Unsubscribe2_operation->call(['userId' => $userId]);
    }
    /**
     * Returns a Gateway's registration and connectivity information. A Gateway id corresponds to the twin modules module identity.
     * @param string $gatewayId
     * @return array
     */
    public function getGateway($gatewayId)
    {
        return $this->_GetGateway_operation->call(['GatewayId' => $gatewayId]);
    }
    /**
     * Allows a caller to configure operations on the Gateway module identified by the Gateway id.
     * @param string $gatewayId
     * @param array $body
     */
    public function updateGateway(
        $gatewayId,
        array $body
    )
    {
        return $this->_UpdateGateway_operation->call([
            'GatewayId' => $gatewayId,
            'body' => $body
        ]);
    }
    /**
     * Get all registered Gateways and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfGateway(
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfGateway_operation->call([
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get all Gateways that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfGateway operation using the token to retrieve more results.
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function queryGateway(
        $pageSize = null,
        array $body
    )
    {
        return $this->_QueryGateway_operation->call([
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of Gateways filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfGateway operation using the token to retrieve more results.
     * @param string|null $siteId
     * @param boolean|null $connected
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfGateway(
        $siteId = null,
        $connected = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfGateway_operation->call([
            'siteId' => $siteId,
            'connected' => $connected,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Register a user to receive Gateway events through SignalR.
     * @param string|null $body
     */
    public function subscribe3($body = null)
    {
        return $this->_Subscribe3_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving Gateway events.
     * @param string $userId
     */
    public function unsubscribe3($userId)
    {
        return $this->_Unsubscribe3_operation->call(['userId' => $userId]);
    }
    /**
     * Returns a publisher's registration and connectivity information. A publisher id corresponds to the twin modules module identity.
     * @param string $publisherId
     * @param boolean|null $onlyServerState
     * @return array
     */
    public function getPublisher(
        $publisherId,
        $onlyServerState = null
    )
    {
        return $this->_GetPublisher_operation->call([
            'publisherId' => $publisherId,
            'onlyServerState' => $onlyServerState
        ]);
    }
    /**
     * Allows a caller to configure operations on the publisher module identified by the publisher id.
     * @param string $publisherId
     * @param array $body
     */
    public function updatePublisher(
        $publisherId,
        array $body
    )
    {
        return $this->_UpdatePublisher_operation->call([
            'publisherId' => $publisherId,
            'body' => $body
        ]);
    }
    /**
     * Get all registered publishers and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param string|null $continuationToken
     * @param integer|null $pageSize
     * @return array
     */
    public function getListOfPublisher(
        $onlyServerState = null,
        $continuationToken = null,
        $pageSize = null
    )
    {
        return $this->_GetListOfPublisher_operation->call([
            'onlyServerState' => $onlyServerState,
            'continuationToken' => $continuationToken,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get all publishers that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfPublisher operation using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function queryPublisher(
        $onlyServerState = null,
        $pageSize = null,
        array $body
    )
    {
        return $this->_QueryPublisher_operation->call([
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of publishers filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfPublisher operation using the token to retrieve more results.
     * @param string|null $siteId
     * @param boolean|null $connected
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfPublisher(
        $siteId = null,
        $connected = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfPublisher_operation->call([
            'siteId' => $siteId,
            'connected' => $connected,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Register a user to receive publisher events through SignalR.
     * @param string|null $body
     */
    public function subscribe4($body = null)
    {
        return $this->_Subscribe4_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving publisher events.
     * @param string $userId
     */
    public function unsubscribe4($userId)
    {
        return $this->_Unsubscribe4_operation->call(['userId' => $userId]);
    }
    /**
     * Returns a supervisor's registration and connectivity information. A supervisor id corresponds to the twin modules module identity.
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
     * Allows a caller to configure recurring discovery runs on the twin module identified by the supervisor id or update site information.
     * @param string $supervisorId
     * @param array $body
     */
    public function updateSupervisor(
        $supervisorId,
        array $body
    )
    {
        return $this->_UpdateSupervisor_operation->call([
            'supervisorId' => $supervisorId,
            'body' => $body
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
     * Allows a caller to reset the twin module using its supervisor identity identifier.
     * @param string $supervisorId
     */
    public function resetSupervisor($supervisorId)
    {
        return $this->_ResetSupervisor_operation->call(['supervisorId' => $supervisorId]);
    }
    /**
     * Get all registered supervisors and therefore twin modules in paged form. The returned model can contain a continuation token if more results are available. Call this operation again using the token to retrieve more results.
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
     * Get all supervisors that match a specified query. The returned model can contain a continuation token if more results are available. Call the GetListOfSupervisors operation using the token to retrieve more results.
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @param array $body
     * @return array
     */
    public function querySupervisors(
        $onlyServerState = null,
        $pageSize = null,
        array $body
    )
    {
        return $this->_QuerySupervisors_operation->call([
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize,
            'body' => $body
        ]);
    }
    /**
     * Get a list of supervisors filtered using the specified query parameters. The returned model can contain a continuation token if more results are available. Call the GetListOfSupervisors operation using the token to retrieve more results.
     * @param string|null $siteId
     * @param boolean|null $connected
     * @param boolean|null $onlyServerState
     * @param integer|null $pageSize
     * @return array
     */
    public function getFilteredListOfSupervisors(
        $siteId = null,
        $connected = null,
        $onlyServerState = null,
        $pageSize = null
    )
    {
        return $this->_GetFilteredListOfSupervisors_operation->call([
            'siteId' => $siteId,
            'connected' => $connected,
            'onlyServerState' => $onlyServerState,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Register a user to receive supervisor events through SignalR.
     * @param string|null $body
     */
    public function subscribe5($body = null)
    {
        return $this->_Subscribe5_operation->call(['body' => $body]);
    }
    /**
     * Unregister a user and stop it from receiving supervisor events.
     * @param string $userId
     */
    public function unsubscribe5($userId)
    {
        return $this->_Unsubscribe5_operation->call(['userId' => $userId]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_RegisterServer_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_CreateApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteAllDisabledApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfApplications_operation;
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
    private $_Cancel_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetApplicationRegistration_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateApplicationRegistration_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteApplication_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfSites_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryApplications_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfApplications_operation;
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
    private $_GetDiscoverer_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateDiscoverer_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_SetDiscoveryMode_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfDiscoverers_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryDiscoverers_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfDiscoverers_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe1_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe1_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_SubscribeByDiscovererId_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_SubscribeByRequestId_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UnsubscribeByRequestId_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UnsubscribeByDiscovererId_operation;
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
    private $_GetListOfEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfEndpoints_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeactivateEndpoint_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe2_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe2_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetGateway_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateGateway_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfGateway_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryGateway_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfGateway_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe3_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe3_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetPublisher_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdatePublisher_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetListOfPublisher_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryPublisher_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfPublisher_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe4_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe4_operation;
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
    private $_QuerySupervisors_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetFilteredListOfSupervisors_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Subscribe5_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_Unsubscribe5_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/applications' => [
                'post' => [
                    'operationId' => 'RegisterServer',
                    'parameters' => [[
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ServerRegistrationRequestApiModel']
                    ]],
                    'responses' => ['200' => []]
                ],
                'put' => [
                    'operationId' => 'CreateApplication',
                    'parameters' => [[
                        'name' => 'body',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/ApplicationRegistrationRequestApiModel']
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationRegistrationResponseApiModel']]]
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
                ],
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
                    'name' => 'body',
                    'in' => 'body',
                    'required' => TRUE,
                    'schema' => ['$ref' => '#/definitions/DiscoveryRequestApiModel']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/applications/discover/{requestId}' => ['delete' => [
                'operationId' => 'Cancel',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
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
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
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
                'post' => [
                    'operationId' => 'QueryApplications',
                    'parameters' => [
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationInfoListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfApplications',
                    'parameters' => [
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/ApplicationRegistrationQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/ApplicationInfoListApiModel']]]
                ]
            ],
            '/v2/applications/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/applications/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/discovery/{discovererId}' => [
                'get' => [
                    'operationId' => 'GetDiscoverer',
                    'parameters' => [
                        [
                            'name' => 'discovererId',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/DiscovererApiModel']]]
                ],
                'patch' => [
                    'operationId' => 'UpdateDiscoverer',
                    'parameters' => [
                        [
                            'name' => 'discovererId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/DiscovererUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ],
                'post' => [
                    'operationId' => 'SetDiscoveryMode',
                    'parameters' => [
                        [
                            'name' => 'discovererId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'mode',
                            'in' => 'query',
                            'required' => TRUE,
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
                            'name' => 'body',
                            'in' => 'body',
                            'required' => FALSE,
                            'schema' => ['$ref' => '#/definitions/DiscoveryConfigApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/discovery' => ['get' => [
                'operationId' => 'GetListOfDiscoverers',
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
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/DiscovererListApiModel']]]
            ]],
            '/v2/discovery/query' => [
                'post' => [
                    'operationId' => 'QueryDiscoverers',
                    'parameters' => [
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
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/DiscovererQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/DiscovererListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfDiscoverers',
                    'parameters' => [
                        [
                            'name' => 'siteId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'discovery',
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
                            'name' => 'connected',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/DiscovererListApiModel']]]
                ]
            ],
            '/v2/discovery/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/discovery/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/discovery/{discovererId}/events' => ['put' => [
                'operationId' => 'SubscribeByDiscovererId',
                'parameters' => [
                    [
                        'name' => 'discovererId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => FALSE,
                        'schema' => ['type' => 'string']
                    ]
                ],
                'responses' => ['200' => []]
            ]],
            '/v2/discovery/requests/{requestId}/events' => ['put' => [
                'operationId' => 'SubscribeByRequestId',
                'parameters' => [
                    [
                        'name' => 'requestId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'body',
                        'in' => 'body',
                        'required' => FALSE,
                        'schema' => ['type' => 'string']
                    ]
                ],
                'responses' => ['200' => []]
            ]],
            '/v2/discovery/requests/{requestId}/events/{userId}' => ['delete' => [
                'operationId' => 'UnsubscribeByRequestId',
                'parameters' => [
                    [
                        'name' => 'requestId',
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
            '/v2/discovery/{discovererId}/events/{userId}' => ['delete' => [
                'operationId' => 'UnsubscribeByDiscovererId',
                'parameters' => [
                    [
                        'name' => 'discovererId',
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
            '/v2/endpoints/{endpointId}' => ['get' => [
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
            ]],
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
                'post' => [
                    'operationId' => 'QueryEndpoints',
                    'parameters' => [
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
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/EndpointRegistrationQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/EndpointInfoListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfEndpoints',
                    'parameters' => [
                        [
                            'name' => 'url',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'certificate',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string',
                            'format' => 'byte'
                        ],
                        [
                            'name' => 'securityMode',
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
                            'name' => 'securityPolicy',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'activated',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'connected',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'endpointState',
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
                            'name' => 'includeNotSeenSince',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'boolean'
                        ],
                        [
                            'name' => 'discovererId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'applicationId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'supervisorId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'siteOrGatewayId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
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
            '/v2/endpoints/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/endpoints/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/gateways/{GatewayId}' => [
                'get' => [
                    'operationId' => 'GetGateway',
                    'parameters' => [[
                        'name' => 'GatewayId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/GatewayInfoApiModel']]]
                ],
                'patch' => [
                    'operationId' => 'UpdateGateway',
                    'parameters' => [
                        [
                            'name' => 'GatewayId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/GatewayUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/gateways' => ['get' => [
                'operationId' => 'GetListOfGateway',
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
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/GatewayListApiModel']]]
            ]],
            '/v2/gateways/query' => [
                'post' => [
                    'operationId' => 'QueryGateway',
                    'parameters' => [
                        [
                            'name' => 'pageSize',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'integer',
                            'format' => 'int32'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/GatewayQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/GatewayListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfGateway',
                    'parameters' => [
                        [
                            'name' => 'siteId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'connected',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/GatewayListApiModel']]]
                ]
            ],
            '/v2/gateways/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/gateways/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/publishers/{publisherId}' => [
                'get' => [
                    'operationId' => 'GetPublisher',
                    'parameters' => [
                        [
                            'name' => 'publisherId',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublisherApiModel']]]
                ],
                'patch' => [
                    'operationId' => 'UpdatePublisher',
                    'parameters' => [
                        [
                            'name' => 'publisherId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/PublisherUpdateApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/publishers' => ['get' => [
                'operationId' => 'GetListOfPublisher',
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
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublisherListApiModel']]]
            ]],
            '/v2/publishers/query' => [
                'post' => [
                    'operationId' => 'QueryPublisher',
                    'parameters' => [
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
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/PublisherQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublisherListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfPublisher',
                    'parameters' => [
                        [
                            'name' => 'siteId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'connected',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/PublisherListApiModel']]]
                ]
            ],
            '/v2/publishers/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/publishers/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
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
                            'name' => 'body',
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
                'post' => [
                    'operationId' => 'QuerySupervisors',
                    'parameters' => [
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
                        ],
                        [
                            'name' => 'body',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/SupervisorQueryApiModel']
                        ]
                    ],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/SupervisorListApiModel']]]
                ],
                'get' => [
                    'operationId' => 'GetFilteredListOfSupervisors',
                    'parameters' => [
                        [
                            'name' => 'siteId',
                            'in' => 'query',
                            'required' => FALSE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'connected',
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
                ]
            ],
            '/v2/supervisors/events' => ['put' => [
                'operationId' => 'Subscribe',
                'parameters' => [[
                    'name' => 'body',
                    'in' => 'body',
                    'required' => FALSE,
                    'schema' => ['type' => 'string']
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/supervisors/events/{userId}' => ['delete' => [
                'operationId' => 'Unsubscribe',
                'parameters' => [[
                    'name' => 'userId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]]
        ],
        'definitions' => [
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
                    'discovererId' => ['type' => 'string'],
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
            'EndpointApiModel' => [
                'properties' => [
                    'url' => ['type' => 'string'],
                    'alternativeUrls' => [
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
                    'supervisorId' => ['type' => 'string'],
                    'discovererId' => ['type' => 'string'],
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
                    'siteOrGatewayId' => ['type' => 'string'],
                    'includeNotSeenSince' => ['type' => 'boolean'],
                    'discovererId' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DiscovererApiModel' => [
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
            'DiscovererUpdateApiModel' => [
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
            'DiscovererListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/DiscovererApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'DiscovererQueryApiModel' => [
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
                    'includeNotSeenSince' => ['type' => 'boolean'],
                    'discovererId' => ['type' => 'string'],
                    'applicationId' => ['type' => 'string'],
                    'supervisorId' => ['type' => 'string'],
                    'siteOrGatewayId' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'GatewayApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'SupervisorApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
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
            'PublisherConfigApiModel' => [
                'properties' => [
                    'capabilities' => [
                        'type' => 'object',
                        'additionalProperties' => ['type' => 'string']
                    ],
                    'jobCheckInterval' => ['type' => 'string'],
                    'heartbeatInterval' => ['type' => 'string'],
                    'maxWorkers' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'jobOrchestratorUrl' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublisherApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'siteId' => ['type' => 'string'],
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
                    'configuration' => ['$ref' => '#/definitions/PublisherConfigApiModel'],
                    'outOfSync' => ['type' => 'boolean'],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'GatewayModulesApiModel' => [
                'properties' => [
                    'supervisor' => ['$ref' => '#/definitions/SupervisorApiModel'],
                    'publisher' => ['$ref' => '#/definitions/PublisherApiModel'],
                    'discoverer' => ['$ref' => '#/definitions/DiscovererApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'GatewayInfoApiModel' => [
                'properties' => [
                    'gateway' => ['$ref' => '#/definitions/GatewayApiModel'],
                    'modules' => ['$ref' => '#/definitions/GatewayModulesApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => ['gateway']
            ],
            'GatewayUpdateApiModel' => [
                'properties' => ['siteId' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'GatewayListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/GatewayApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'GatewayQueryApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublisherUpdateApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
                    'configuration' => ['$ref' => '#/definitions/PublisherConfigApiModel'],
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
            'PublisherListApiModel' => [
                'properties' => [
                    'items' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/PublisherApiModel']
                    ],
                    'continuationToken' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'PublisherQueryApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'SupervisorUpdateApiModel' => [
                'properties' => [
                    'siteId' => ['type' => 'string'],
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
                    'connected' => ['type' => 'boolean']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ]
        ]
    ];
}
