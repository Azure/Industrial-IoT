// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;

    /// <summary>
    /// Operation extensions
    /// </summary>
    public static class RegistryOperationContextModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RegistryOperationContextModel Clone(
            this RegistryOperationContextModel model) {
            model = model.Validate();
            return new RegistryOperationContextModel {
                AuthorityId = model.AuthorityId,
                Time = model.Time
            };
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RegistryOperationContextModel Validate(
            this RegistryOperationContextModel context) {
            if (context == null) {
                context = new RegistryOperationContextModel {
                    AuthorityId = null, // Should throw if configured
                    Time = DateTime.UtcNow
                };
            }
            return context;
        }
    }
}
