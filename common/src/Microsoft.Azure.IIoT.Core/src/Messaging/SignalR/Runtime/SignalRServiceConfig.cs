// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// SignalR configuration
    /// </summary>
    public class SignalRServiceConfig : ConfigBase, ISignalRServiceConfig {

        private const string kSignalRConnectionString = "SignalR:ConnectionString";
        private const string kSignalRHubName = "SignalR:HubName";

        /// <inheritdoc/>
        public string SignalRConnString => GetStringOrDefault(kSignalRConnectionString,
            () => GetStringOrDefault(PcsVariable.PCS_SIGNALR_CONNSTRING));
        /// <inheritdoc/>
        public string SignalRHubName=> GetStringOrDefault(kSignalRHubName,
            () => GetStringOrDefault(PcsVariable.PCS_SIGNALR_HUBNAME));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public SignalRServiceConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
