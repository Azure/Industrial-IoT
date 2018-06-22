// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Proxy.Runtime {
    using Microsoft.Azure.IIoT.Http.Proxy;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Services.Http.Proxy;
    using Microsoft.Azure.IIoT.Services.Runtime;
    using Microsoft.Extensions.Configuration;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Web service configuration - wraps a configuration root
    /// </summary>
    public class Config : ServiceConfig, IReverseProxyConfig, IThumbprintValidatorConfig {

        //
        // Service config
        //
        private const string kEndpointKey = "endpoints";
        public IDictionary<string, string> ResourceIdToHostLookup =>
            Configuration.GetSection(kEndpointKey).GetChildren()
                .ToDictionary(c => c.Key, c => c.Value);

        //
        // Validator config
        //
        private const string kCertThumbprintKey = "sslCertThumbprint";
        /// <summary>Pinned cert thumbprint</summary>
        public string CertThumbprint => GetString(kCertThumbprintKey);


        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) : 
            base(Uptime.ProcessId, ServiceInfo.ID, configuration) {
        }
    }
}
