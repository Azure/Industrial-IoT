// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Handler {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Signing request handler
    /// </summary>
    public sealed class SigningRequestHandler : ICertificateRequestListener {

        /// <summary>
        /// Create signing request handler
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="issuer"></param>
        public SigningRequestHandler(IRequestWorkflow workflow, ICertificateIssuer issuer) {

            _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
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
                if (request.Record.Type == CertificateRequestType.SigningRequest) {
                    await ProcessSigningRequestAsync(request);
                }
            }
            catch (Exception ex) {
                await Try.Async(() => _workflow.FailRequestAsync(request.Record.RequestId, ex));
            }
        }

        /// <summary>
        /// Process signing request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ProcessSigningRequestAsync(CertificateRequestModel request,
            CancellationToken ct = default) {

            var csr = request.SigningRequest.ToCertificationRequest();
            var extensions = request.Entity.ToX509Extensions();
            if (csr.Extensions != null) {
                extensions = extensions.Concat(csr.Extensions);
            }
            var subject = csr.Subject;
            if (!string.IsNullOrEmpty(request.Entity.SubjectName)) {
                subject = new X500DistinguishedName(request.Entity.SubjectName);
            }
            var now = DateTime.UtcNow.AddDays(-1);
            var notBefore = new DateTime(now.Year, now.Month, now.Day,
                0, 0, 0, DateTimeKind.Utc);

            // Call into issuer to issue new certificate for given public key
            var certKeyPair = await _issuer.CreateSignedCertificateAsync(
                request.Record.GroupId /* issuer cert */,
                request.Entity.Id /* issued cert name == entity id */,
                csr.PublicKey, subject, notBefore, sn => extensions, ct);

            await _workflow.CompleteRequestAsync(request.Record.RequestId, record => {
                record.Certificate = certKeyPair.ToServiceModel();
            }, ct);
        }

        private readonly IRequestWorkflow _workflow;
        private readonly ICertificateIssuer _issuer;
    }
}
