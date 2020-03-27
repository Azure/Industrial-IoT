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
        /// Dynamicall configure use of tunnel or use of regular http
        /// </summary>
        bool UseTunnel { get; set; }
    }
}
