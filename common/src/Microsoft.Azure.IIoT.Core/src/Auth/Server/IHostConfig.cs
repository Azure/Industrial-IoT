﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Server {
    /// <summary>
    /// Host configuration
    /// </summary>
    public interface IHostConfig {

        /// <summary>
        /// null value allows http. Should always be set to
        /// the https port except for local development.
        /// JWT tokens are not encrypted and if not sent over
        /// HTTPS will allow an attacker to get the same
        /// authorization.
        /// </summary>
        int HttpsRedirectPort { get; }
    }
}
