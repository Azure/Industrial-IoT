// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate request management
    /// </summary>
    public sealed class CertificateRequestManager : IRequestManagement, IRequestWorkflow {

        /// <summary>
        /// Create certificate request
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="broker"></param>
        /// <param name="serializer"></param>
        public CertificateRequestManager(IRequestRepository repo,
            ICertificateRequestEventBroker broker, IJsonSerializer serializer) {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <inheritdoc/>
        public async Task ApproveRequestAsync(string requestId,
            VaultOperationContextModel context, CancellationToken ct) {
            var result = await _repo.UpdateAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.New:
                        request.Record.State = CertificateRequestState.Approved;
                        request.Record.Approved = context.Validate();
                        return true;
                    case CertificateRequestState.Approved:
                        return false;
                    default:
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestApprovedAsync(result));
        }

        /// <inheritdoc/>
        public async Task RejectRequestAsync(string requestId,
            VaultOperationContextModel context, CancellationToken ct) {
            var result = await _repo.UpdateAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.New:
                        request.Record.State = CertificateRequestState.Rejected;
                        request.Record.Approved = context.Validate();
                        return true;
                    case CertificateRequestState.Rejected:
                        return false;
                    default:
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestCompletedAsync(result));
        }

        /// <inheritdoc/>
        public async Task AcceptRequestAsync(string requestId,
            VaultOperationContextModel context, CancellationToken ct) {
            var result = await _repo.UpdateAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.New:
                    case CertificateRequestState.Approved:
                    case CertificateRequestState.Rejected:
                    case CertificateRequestState.Failure:
                    case CertificateRequestState.Completed:
                        request.Record.State = CertificateRequestState.Accepted;
                        request.Record.Accepted = context.Validate();
                        return true;
                    case CertificateRequestState.Accepted:
                        return false;
                    default:
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestAcceptedAsync(result));
        }

        /// <inheritdoc/>
        public async Task DeleteRequestAsync(string requestId,
            VaultOperationContextModel context, CancellationToken ct) {
            var result = await _repo.DeleteAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.Accepted:
                        return true;
                    default:
                        if (request.Record.Accepted != null &&
                            (request.Record.Accepted.Time + TimeSpan.FromDays(1))
                                < DateTime.UtcNow) {
                            return true;
                        }
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestDeletedAsync(result));
        }

        /// <inheritdoc/>
        public async Task FailRequestAsync<T>(string requestId, T errorInfo,
            CancellationToken ct) {
            var result = await _repo.UpdateAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.New:
                    case CertificateRequestState.Approved:
                    case CertificateRequestState.Rejected:
                        request.Record.State = CertificateRequestState.Failure;
                        request.Record.ErrorInfo = _serializer.FromObject(errorInfo);
                        return true;
                    case CertificateRequestState.Failure:
                    case CertificateRequestState.Accepted:
                    case CertificateRequestState.Completed:
                        return false;
                    default:
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestCompletedAsync(result));
        }

        /// <inheritdoc/>
        public async Task CompleteRequestAsync(string requestId,
            Action<CertificateRequestModel> predicate, CancellationToken ct) {
            var result = await _repo.UpdateAsync(requestId, request => {
                switch (request.Record.State) {
                    case CertificateRequestState.New:
                    case CertificateRequestState.Approved:
                    case CertificateRequestState.Rejected:
                        predicate(request);
                        request.Record.State = CertificateRequestState.Completed;
                        return true;
                    case CertificateRequestState.Failure:
                    case CertificateRequestState.Accepted:
                    case CertificateRequestState.Completed:
                        return false;
                    default:
                        throw new ResourceInvalidStateException(
                            $"Request in the wrong state ({request.Record.State}.");
                }
            }, ct);
            await _broker.NotifyAllAsync(
                l => l.OnCertificateRequestCompletedAsync(result));
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestRecordModel> GetRequestAsync(string requestId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId),
                    "The request id must be provided");
            }
            var request = await _repo.FindAsync(requestId, ct);
            if (request == null) {
                throw new ResourceNotFoundException("Request not found");
            }
            return request.Record;
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestQueryResultModel> QueryRequestsAsync(
            CertificateRequestQueryRequestModel query, int? maxResults,
            CancellationToken ct) {
            var results = await _repo.QueryAsync(query, null, maxResults, ct);
            return new CertificateRequestQueryResultModel {
                Requests = results.Requests.Select(r => r.Record).ToList(),
                NextPageLink = results.NextPageLink
            };
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestQueryResultModel> ListRequestsAsync(
            string nextPageLink, int? maxResults,
            CancellationToken ct) {
            var results = await _repo.QueryAsync(null, nextPageLink, maxResults, ct);
            return new CertificateRequestQueryResultModel {
                Requests = results.Requests.Select(r => r.Record).ToList(),
                NextPageLink = results.NextPageLink
            };
        }

        private readonly IRequestRepository _repo;
		private readonly IJsonSerializer _serializer;
        private readonly ICertificateRequestEventBroker _broker;
    }
}
