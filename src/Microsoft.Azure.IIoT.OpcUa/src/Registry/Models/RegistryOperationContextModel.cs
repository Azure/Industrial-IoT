// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    public class RegistryOperationContextModel {

        /// <summary>
        /// User
        /// </summary>
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        public DateTime Time { get; set; }
    }
}

