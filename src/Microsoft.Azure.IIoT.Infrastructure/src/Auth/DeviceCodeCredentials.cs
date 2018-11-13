// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;

    /// <summary>
    /// Injectable device code credentials
    /// </summary>
    public class DeviceCodeCredentials : TokenProviderCredentials {

        /// <summary>
        /// Create console output device code provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeCredentials(IClientConfig config, ILogger logger) :
            this((c, exp, msg) => Console.WriteLine(msg), config, logger) {
        }

        /// <summary>
        /// Create device code cred provider with callback
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeCredentials(Action<string, DateTimeOffset, string> callback,
            IClientConfig config, ILogger logger) :
            base(new DeviceCodeTokenProvider(callback, config, null, logger), config) {
            if (string.IsNullOrEmpty(config?.AppId)) {
                throw new InvalidConfigurationException(
                    "Device code token provider was not configured with " +
                    "a client id.  No credentials can be created.");
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
        }
    }
}
