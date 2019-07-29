// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry
    /// </summary>
    public interface IApplicationRegistry {

        /// <summary>
        /// Register application using the specified information.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Read full application model for specified
        /// application (server/client) which includes all
        /// endpoints if there are any.
        /// </summary>
        /// <param name="applicationId">The applicationId</param>
        /// <param name="filterInactiveEndpoints"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveEndpoints = false,
            CancellationToken ct = default);

        /// <summary>
        /// Update an existing application, e.g. server
        /// certificate, or additional capabilities.
        /// </summary>
        /// <param name="applicationId">The applicationId</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all applications or continue find query.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find applications for the specified information
        /// criterias.  The returned continuation if any must
        /// be passed to ListApplicationsAsync.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get list of registered application sites to group
        /// applications visually
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Disable the application. Does not remove the application
        /// from the database.
        /// </summary>
        /// <param name="applicationId">The applicationId</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisableApplicationAsync(string applicationId,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);

        /// <summary>
        /// Re-enable a potentially disabled application.
        /// </summary>
        /// <param name="applicationId">The applicationId</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task EnableApplicationAsync(string applicationId,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);

        /// <summary>
        /// Unregister application and all associated endpoints.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);

        /// <summary>
        /// Clean up all applications and endpoints that have not
        /// been seen since for the amount of time
        /// </summary>
        /// <param name="notSeenFor"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);
    }
}
