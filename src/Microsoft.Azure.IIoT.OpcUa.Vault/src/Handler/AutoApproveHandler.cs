// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Handler {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Auto approve handler
    /// </summary>
    public sealed class AutoApproveHandler : ICertificateRequestListener {

        /// <summary>
        /// Create approver
        /// </summary>
        /// <param name="management"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public AutoApproveHandler(IRequestManagement management, IVaultConfig config,
            ILogger logger) {
            _management = management ?? throw new ArgumentNullException(nameof(management));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task OnCertificateRequestSubmittedAsync(CertificateRequestModel request) {
            if (!_config.AutoApprove) {
                return;
            }
            await _management.ApproveRequestAsync(request.Record.RequestId,
                request.Record.Submitted);
            _logger.Information("Request {@request} for {@entity} was auto-approved.",
                request.Record, request.Entity);
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
        public Task OnCertificateRequestApprovedAsync(CertificateRequestModel request) {
            return Task.CompletedTask;
        }

        private readonly IVaultConfig _config;
        private readonly ILogger _logger;
        private readonly IRequestManagement _management;
    }
}
