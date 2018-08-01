// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    public static class ResourceGroupFactoryEx {

        /// <summary>
        /// create randomly named resource group
        /// </summary>
        /// <param name="deleteOnDispose"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> CreateAsync(
            this IResourceGroupFactory service, string resourceGroup,
            bool deleteOnDispose) =>
            service.CreateAsync(resourceGroup, deleteOnDispose, null);

        /// <summary>
        /// create randomly named resource group
        /// </summary>
        /// <param name="deleteOnDispose"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> CreateAsync(
            this IResourceGroupFactory service, bool deleteOnDispose) =>
            service.CreateAsync(null, deleteOnDispose);

        /// <summary>
        /// create named resource group that is non auto-delete
        /// on dispose in specified subscription.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> CreateAsync(
            this IResourceGroupFactory service, string resourceGroup,
            ISubscriptionInfo subscription) =>
            service.CreateAsync(resourceGroup, false, subscription);

        /// <summary>
        /// create non auto-delete on dispose resource group
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> CreateAsync(
            this IResourceGroupFactory service, string resourceGroup) =>
            service.CreateAsync(resourceGroup, false);

        /// <summary>
        /// create non auto-delete on dispose randomly named
        /// resource group
        /// </summary>
        /// <returns></returns>
        public static Task<IResourceGroupResource> CreateAsync(
            this IResourceGroupFactory service) =>
            service.CreateAsync(null, false);

        /// <summary>
        /// Get non auto-delete on dispose resource group
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> GetAsync(
            this IResourceGroupFactory service, string resourceGroup,
            ISubscriptionInfo subscription) =>
            service.GetAsync(resourceGroup, false, subscription);

        /// <summary>
        /// Get named resource group
        /// </summary>
        /// <param name="deleteOnDispose"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> GetAsync(
            this IResourceGroupFactory service, string resourceGroup,
            bool deleteOnDispose) =>
            service.GetAsync(resourceGroup, deleteOnDispose, null);

        /// <summary>
        /// Get non auto-delete on dispose resource group
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <returns></returns>
        public static Task<IResourceGroupResource> GetAsync(
            this IResourceGroupFactory service, string resourceGroup) =>
            service.GetAsync(resourceGroup, false);

        /// <summary>
        /// Get or create new  named resource group
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="deleteOnDispose"></param>
        /// <param name="subscriptionInfo"></param>
        /// <returns></returns>
        public static async Task<IResourceGroupResource> GetOrCreateAsync(
            this IResourceGroupFactory service, string resourceGroup,
            bool deleteOnDispose, ISubscriptionInfo subscriptionInfo = null) {
            var rg = await Try.Async(() => service.GetAsync(resourceGroup,
                deleteOnDispose, subscriptionInfo));
            if (rg == null) {
                rg = await service.CreateAsync(resourceGroup, deleteOnDispose,
                    subscriptionInfo);
            }
            return rg;
        }
    }
}
