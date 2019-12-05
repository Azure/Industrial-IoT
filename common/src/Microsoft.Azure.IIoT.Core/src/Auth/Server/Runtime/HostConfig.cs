﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Host configuration
    /// </summary>
    public class HostConfig : ConfigBase, IHostConfig {

        /// <summary>
        /// Host configuration
        /// </summary>
        private const string kAuth_HttpsRedirectPortKey = "Auth:HttpsRedirectPort";

        /// <summary>Https enforced</summary>
        public int HttpsRedirectPort => GetIntOrDefault(kAuth_HttpsRedirectPortKey,
            GetIntOrDefault("PCS_AUTH_HTTPSREDIRECTPORT", 0));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public HostConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
