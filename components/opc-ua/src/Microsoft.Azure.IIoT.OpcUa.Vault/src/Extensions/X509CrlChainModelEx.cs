// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Crl collection model
    /// </summary>
    public static class X509CrlChainModelEx {

        /// <summary>
        /// Create collection model
        /// </summary>
        /// <param name="crls"></param>
        public static X509CrlChainModel ToServiceModel(this IEnumerable<Crl> crls) {
            return new X509CrlChainModel {
                Chain = crls
                    .Select(crl => crl.ToServiceModel())
                    .ToList(),
            };
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="crls"></param>
        public static IEnumerable<Crl> ToStackModel(this X509CrlChainModel crls) {
            return crls.Chain.Select(c => c.ToStackModel());
        }
    }
}
