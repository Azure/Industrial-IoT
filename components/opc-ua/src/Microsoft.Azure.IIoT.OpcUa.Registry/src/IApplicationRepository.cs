// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application repository - used by application registry to
    /// store application objects.
    /// </summary>
    public interface IApplicationRepository {

        /// <summary>
        /// Add application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="disabled"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoModel> AddAsync(
            ApplicationInfoModel application, bool? disabled = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="throwIfNotFound"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoModel> GetAsync(string applicationId,
            bool throwIfNotFound = true,
            CancellationToken ct = default);

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="updater"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoModel> UpdateAsync(string applicationId,
            Func<ApplicationInfoModel, bool?, (bool?, bool?)> updater,
            CancellationToken ct = default);

        /// <summary>
        /// Delete application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="precondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoModel> DeleteAsync(string applicationId,
            Func<ApplicationInfoModel, bool> precondition = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find applications for the specified information
        /// criterias.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> QueryAsync(
            ApplicationRegistrationQueryModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> ListAsync(string continuation,
            int? pageSize, CancellationToken ct = default);

        /// <summary>
        /// List all applications in a site
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<ApplicationInfoModel>> ListAllAsync(string siteId,
            string supervisorId,
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
    }
}
