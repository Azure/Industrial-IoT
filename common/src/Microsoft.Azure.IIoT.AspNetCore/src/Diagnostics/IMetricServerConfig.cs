// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Diagnostics {

    /// <summary>
    /// Configuration interface for prometheus metric server
    /// </summary>
    public interface IMetricServerConfig {

        /// <summary>
        /// Metric server port
        /// </summary>
        int Port { get; }
    }
}
