// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using System;

    /// <summary>
    /// Session services config
    /// </summary>
    public interface ISessionServicesConfig {

        /// <summary>
        /// Max number of concurrent session
        /// </summary>
        int MaxSessionCount { get; }

        /// <summary>
        /// Max session timeout allowed
        /// </summary>
        TimeSpan MaxSessionTimeout { get; }

        /// <summary>
        /// Min session timeout
        /// </summary>
        TimeSpan MinSessionTimeout { get; }

        /// <summary>
        /// Max request age
        /// </summary>
        TimeSpan MaxRequestAge { get; }

        /// <summary>
        /// Nonce length to use
        /// </summary>
        int NonceLength { get; }
    }
}
