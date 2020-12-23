// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Status flags
    /// </summary>
    public static class X509ChainStatusEx {

        /// <summary>
        /// To service model
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static X509ChainStatus ToServiceModel(this X509ChainStatusFlags flags) {
            // TODO
            return (X509ChainStatus)flags;
        }
    }
}
