// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : LogConfig, IModuleConfig {

        /// <summary>
        /// Module configuration
        /// </summary>
        private const string EDGEHUB_CONNSTRING_KEY = "EdgeHubConnectionString";
        /// <summary>Hub connection string</summary>
        public string EdgeHubConnectionString =>
            GetString(EDGEHUB_CONNSTRING_KEY);
        /// <summary>Whether to bypass cert validation</summary>
        public bool BypassCertVerification =>
            GetBool(nameof(BypassCertVerification));
        /// <summary>Transports to use</summary>
        public TransportOption Transport => Enum.Parse<TransportOption>(
            GetString(nameof(Transport), nameof(TransportOption.Amqp)), true);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(Uptime.ProcessId, configuration) {
        }
    }
}
