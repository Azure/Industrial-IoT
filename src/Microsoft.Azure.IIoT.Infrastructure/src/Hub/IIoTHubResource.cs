// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Hub {
    using System.Threading.Tasks;

    /// <summary>
    /// A managed iot hub
    /// </summary>
    public interface IIoTHubResource : IResource {

        /// <summary>
        /// Primary connection string
        /// </summary>
        string PrimaryConnectionString { get; }

        /// <summary>
        /// Secondary connection string
        /// </summary>
        string SecondaryConnectionString { get; }

        /// <summary>
        /// Check whether the endpoints are healthy
        /// </summary>
        /// <returns></returns>
        Task<bool> IsHealthyAsync();
    }
}
