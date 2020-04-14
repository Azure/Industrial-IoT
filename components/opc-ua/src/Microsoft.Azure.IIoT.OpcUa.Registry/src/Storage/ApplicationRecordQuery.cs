// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry listener providing record query services by storing the
    /// application events in the database then query on top of it.
    /// </summary>
    public sealed class ApplicationRecordQuery : IApplicationRecordQuery,
        IApplicationRegistryListener, IDisposable {

        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public ApplicationRecordQuery(IItemContainerFactory db, ILogger logger,
            IRegistryEvents<IApplicationRegistryListener> events = null) {
            _database = new ApplicationDatabase(db, logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unregister = events?.Register(this);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _unregister?.Invoke();
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return AddAsync(application);
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            // TODO handle patching
            return UpdateAsync(application);
        }

        /// <inheritdoc/>
        public Task OnApplicationEnabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return AddAsync(application);
        }

        /// <inheritdoc/>
        public Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return DeleteAsync(application.ApplicationId);
        }

        /// <inheritdoc/>
        public Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            string applicationId, ApplicationInfoModel application) {
            return DeleteAsync(applicationId);
        }

        /// <inheritdoc/>
        public Task<ApplicationRecordListModel> QueryApplicationsAsync(
            ApplicationRecordQueryModel query, CancellationToken ct) {
            return _database.QueryApplicationsAsync(query, ct);
        }

        /// <summary>
        /// Add application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="noUpdate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task AddAsync(ApplicationInfoModel application,
            bool noUpdate = false, CancellationToken ct = default) {
            try {
                await _database.AddAsync(application, null, ct);
            }
            catch (ConflictingResourceException) when (!noUpdate) {
                await UpdateAsync(application, true, ct);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add application to index");
            }
        }

        /// <summary>
        /// Update application in index
        /// </summary>
        /// <param name="application"></param>
        /// <param name="noAdd"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task UpdateAsync(ApplicationInfoModel application,
            bool noAdd = false, CancellationToken ct = default) {
            try {
                await _database.UpdateAsync(application.ApplicationId,
                    (existing, d) => {
                        existing.Patch(application);
                        return (true, d);
                    }, ct);
            }
            catch (ResourceNotFoundException) when (!noAdd) {
                await AddAsync(application, true, ct);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to update application in index");
            }
        }

        /// <summary>
        /// Delete application from index
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task DeleteAsync(string applicationId,
            CancellationToken ct = default) {
            try {
                await _database.DeleteAsync(applicationId, a => true, ct);
            }
            catch (ResourceNotFoundException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to delete application from index");
            }
        }

        private readonly ApplicationDatabase _database;
        private readonly ILogger _logger;
        private readonly Action _unregister;
    }
}
