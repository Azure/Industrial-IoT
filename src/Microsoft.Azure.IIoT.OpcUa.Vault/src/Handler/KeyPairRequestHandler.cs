// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Handler {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Key pair request handler
    /// </summary>
    public sealed class KeyPairRequestHandler : ICertificateRequestListener {

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="workflow"></param>
        /// <param name="issuer"></param>
        public KeyPairRequestHandler(IKeyHandleSerializer serializer,
            IRequestWorkflow workflow, ICertificateIssuer issuer) {

            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        }

        /// <inheritdoc/>
        public Task OnCertificateRequestSubmittedAsync(CertificateRequestModel request) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnCertificateRequestCompletedAsync(CertificateRequestModel request) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnCertificateRequestAcceptedAsync(CertificateRequestModel request) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnCertificateRequestDeletedAsync(CertificateRequestModel request) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnCertificateRequestApprovedAsync(CertificateRequestModel request) {
            try {
                if (request.Record.Type == CertificateRequestType.KeyPairRequest) {
                    await CreateNewKeyPairAsync(request);
                }
            }
            catch (Exception ex) {
                await Try.Async(() => _workflow.FailRequestAsync(request.Record.RequestId, ex));
            }
        }

        /// <summary>
        /// Create new key pair
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task CreateNewKeyPairAsync(CertificateRequestModel request,
            CancellationToken ct = default) {
            var now = DateTime.UtcNow.AddDays(-1);
            var notBefore = new DateTime(now.Year, now.Month, now.Day,
                0, 0, 0, DateTimeKind.Utc);

            // Call into issuer service to issue new certificate and private key
            var certKeyPair = await _issuer.CreateCertificateAndPrivateKeyAsync(
                request.Record.GroupId /* issuer cert */,
                request.Entity.Id /* issued cert name == entity id */,
                new X500DistinguishedName(request.Entity.SubjectName), notBefore,
                new CreateKeyParams {
                    Type = KeyType.RSA, // TODO - should come from request
                    KeySize = 2048 // TODO - should come from request
                },
                sn => request.Entity.ToX509Extensions(), ct);

            await _workflow.CompleteRequestAsync(request.Record.RequestId, record => {
                record.KeyHandle = _serializer.SerializeHandle(certKeyPair.KeyHandle);
                record.Certificate = certKeyPair.ToServiceModel();
            }, ct);
        }

        private readonly IRequestWorkflow _workflow;
        private readonly IKeyHandleSerializer _serializer;
        private readonly ICertificateIssuer _issuer;
    }
}
