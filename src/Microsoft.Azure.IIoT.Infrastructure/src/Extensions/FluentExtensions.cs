// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Management.Fluent {
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;
    using System;
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
            if (collection == null) {
                throw new ArgumentNullException(nameof(collection));
            }
            if (string.IsNullOrEmpty(resourceGroup)) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            try {
                var resource = await collection.GetByResourceGroupAsync(
                    resourceGroup, name);
                return resource != null;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Select new name and avoid collision in the resource
        /// group.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="prefix"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<string> SelectResourceNameAsync<T>(
            this ISupportsGettingByResourceGroup<T> collection,
            string resourceGroup, string prefix, string name = null) where T : class {

            if (string.IsNullOrEmpty(prefix)) {
                throw new ArgumentNullException(nameof(prefix));
            }
            // Check name - null means we need to create one
            if (string.IsNullOrEmpty(name)) {
                while (true) {
                    name = StringEx.CreateUnique(10, prefix);
                    var exists = await collection.ContainsAsync(resourceGroup, name);
                    if (!exists) {
                        break;
                    }
                }
            }
            else {
                var exists = await collection.ContainsAsync(resourceGroup, name);
                if (exists) {
                    throw new ArgumentException(
                        $"A {prefix} resource exists with name {name}", nameof(name));
                }
            }
            return name;
        }
    }
}
