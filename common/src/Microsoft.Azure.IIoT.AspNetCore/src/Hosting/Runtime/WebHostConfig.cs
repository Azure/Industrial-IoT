// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Web Host configuration
    /// </summary>
    public class WebHostConfig : ConfigBase, IWebHostConfig {

        /// <summary>
        /// Host configuration
        /// </summary>
        private const string kAuth_HttpsRedirectPortKey = "Auth:HttpsRedirectPort";
        private const string kHost_ServicePathBase = "Host:ServicePathBase";

        /// <summary>Https enforced</summary>
        public int HttpsRedirectPort => GetIntOrDefault(kAuth_HttpsRedirectPortKey,
            () => GetIntOrDefault("PCS_AUTH_HTTPSREDIRECTPORT", () => 0));

        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(kHost_ServicePathBase,
            () => GetStringOrDefault(PcsVariable.PCS_SERVICE_PATH_BASE));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public WebHostConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
