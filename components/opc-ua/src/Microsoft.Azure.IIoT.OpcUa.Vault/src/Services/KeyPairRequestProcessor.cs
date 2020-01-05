// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key pair request processor
    /// </summary>
    public sealed class KeyPairRequestProcessor : IKeyPairRequestProcessor {

        /// <summary>
        /// Create certificate request
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="serializer"></param>
        /// <param name="entities"></param>
        /// <param name="repo"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public KeyPairRequestProcessor(IRequestRepository repo, IKeyStore keys,
            IKeyHandleSerializer serializer, IEntityInfoResolver entities,
            ICertificateRequestEventBroker broker, ILogger logger) {

            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<StartNewKeyPairRequestResultModel> StartNewKeyPairRequestAsync(
            StartNewKeyPairRequestModel request, VaultOperationContextModel context,
            CancellationToken ct) {

            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.EntityId)) {
                throw new ArgumentNullException(nameof(request.EntityId));
            }
            if (string.IsNullOrEmpty(request.GroupId)) {
                throw new ArgumentNullException(nameof(request.GroupId));
            }
            if (string.IsNullOrEmpty(request.SubjectName)) {
                throw new ArgumentNullException(nameof(request.SubjectName));
            }
            // Get entity
            var entity = await _entities.FindEntityAsync(request.EntityId);
            if (entity == null) {
                throw new ResourceNotFoundException("Entity not found");
            }

            // Validate subject name
            var subjectList = Opc.Ua.Utils.ParseDistinguishedName(request.SubjectName);
            if (subjectList == null ||
                subjectList.Count == 0) {
                throw new ArgumentException("Invalid Subject", nameof(request.SubjectName));
            }
            if (!subjectList.Any(c => c.StartsWith("CN=", StringComparison.InvariantCulture))) {
                throw new ArgumentException("Invalid Subject, must have a common name (CN=).",
                    nameof(request.SubjectName));
            }
            entity.SubjectName = string.Join(", ", subjectList);

            // Add domain names
            if (request.DomainNames != null) {
                if (entity.Addresses == null) {
                    entity.Addresses = request.DomainNames;
                }
                else {
                    entity.Addresses.AddRange(request.DomainNames);
                }
            }

            var result = await _repo.AddAsync(new CertificateRequestModel {
                Record = new CertificateRequestRecordModel {
                    Type = CertificateRequestType.KeyPairRequest,
                    EntityId = entity.Id,
                    GroupId = request.GroupId,
                    Submitted = context.Validate(),
                },
                Entity = entity
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestSubmittedAsync(result));

            _logger.Information("New Key pair request submitted.");
            return new StartNewKeyPairRequestResultModel {
                RequestId = result.Record.RequestId
            };
        }

        /// <inheritdoc/>
        public async Task<FinishNewKeyPairRequestResultModel> FinishNewKeyPairRequestAsync(
            string requestId, VaultOperationContextModel context, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = await _repo.FindAsync(requestId, ct);
            if (request == null) {
                throw new ResourceNotFoundException("Request not found");
            }
            try {
                var entity = await _entities.FindEntityAsync(request.Entity.Id);
                if (entity != null) {
                    throw new ResourceInvalidStateException("Entity removed.");
                }
                var result = new FinishNewKeyPairRequestResultModel {
                    Request = request.Record
                };
                if (request.Record.State == CertificateRequestState.Completed) {
                    result.Certificate = request.Certificate;
                    // get private key
                    if (request.KeyHandle != null) {
                        var handle = Try.Op(
                            () => _serializer.DeserializeHandle(request.KeyHandle));
                        if (handle != null) {
                            var privateKey = await Try.Async(
                                () => _keys.ExportKeyAsync(handle, ct));
                            result.PrivateKey = privateKey.ToServiceModel();
                            await Try.Async(
                                () => _keys.DeleteKeyAsync(handle, ct));
                        }
                    }
                }
                return result;
            }
            finally {
                if (request.Record.State == CertificateRequestState.Completed) {
                    // Accept
                    await _broker.NotifyAllAsync(
                        l => l.OnCertificateRequestAcceptedAsync(request));
                    _logger.Information("Key pair response accepted and finished.");
                }
            }
        }

        private readonly IKeyStore _keys;
        private readonly IKeyHandleSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IRequestRepository _repo;
        private readonly ICertificateRequestEventBroker _broker;
        private readonly IEntityInfoResolver _entities;
    }
}
