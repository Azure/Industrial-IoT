// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {

    /// <summary>
    /// Node attribute extensions
    /// </summary>
    public static class NodeAttributesEx {

        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this INodeAttributes attributes,
            uint attribute, T defaultValue) where T : class {
            if (attributes.TryGetAttribute<T>(attribute, out var result)) {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetAttribute<T>(this INodeAttributes attributes,
            uint attribute, T? defaultValue) where T : struct {
            if (attributes.TryGetAttribute<T>(attribute, out var result)) {
                return result;
            }
            return defaultValue;
        }
    }
}
