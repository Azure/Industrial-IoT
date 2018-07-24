// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Management.Fluent {
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;
    using System.Threading.Tasks;

    public static class FluentExtensions {

        /// <summary>
        /// Returns whether a resource with name exists in
        /// the resource group.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<bool> ContainsAsync<T>(
            this ISupportsGettingByResourceGroup<T> collection,
            string resourceGroup, string name) where T : class {
            try {
                var resource = await collection.GetByResourceGroupAsync(
                    resourceGroup, name);
                return resource != null;
            }
            catch {
                return false;
            }
        }
    }
}
