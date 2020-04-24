// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// SignalR configuration
    /// </summary>
    public class SignalRServiceConfig : ConfigBase, ISignalRServiceConfig {

        private const string kSignalRConnectionStringKey = "SignalR:ConnectionString";
        private const string kSignalRServiceModeKey = "SignalR:ServiceMode";
        private const string kSignalRServerLessMode = "Serverless";

        /// <inheritdoc/>
        public string SignalRConnString => GetStringOrDefault(kSignalRConnectionStringKey,
            () => GetStringOrDefault(PcsVariable.PCS_SIGNALR_CONNSTRING));
        /// <inheritdoc/>
        public bool SignalRServerLess =>
            SignalRServiceMode.EqualsIgnoreCase(kSignalRServerLessMode);
        /// <summary>Mode string</summary>
        public string SignalRServiceMode => GetStringOrDefault(kSignalRServiceModeKey,
            () => GetStringOrDefault(PcsVariable.PCS_SIGNALR_MODE, () => kSignalRServerLessMode));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public SignalRServiceConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
