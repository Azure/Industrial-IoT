// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry stubs
    /// </summary>
    public sealed class ApplicationRegistryStub : IApplicationBulkProcessor,
        IApplicationRegistry {

        /// <inheritdoc/>
        public Task DisableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task EnableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveEndpoints, CancellationToken ct) {
            return Task.FromException<ApplicationRegistrationModel>(
                new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task<ApplicationInfoListModel> ListApplicationsAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            return Task.FromResult(new ApplicationInfoListModel());
        }

        /// <inheritdoc/>
        public Task<ApplicationSiteListModel> ListSitesAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            return Task.FromResult(new ApplicationSiteListModel());
        }

        /// <inheritdoc/>
        public Task ProcessDiscoveryEventsAsync(string siteId, string supervisorId,
            DiscoveryResultModel result, IEnumerable<DiscoveryEventModel> events) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct) {
            return Task.FromResult(new ApplicationInfoListModel());
        }

        /// <inheritdoc/>
        public Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct) {
            return Task.FromException<ApplicationRegistrationResultModel>(
                new NotSupportedException());
        }

        /// <inheritdoc/>
        public Task UnregisterApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }
    }
}
