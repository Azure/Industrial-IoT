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
    using System.Security;

    /// <summary>
    /// Injectable User credentials
    /// </summary>
    public class UserLoginCredentials : ICredentialProvider {

        /// <summary>
        /// Create device code cred provider with callback
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="config"></param>
        public UserLoginCredentials(Func<string> user, Func<SecureString> password,
            IClientConfig config) {
            if (string.IsNullOrEmpty(config.ClientId)) {
                throw new InvalidConfigurationException(
                    "User credential token provider was not configured with " +
                    "a client id.  No credentials can be created.");
            }
            Credentials = new AzureCredentials(new UserLoginInformation {
                ClientId = config.ClientId,
                UserName = user(),
                Password = password().ToString()
            }, config.TenantId ?? "common", AzureEnvironment.AzureGlobalCloud);
        }

        /// <inheritdoc/>
        public AzureCredentials Credentials { get; }
    }
}