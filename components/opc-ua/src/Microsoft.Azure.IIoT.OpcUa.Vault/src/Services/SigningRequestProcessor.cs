// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Serilog;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Signing request processor
    /// </summary>
    public sealed class SigningRequestProcessor : ISigningRequestProcessor {

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="entities"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public SigningRequestProcessor(IRequestRepository repo, IEntityInfoResolver entities,
            ICertificateRequestEventBroker broker, ILogger logger) {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <inheritdoc/>
        public async Task<StartSigningRequestResultModel> StartSigningRequestAsync(
            StartSigningRequestModel request, VaultOperationContextModel context,
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

            var entity = await _entities.FindEntityAsync(request.EntityId);
            if (entity == null) {
                throw new ResourceNotFoundException("Entity not found");
            }

            // Validate signing request and update entity information
            var signingRequest = request.ToRawData();
            var info = signingRequest.ToCertificationRequest();
            var altNames = info.Extensions?
                .OfType<X509SubjectAltNameExtension>()
                .SingleOrDefault();

            if (!(entity.Uris?.All(
                    u => altNames?.Uris?.Any(x => u.EqualsIgnoreCase(x)) ?? false)
                ?? true)) {
                throw new ArgumentException(
                    "Signing Request's alternative names does not include entity's uris");
            }

            var domainNames = new HashSet<string>(entity.Addresses ?? new List<string>());
            if (altNames?.DomainNames != null) {
                foreach (var name in altNames.DomainNames) {
                    domainNames.Add(name);
                }
            }
            if (altNames?.IPAddresses != null) {
                foreach (var name in altNames.IPAddresses) {
                    domainNames.Add(name);
                }
            }
            var uris = new HashSet<string>(entity.Uris ?? new List<string>());
            if (altNames?.Uris != null) {
                foreach (var name in altNames.Uris) {
                    uris.Add(name);
                }
            }

            entity.Addresses = domainNames.ToList();
            entity.Uris = uris.ToList();
            entity.SubjectName = info.Subject.Name;

            var result = await _repo.AddAsync(new CertificateRequestModel {
                Record = new CertificateRequestRecordModel {
                    Type = CertificateRequestType.KeyPairRequest,
                    EntityId = entity.Id,
                    GroupId = request.GroupId,
                    Submitted = context.Validate(),
                },
                Entity = entity.Validate(),
                SigningRequest = signingRequest
            }, ct);

            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestSubmittedAsync(result));
            _logger.Information("New signing request submitted.");
            return new StartSigningRequestResultModel {
                RequestId = result.Record.RequestId
            };
        }

        /// <inheritdoc/>
        public async Task<FinishSigningRequestResultModel> FinishSigningRequestAsync(
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
                if (request.Record.State == CertificateRequestState.Completed) {
                    return new FinishSigningRequestResultModel {
                        Request = request.Record,
                        Certificate = request.Certificate
                    };
                }
                return new FinishSigningRequestResultModel {
                    Request = request.Record
                };
            }
            finally {
                if (request.Record.State == CertificateRequestState.Completed) {
                    // Accept
                    await _broker.NotifyAllAsync(
                        l => l.OnCertificateRequestAcceptedAsync(request));
                    _logger.Information("Signing response accepted and finished.");
                }
            }
        }

        private readonly IRequestRepository _repo;
        private readonly ILogger _logger;
        private readonly IEntityInfoResolver _entities;
        private readonly ICertificateRequestEventBroker _broker;
    }
}
