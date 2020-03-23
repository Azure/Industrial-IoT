// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Forwarded headers processing configuration.
    /// </summary>
    public class ForwardedHeadersConfig : ConfigBase, IForwardedHeadersConfig {

        private const string kAspNetCore_ForwardedHeaders_Enabled = "AspNetCore:ForwardedHeaders:Enabled";
        private const string kAspNetCore_ForwardedHeaders_ForwardLimit = "AspNetCore:ForwardedHeaders:ForwardLimit";

        private const bool kAspNetCore_ForwardedHeaders_Enabled_Default = false;
        private const int kAspNetCore_ForwardedHeaders_ForwardLimit_Default = 0;

        /// <inheritdoc/>
        public bool AspNetCoreForwardedHeadersEnabled =>
            GetBoolOrDefault(kAspNetCore_ForwardedHeaders_Enabled,
                () => GetBoolOrDefault(AspNetCoreVariable.ASPNETCORE_FORWARDEDHEADERS_ENABLED,
                () => kAspNetCore_ForwardedHeaders_Enabled_Default));

        /// <inheritdoc/>
        public int AspNetCoreForwardedHeadersForwardLimit =>
            GetIntOrDefault(kAspNetCore_ForwardedHeaders_ForwardLimit,
                () => GetIntOrDefault(AspNetCoreVariable.ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT,
                () => kAspNetCore_ForwardedHeaders_ForwardLimit_Default));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ForwardedHeadersConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
