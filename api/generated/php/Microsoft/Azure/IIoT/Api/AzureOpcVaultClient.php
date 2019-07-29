<?php
namespace Microsoft\Azure\IIoT\Api;
final class AzureOpcVaultClient
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
        $this->_GetIssuerCertificateChain_operation = $_client->createOperation('GetIssuerCertificateChain');
        $this->_GetIssuerCrlChain_operation = $_client->createOperation('GetIssuerCrlChain');
        $this->_GetIssuerCertificateChain1_operation = $_client->createOperation('GetIssuerCertificateChain');
        $this->_GetIssuerCrlChain1_operation = $_client->createOperation('GetIssuerCrlChain');
        $this->_StartSigningRequest_operation = $_client->createOperation('StartSigningRequest');
        $this->_FinishSigningRequest_operation = $_client->createOperation('FinishSigningRequest');
        $this->_StartNewKeyPairRequest_operation = $_client->createOperation('StartNewKeyPairRequest');
        $this->_FinishNewKeyPairRequest_operation = $_client->createOperation('FinishNewKeyPairRequest');
        $this->_ApproveRequest_operation = $_client->createOperation('ApproveRequest');
        $this->_RejectRequest_operation = $_client->createOperation('RejectRequest');
        $this->_AcceptRequest_operation = $_client->createOperation('AcceptRequest');
        $this->_GetRequest_operation = $_client->createOperation('GetRequest');
        $this->_DeleteRequest_operation = $_client->createOperation('DeleteRequest');
        $this->_QueryRequests_operation = $_client->createOperation('QueryRequests');
        $this->_ListRequests_operation = $_client->createOperation('ListRequests');
        $this->_GetStatus_operation = $_client->createOperation('GetStatus');
        $this->_ListGroups_operation = $_client->createOperation('ListGroups');
        $this->_CreateGroup_operation = $_client->createOperation('CreateGroup');
        $this->_GetGroup_operation = $_client->createOperation('GetGroup');
        $this->_UpdateGroup_operation = $_client->createOperation('UpdateGroup');
        $this->_DeleteGroup_operation = $_client->createOperation('DeleteGroup');
        $this->_CreateRoot_operation = $_client->createOperation('CreateRoot');
        $this->_RenewIssuerCertificate_operation = $_client->createOperation('RenewIssuerCertificate');
        $this->_AddTrustRelationship_operation = $_client->createOperation('AddTrustRelationship');
        $this->_ListTrustedCertificates_operation = $_client->createOperation('ListTrustedCertificates');
        $this->_RemoveTrustRelationship_operation = $_client->createOperation('RemoveTrustRelationship');
    }
    /**
     * @param string $serialNumber
     * @return array
     */
    public function getIssuerCertificateChain($serialNumber)
    {
        return $this->_GetIssuerCertificateChain_operation->call(['serialNumber' => $serialNumber]);
    }
    /**
     * @param string $serialNumber
     * @return array
     */
    public function getIssuerCrlChain($serialNumber)
    {
        return $this->_GetIssuerCrlChain_operation->call(['serialNumber' => $serialNumber]);
    }
    /**
     * @param string $serialNumber
     */
    public function getIssuerCertificateChain1($serialNumber)
    {
        return $this->_GetIssuerCertificateChain1_operation->call(['serialNumber' => $serialNumber]);
    }
    /**
     * @param string $serialNumber
     */
    public function getIssuerCrlChain1($serialNumber)
    {
        return $this->_GetIssuerCrlChain1_operation->call(['serialNumber' => $serialNumber]);
    }
    /**
     * The request is in the 'New' state after this call.
Requires Writer or Manager role.
     * @param array $signingRequest
     * @return array
     */
    public function startSigningRequest(array $signingRequest)
    {
        return $this->_StartSigningRequest_operation->call(['signingRequest' => $signingRequest]);
    }
    /**
     * Can be called in any state.
After a successful fetch in 'Completed' state, the request is
moved into 'Accepted' state.
Requires Writer role.
     * @param string $requestId
     * @return array
     */
    public function finishSigningRequest($requestId)
    {
        return $this->_FinishSigningRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * The request is in the 'New' state after this call.
Requires Writer or Manager role.
     * @param array $newKeyPairRequest
     * @return array
     */
    public function startNewKeyPairRequest(array $newKeyPairRequest)
    {
        return $this->_StartNewKeyPairRequest_operation->call(['newKeyPairRequest' => $newKeyPairRequest]);
    }
    /**
     * Can be called in any state.
Fetches private key in 'Completed' state.
After a successful fetch in 'Completed' state, the request is
moved into 'Accepted' state.
Requires Writer role.
     * @param string $requestId
     * @return array
     */
    public function finishNewKeyPairRequest($requestId)
    {
        return $this->_FinishNewKeyPairRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     *  Validates the request with the application database.
- If Approved:
  - New Key Pair request: Creates the new key pair
        in the requested format, signs the certificate and stores the
        private key for later securely in KeyVault.
  - Cert Signing Request: Creates and signs the certificate.
        Deletes the CSR from the database.
 Stores the signed certificate for later use in the Database.
 The request is in the 'Approved' or 'Rejected' state after this call.
 Requires Approver role.
 Approver needs signing rights in KeyVault.
     * @param string $requestId
     */
    public function approveRequest($requestId)
    {
        return $this->_ApproveRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * The request is in the 'Rejected' state after this call.
Requires Approver role.
Approver needs signing rights in KeyVault.
     * @param string $requestId
     */
    public function rejectRequest($requestId)
    {
        return $this->_RejectRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * The request is in the 'Accepted' state after this call.
Requires Writer role.
     * @param string $requestId
     */
    public function acceptRequest($requestId)
    {
        return $this->_AcceptRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * @param string $requestId
     * @return array
     */
    public function getRequest($requestId)
    {
        return $this->_GetRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * By purging the request it is actually physically deleted from the
database, including the public key and other information.
Requires Manager role.
     * @param string $requestId
     */
    public function deleteRequest($requestId)
    {
        return $this->_DeleteRequest_operation->call(['requestId' => $requestId]);
    }
    /**
     * Get all certificate requests in paged form.
The returned model can contain a link to the next page if more results are
available.  Use ListRequests to continue.
     * @param array|null $query
     * @param integer|null $pageSize
     * @return array
     */
    public function queryRequests(
        array $query = null,
        $pageSize = null
    )
    {
        return $this->_QueryRequests_operation->call([
            'query' => $query,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Get all certificate requests in paged form or continue a current listing or
query.
The returned model can contain a link to the next page if more results are
available.
     * @param string|null $nextPageLink
     * @param integer|null $pageSize
     * @return array
     */
    public function listRequests(
        $nextPageLink = null,
        $pageSize = null
    )
    {
        return $this->_ListRequests_operation->call([
            'nextPageLink' => $nextPageLink,
            'pageSize' => $pageSize
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
     * A trust group has a root certificate which issues certificates
to entities.  Entities can be part of a trust group and thus
trust the root certificate and all entities that the root has
issued certificates for.
     * @param string|null $nextPageLink
     * @param integer|null $pageSize
     * @return array
     */
    public function listGroups(
        $nextPageLink = null,
        $pageSize = null
    )
    {
        return $this->_ListGroups_operation->call([
            'nextPageLink' => $nextPageLink,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Requires manager role.
     * @param array $request
     * @return array
     */
    public function createGroup(array $request)
    {
        return $this->_CreateGroup_operation->call(['request' => $request]);
    }
    /**
     * A trust group has a root certificate which issues certificates
to entities.  Entities can be part of a trust group and thus
trust the root certificate and all entities that the root has
issued certificates for.
     * @param string $groupId
     * @return array
     */
    public function getGroup($groupId)
    {
        return $this->_GetGroup_operation->call(['groupId' => $groupId]);
    }
    /**
     * Use this function with care and only if you are aware of
the security implications.
Requires manager role.
     * @param string $groupId
     * @param array $request
     */
    public function updateGroup(
        $groupId,
        array $request
    )
    {
        return $this->_UpdateGroup_operation->call([
            'groupId' => $groupId,
            'request' => $request
        ]);
    }
    /**
     * After this operation the Issuer CA, CRLs and keys become inaccessible.
Use this function with extreme caution.
Requires manager role.
     * @param string $groupId
     */
    public function deleteGroup($groupId)
    {
        return $this->_DeleteGroup_operation->call(['groupId' => $groupId]);
    }
    /**
     * Requires manager role.
     * @param array $request
     * @return array
     */
    public function createRoot(array $request)
    {
        return $this->_CreateRoot_operation->call(['request' => $request]);
    }
    /**
     * @param string $groupId
     */
    public function renewIssuerCertificate($groupId)
    {
        return $this->_RenewIssuerCertificate_operation->call(['groupId' => $groupId]);
    }
    /**
     * Define trust between two entities.  The entities are identifiers
of application, groups, or endpoints.
     * @param string $entityId
     * @param string $trustedEntityId
     */
    public function addTrustRelationship(
        $entityId,
        $trustedEntityId
    )
    {
        return $this->_AddTrustRelationship_operation->call([
            'entityId' => $entityId,
            'trustedEntityId' => $trustedEntityId
        ]);
    }
    /**
     * Returns all certificates the entity should trust based on the
applied trust configuration.
     * @param string $entityId
     * @param string|null $nextPageLink
     * @param integer|null $pageSize
     * @return array
     */
    public function listTrustedCertificates(
        $entityId,
        $nextPageLink = null,
        $pageSize = null
    )
    {
        return $this->_ListTrustedCertificates_operation->call([
            'entityId' => $entityId,
            'nextPageLink' => $nextPageLink,
            'pageSize' => $pageSize
        ]);
    }
    /**
     * Removes trust between two entities.  The entities are identifiers
of application, groups, or endpoints.
     * @param string $entityId
     * @param string $untrustedEntityId
     */
    public function removeTrustRelationship(
        $entityId,
        $untrustedEntityId
    )
    {
        return $this->_RemoveTrustRelationship_operation->call([
            'entityId' => $entityId,
            'untrustedEntityId' => $untrustedEntityId
        ]);
    }
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetIssuerCertificateChain_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetIssuerCrlChain_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetIssuerCertificateChain1_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetIssuerCrlChain1_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_StartSigningRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_FinishSigningRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_StartNewKeyPairRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_FinishNewKeyPairRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ApproveRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_RejectRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_AcceptRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteRequest_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_QueryRequests_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ListRequests_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetStatus_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ListGroups_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_CreateGroup_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_GetGroup_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_UpdateGroup_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_DeleteGroup_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_CreateRoot_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_RenewIssuerCertificate_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_AddTrustRelationship_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_ListTrustedCertificates_operation;
    /**
     * @var \Microsoft\Rest\OperationInterface
     */
    private $_RemoveTrustRelationship_operation;
    const _SWAGGER_OBJECT_DATA = [
        'host' => 'localhost',
        'paths' => [
            '/v2/certificates/{serialNumber}' => ['get' => [
                'operationId' => 'GetIssuerCertificateChain',
                'parameters' => [[
                    'name' => 'serialNumber',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/X509CertificateChainApiModel']]]
            ]],
            '/v2/certificates/{serialNumber}/crl' => ['get' => [
                'operationId' => 'GetIssuerCrlChain',
                'parameters' => [[
                    'name' => 'serialNumber',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/X509CrlChainApiModel']]]
            ]],
            '/v2/issuer/{serialNumber}' => ['get' => [
                'operationId' => 'GetIssuerCertificateChain',
                'parameters' => [[
                    'name' => 'serialNumber',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/crl/{serialNumber}' => ['get' => [
                'operationId' => 'GetIssuerCrlChain',
                'parameters' => [[
                    'name' => 'serialNumber',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/requests/sign' => ['put' => [
                'operationId' => 'StartSigningRequest',
                'parameters' => [[
                    'name' => 'signingRequest',
                    'in' => 'body',
                    'required' => TRUE,
                    'schema' => ['$ref' => '#/definitions/StartSigningRequestApiModel']
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StartSigningRequestResponseApiModel']]]
            ]],
            '/v2/requests/sign/{requestId}' => ['get' => [
                'operationId' => 'FinishSigningRequest',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/FinishSigningRequestResponseApiModel']]]
            ]],
            '/v2/requests/keypair' => ['put' => [
                'operationId' => 'StartNewKeyPairRequest',
                'parameters' => [[
                    'name' => 'newKeyPairRequest',
                    'in' => 'body',
                    'required' => TRUE,
                    'schema' => ['$ref' => '#/definitions/StartNewKeyPairRequestApiModel']
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StartNewKeyPairRequestResponseApiModel']]]
            ]],
            '/v2/requests/keypair/{requestId}' => ['get' => [
                'operationId' => 'FinishNewKeyPairRequest',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/FinishNewKeyPairRequestResponseApiModel']]]
            ]],
            '/v2/requests/{requestId}/approve' => ['post' => [
                'operationId' => 'ApproveRequest',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/requests/{requestId}/reject' => ['post' => [
                'operationId' => 'RejectRequest',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/requests/{requestId}/accept' => ['post' => [
                'operationId' => 'AcceptRequest',
                'parameters' => [[
                    'name' => 'requestId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/requests/{requestId}' => [
                'get' => [
                    'operationId' => 'GetRequest',
                    'parameters' => [[
                        'name' => 'requestId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/CertificateRequestRecordApiModel']]]
                ],
                'delete' => [
                    'operationId' => 'DeleteRequest',
                    'parameters' => [[
                        'name' => 'requestId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/requests/query' => ['post' => [
                'operationId' => 'QueryRequests',
                'parameters' => [
                    [
                        'name' => 'query',
                        'in' => 'body',
                        'required' => FALSE,
                        'schema' => ['$ref' => '#/definitions/CertificateRequestQueryRequestApiModel']
                    ],
                    [
                        'name' => 'pageSize',
                        'in' => 'query',
                        'required' => FALSE,
                        'type' => 'integer',
                        'format' => 'int32'
                    ]
                ],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/CertificateRequestQueryResponseApiModel']]]
            ]],
            '/v2/requests' => ['get' => [
                'operationId' => 'ListRequests',
                'parameters' => [
                    [
                        'name' => 'nextPageLink',
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
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/CertificateRequestQueryResponseApiModel']]]
            ]],
            '/v2/status' => ['get' => [
                'operationId' => 'GetStatus',
                'parameters' => [],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/StatusResponseApiModel']]]
            ]],
            '/v2/groups' => [
                'get' => [
                    'operationId' => 'ListGroups',
                    'parameters' => [
                        [
                            'name' => 'nextPageLink',
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
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/TrustGroupRegistrationListApiModel']]]
                ],
                'put' => [
                    'operationId' => 'CreateGroup',
                    'parameters' => [[
                        'name' => 'request',
                        'in' => 'body',
                        'required' => TRUE,
                        'schema' => ['$ref' => '#/definitions/TrustGroupRegistrationRequestApiModel']
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/TrustGroupRegistrationResponseApiModel']]]
                ]
            ],
            '/v2/groups/{groupId}' => [
                'get' => [
                    'operationId' => 'GetGroup',
                    'parameters' => [[
                        'name' => 'groupId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/TrustGroupRegistrationApiModel']]]
                ],
                'post' => [
                    'operationId' => 'UpdateGroup',
                    'parameters' => [
                        [
                            'name' => 'groupId',
                            'in' => 'path',
                            'required' => TRUE,
                            'type' => 'string'
                        ],
                        [
                            'name' => 'request',
                            'in' => 'body',
                            'required' => TRUE,
                            'schema' => ['$ref' => '#/definitions/TrustGroupUpdateRequestApiModel']
                        ]
                    ],
                    'responses' => ['200' => []]
                ],
                'delete' => [
                    'operationId' => 'DeleteGroup',
                    'parameters' => [[
                        'name' => 'groupId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]],
                    'responses' => ['200' => []]
                ]
            ],
            '/v2/groups/root' => ['put' => [
                'operationId' => 'CreateRoot',
                'parameters' => [[
                    'name' => 'request',
                    'in' => 'body',
                    'required' => TRUE,
                    'schema' => ['$ref' => '#/definitions/TrustGroupRootCreateRequestApiModel']
                ]],
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/TrustGroupRegistrationResponseApiModel']]]
            ]],
            '/v2/groups/{groupId}/renew' => ['post' => [
                'operationId' => 'RenewIssuerCertificate',
                'parameters' => [[
                    'name' => 'groupId',
                    'in' => 'path',
                    'required' => TRUE,
                    'type' => 'string'
                ]],
                'responses' => ['200' => []]
            ]],
            '/v2/trustlists/{entityId}/{trustedEntityId}' => ['put' => [
                'operationId' => 'AddTrustRelationship',
                'parameters' => [
                    [
                        'name' => 'entityId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'trustedEntityId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]
                ],
                'responses' => ['200' => []]
            ]],
            '/v2/trustlists/{entityId}' => ['get' => [
                'operationId' => 'ListTrustedCertificates',
                'parameters' => [
                    [
                        'name' => 'entityId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'nextPageLink',
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
                'responses' => ['200' => ['schema' => ['$ref' => '#/definitions/X509CertificateListApiModel']]]
            ]],
            '/v2/trustlists/{entityId}/{untrustedEntityId}' => ['delete' => [
                'operationId' => 'RemoveTrustRelationship',
                'parameters' => [
                    [
                        'name' => 'entityId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ],
                    [
                        'name' => 'untrustedEntityId',
                        'in' => 'path',
                        'required' => TRUE,
                        'type' => 'string'
                    ]
                ],
                'responses' => ['200' => []]
            ]]
        ],
        'definitions' => [
            'X509CertificateApiModel' => [
                'properties' => [
                    'subject' => ['type' => 'string'],
                    'thumbprint' => ['type' => 'string'],
                    'serialNumber' => ['type' => 'string'],
                    'notBeforeUtc' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'notAfterUtc' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ],
                    'certificate' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => ['certificate']
            ],
            'X509CertificateChainApiModel' => [
                'properties' => ['chain' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/X509CertificateApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'X509CrlApiModel' => [
                'properties' => [
                    'issuer' => ['type' => 'string'],
                    'crl' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => ['crl']
            ],
            'X509CrlChainApiModel' => [
                'properties' => ['chain' => [
                    'type' => 'array',
                    'items' => ['$ref' => '#/definitions/X509CrlApiModel']
                ]],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'StartSigningRequestApiModel' => [
                'properties' => [
                    'entityId' => ['type' => 'string'],
                    'groupId' => ['type' => 'string'],
                    'certificateRequest' => ['type' => 'object']
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'entityId',
                    'groupId',
                    'certificateRequest'
                ]
            ],
            'StartSigningRequestResponseApiModel' => [
                'properties' => ['requestId' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => ['requestId']
            ],
            'VaultOperationContextApiModel' => [
                'properties' => [
                    'authorityId' => ['type' => 'string'],
                    'time' => [
                        'type' => 'string',
                        'format' => 'date-time'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => ['time']
            ],
            'CertificateRequestRecordApiModel' => [
                'properties' => [
                    'requestId' => ['type' => 'string'],
                    'entityId' => ['type' => 'string'],
                    'groupId' => ['type' => 'string'],
                    'state' => [
                        'type' => 'string',
                        'enum' => [
                            'New',
                            'Approved',
                            'Rejected',
                            'Failure',
                            'Completed',
                            'Accepted'
                        ]
                    ],
                    'type' => [
                        'type' => 'string',
                        'enum' => [
                            'SigningRequest',
                            'KeyPairRequest'
                        ]
                    ],
                    'errorInfo' => ['type' => 'object'],
                    'submitted' => ['$ref' => '#/definitions/VaultOperationContextApiModel'],
                    'approved' => ['$ref' => '#/definitions/VaultOperationContextApiModel'],
                    'accepted' => ['$ref' => '#/definitions/VaultOperationContextApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'FinishSigningRequestResponseApiModel' => [
                'properties' => [
                    'request' => ['$ref' => '#/definitions/CertificateRequestRecordApiModel'],
                    'certificate' => ['$ref' => '#/definitions/X509CertificateApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'StartNewKeyPairRequestApiModel' => [
                'properties' => [
                    'entityId' => ['type' => 'string'],
                    'groupId' => ['type' => 'string'],
                    'certificateType' => [
                        'type' => 'string',
                        'enum' => [
                            'ApplicationInstanceCertificate',
                            'HttpsCertificate',
                            'UserCredentialCertificate'
                        ]
                    ],
                    'subjectName' => ['type' => 'string'],
                    'domainNames' => [
                        'type' => 'array',
                        'items' => ['type' => 'string']
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'entityId',
                    'groupId',
                    'certificateType',
                    'subjectName'
                ]
            ],
            'StartNewKeyPairRequestResponseApiModel' => [
                'properties' => ['requestId' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => ['requestId']
            ],
            'PrivateKeyApiModel' => [
                'properties' => [
                    'kty' => [
                        'type' => 'string',
                        'enum' => [
                            'RSA',
                            'ECC',
                            'AES'
                        ]
                    ],
                    'n' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'e' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'dp' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'dq' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'qi' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'p' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'q' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'crv' => ['type' => 'string'],
                    'x' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'y' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'd' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'k' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ],
                    'key_hsm' => [
                        'type' => 'string',
                        'format' => 'byte'
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'FinishNewKeyPairRequestResponseApiModel' => [
                'properties' => [
                    'request' => ['$ref' => '#/definitions/CertificateRequestRecordApiModel'],
                    'certificate' => ['$ref' => '#/definitions/X509CertificateApiModel'],
                    'privateKey' => ['$ref' => '#/definitions/PrivateKeyApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'CertificateRequestQueryRequestApiModel' => [
                'properties' => [
                    'entityId' => ['type' => 'string'],
                    'state' => [
                        'type' => 'string',
                        'enum' => [
                            'New',
                            'Approved',
                            'Rejected',
                            'Failure',
                            'Completed',
                            'Accepted'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'CertificateRequestQueryResponseApiModel' => [
                'properties' => [
                    'requests' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/CertificateRequestRecordApiModel']
                    ],
                    'nextPageLink' => ['type' => 'string']
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
            'TrustGroupApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'parentId' => ['type' => 'string'],
                    'type' => [
                        'type' => 'string',
                        'enum' => [
                            'ApplicationInstanceCertificate',
                            'HttpsCertificate',
                            'UserCredentialCertificate'
                        ]
                    ],
                    'subjectName' => ['type' => 'string'],
                    'lifetime' => ['type' => 'string'],
                    'keySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'signatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ],
                    'issuedLifetime' => ['type' => 'string'],
                    'issuedKeySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'issuedSignatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'name',
                    'subjectName'
                ]
            ],
            'TrustGroupRegistrationApiModel' => [
                'properties' => [
                    'id' => ['type' => 'string'],
                    'group' => ['$ref' => '#/definitions/TrustGroupApiModel']
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'id',
                    'group'
                ]
            ],
            'TrustGroupRegistrationListApiModel' => [
                'properties' => [
                    'registrations' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/TrustGroupRegistrationApiModel']
                    ],
                    'nextPageLink' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'TrustGroupRegistrationRequestApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'parentId' => ['type' => 'string'],
                    'subjectName' => ['type' => 'string'],
                    'issuedLifetime' => ['type' => 'string'],
                    'issuedKeySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'issuedSignatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'name',
                    'parentId',
                    'subjectName'
                ]
            ],
            'TrustGroupRegistrationResponseApiModel' => [
                'properties' => ['id' => ['type' => 'string']],
                'additionalProperties' => FALSE,
                'required' => ['id']
            ],
            'TrustGroupUpdateRequestApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'issuedLifetime' => ['type' => 'string'],
                    'issuedKeySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'issuedSignatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ],
            'TrustGroupRootCreateRequestApiModel' => [
                'properties' => [
                    'name' => ['type' => 'string'],
                    'type' => [
                        'type' => 'string',
                        'enum' => [
                            'ApplicationInstanceCertificate',
                            'HttpsCertificate',
                            'UserCredentialCertificate'
                        ]
                    ],
                    'subjectName' => ['type' => 'string'],
                    'lifetime' => ['type' => 'string'],
                    'keySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'signatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ],
                    'issuedLifetime' => ['type' => 'string'],
                    'issuedKeySize' => [
                        'type' => 'integer',
                        'format' => 'int32'
                    ],
                    'issuedSignatureAlgorithm' => [
                        'type' => 'string',
                        'enum' => [
                            'Rsa256',
                            'Rsa384',
                            'Rsa512',
                            'Rsa256Pss',
                            'Rsa384Pss',
                            'Rsa512Pss'
                        ]
                    ]
                ],
                'additionalProperties' => FALSE,
                'required' => [
                    'name',
                    'subjectName',
                    'lifetime'
                ]
            ],
            'X509CertificateListApiModel' => [
                'properties' => [
                    'certificates' => [
                        'type' => 'array',
                        'items' => ['$ref' => '#/definitions/X509CertificateApiModel']
                    ],
                    'nextPageLink' => ['type' => 'string']
                ],
                'additionalProperties' => FALSE,
                'required' => []
            ]
        ]
    ];
}
