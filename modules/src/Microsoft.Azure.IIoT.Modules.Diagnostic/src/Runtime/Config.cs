// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.Runtime {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : ConfigBase, IModuleConfig {

        /// <summary>
        /// Module configuration
        /// </summary>
        private const string kEdgeHubConnectionString = "EdgeHubConnectionString";

        /// <summary>Hub connection string</summary>
        public string EdgeHubConnectionString =>
            GetStringOrDefault(kEdgeHubConnectionString);
        /// <summary>Whether to bypass cert validation</summary>
        public bool BypassCertVerification =>
            GetBoolOrDefault(nameof(BypassCertVerification), () => false);
        /// <summary>Whether to enable metrics collection</summary>
        public bool EnableMetrics => false;
        /// <summary>Transports to use</summary>
        public TransportOption Transport => Enum.Parse<TransportOption>(
            GetStringOrDefault(nameof(Transport), () => nameof(TransportOption.Any)), true);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
