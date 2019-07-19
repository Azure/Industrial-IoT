// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application extended query interface.
    /// </summary>
    public interface IApplicationRecordQuery {

        /// <summary>
        /// Query for Applications using the search parameters
        /// required for the OPC UA GDS server QueryServers
        /// and QueryApplications API.
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRecordListModel> QueryApplicationsAsync(
            ApplicationRecordQueryModel query,
            CancellationToken ct = default);
    }
}
