// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using System.Threading.Tasks;

    public interface ISubscriptionInfo {

        /// <summary>
        /// Environment of subscription
        /// </summary>
        Task<string> GetEnvironment();

        /// <summary>
        /// Select subscription to use
        /// </summary>
        Task<string> GetSubscriptionId();

        /// <summary>
        /// Selected region
        /// </summary>
        Task<string> GetRegionAsync();
    }
}
