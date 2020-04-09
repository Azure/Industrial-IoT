// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {

    /// <summary>
    /// Configuration for http tunnel usage
    /// </summary>
    public interface IHttpTunnelConfig {

        /// <summary>
        /// Dynamically configure use of tunnel or the use of
        /// regular http client
        /// </summary>
        bool UseTunnel { get; set; }
    }
}
