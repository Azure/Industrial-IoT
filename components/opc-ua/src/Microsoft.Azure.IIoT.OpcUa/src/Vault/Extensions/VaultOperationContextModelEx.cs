// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Operation extensions
    /// </summary>
    public static class VaultOperationContextModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static VaultOperationContextModel Clone(
            this VaultOperationContextModel model) {
            model = model.Validate();
            return new VaultOperationContextModel {
                AuthorityId = model.AuthorityId,
                Time = model.Time
            };
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static VaultOperationContextModel Validate(
            this VaultOperationContextModel context) {
            if (context == null) {
                context = new VaultOperationContextModel {
                    AuthorityId = null, // Should throw if configured
                    Time = DateTime.UtcNow
                };
            }
            return context;
        }
    }
}
