// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry
    /// </summary>
    public interface IApplicationRegistry {

        /// <summary>
        /// Register application using the application info as
        /// template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResultModel> RegisterAsync(
            ApplicationRegistrationRequestModel request);

        /// <summary>
        /// Read full application model for specified
        /// application (server/client) which includes all
        /// endpoints if there are any.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="filterInactiveTwins"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins = false);

        /// <summary>
        /// Update an existing application, e.g. server
        /// certificate, or additional capabilities.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(
            ApplicationRegistrationUpdateModel request);

        /// <summary>
        /// Get list of registered application sites to group
        /// applications visually
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize = null);

        /// <summary>
        /// List all applications or continue find query.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize = null);

        /// <summary>
        /// Find applications for the specified information
        /// criterias.  The returned continuation if any must
        /// be passed to ListApplicationsAsync.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize = null);

        /// <summary>
        /// Unregister application and all associated endpoints.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId);

        /// <summary>
        /// Clean up all applications and twins that have not
        /// been seen since for the amount of time
        /// </summary>
        /// <param name="notSeenFor"></param>
        /// <returns></returns>
        Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor);
    }
}
