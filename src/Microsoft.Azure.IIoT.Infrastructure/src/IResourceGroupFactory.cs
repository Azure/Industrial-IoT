// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using System.Threading.Tasks;

    /// <summary>
    /// Azure resource group factory
    /// </summary>
    public interface IResourceGroupFactory {

        /// <summary>
        /// Finds and returns resource group based on specified
        /// information.
        /// </summary>
        /// <param name="resourceGroup">The name of the resource
        /// group to find</param>
        /// <param name="deleteOnDispose">Whether the group is
        /// auto deleted on dispose</param>
        /// <param name="subscription">Optional subscription
        /// info to use when creating the resource group</param>
        /// <returns></returns>
        Task<IResourceGroupResource> GetAsync(
            string resourceGroup, bool deleteOnDispose,
            ISubscriptionInfo subscription);

        /// <summary>
        /// Create resource group based on the specified
        /// information.
        /// </summary>
        /// <param name="resourceGroup">The name of the resource
        /// group or null for random name</param>
        /// <param name="deleteOnDispose">Whether the group is
        /// auto deleted on dispose</param>
        /// <param name="subscription">Optional subscription
        /// info to use when creating the resource group</param>
        /// <returns></returns>
        Task<IResourceGroupResource> CreateAsync(
            string resourceGroup, bool deleteOnDispose,
            ISubscriptionInfo subscription);
    }
}
