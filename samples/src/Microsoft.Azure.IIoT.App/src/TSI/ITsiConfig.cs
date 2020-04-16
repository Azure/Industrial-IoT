// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.TSI {

    /// <summary>
    /// Configuration interface for TSI
    /// </summary>
    public interface ITsiConfig {
        /// <summary>
        /// Determines URL path base for TSI query.
        /// </summary>
        string DataAccessFQDN { get; }
    }
}
