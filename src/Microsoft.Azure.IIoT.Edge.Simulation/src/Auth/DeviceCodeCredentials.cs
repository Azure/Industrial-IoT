// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Management.Auth {
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using System;

    /// <summary>
    /// Injectable device code credentials
    /// </summary>
    public class DeviceCodeCredentials : ICredentialProvider {

        /// <summary>
        /// Create console output device code provider
        /// </summary>
        /// <param name="config"></param>
        public DeviceCodeCredentials(IClientConfig config) :
            this((c, exp, msg) => Console.WriteLine(msg), config) {
        }

        /// <summary>
        /// Create device code cred provider with callback
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="config"></param>
        public DeviceCodeCredentials(Action<string, DateTimeOffset, string> callback,
            IClientConfig config) {
            if (string.IsNullOrEmpty(config?.ClientId)) {
                throw new InvalidConfigurationException(
                    "Device code token provider was not configured with " +
                    "a client id.  No credentials can be created.");
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            Credentials = new AzureCredentials(new DeviceCredentialInformation {
                ClientId = config.ClientId,
                DeviceCodeFlowHandler = codeResult => {
                    callback(codeResult.DeviceCode, codeResult.ExpiresOn,
                        codeResult.Message);
                    return true;
                }
            }, config.TenantId ?? "common", AzureEnvironment.AzureGlobalCloud);
        }

        /// <inheritdoc/>
        public AzureCredentials Credentials { get; }
    }
}