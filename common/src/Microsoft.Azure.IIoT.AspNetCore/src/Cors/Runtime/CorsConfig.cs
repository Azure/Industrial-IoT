// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Cors.Runtime {
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Cors configuration
    /// </summary>
    public class CorsConfig : ConfigBase, ICorsConfig {

        /// <summary>
        /// Cors configuration
        /// </summary>
        private const string kCorsWhitelistKey = "Cors:Whitelist";
        /// <summary>Cors whitelist</summary>
        public string CorsWhitelist => GetStringOrDefault(kCorsWhitelistKey,
            () => GetStringOrDefault(PcsVariable.PCS_CORS_WHITELIST,
                () => "*"));
        /// <summary>Whether enabled</summary>
        public bool CorsEnabled =>
            !string.IsNullOrEmpty(CorsWhitelist.Trim());

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CorsConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
