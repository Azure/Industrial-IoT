// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Module.Client.Runtime {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Module configuration
    /// </summary>
    public class ModuleConfig : ConfigBase {

        /// <summary>
        /// Module configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string kEdgeHubConnectionStringKey = "EdgeHubConnectionString";
        public const string kBypassCertVerificationKey = "BypassCertVerification";
        public const string kTransportKey = "Transport";
        public const string kEnableMetricsKey = "EnableMetrics";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>Hub connection string</summary>
        public string EdgeHubConnectionString =>
            GetStringOrDefault(kEdgeHubConnectionStringKey);
        /// <summary>Whether to bypass cert validation</summary>
        public bool BypassCertVerification =>
            GetBoolOrDefault(kBypassCertVerificationKey, () => false);
        /// <summary>Whether to enable metrics collection</summary>
        public bool EnableMetrics  =>
            GetBoolOrDefault(kEnableMetricsKey, () => true);
        /// <summary>Transports to use</summary>
        public TransportOption Transport => (TransportOption)Enum.Parse(typeof(TransportOption),
            GetStringOrDefault(kTransportKey, () => nameof(TransportOption.MqttOverTcp)), true);

        /// <summary>
        /// Create configuration
        /// </summary>
        /// <param name="configuration"></param>
        public ModuleConfig(IConfiguration configuration = null) :
            base(configuration) {
        }
    }
}
