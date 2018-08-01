// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using System;
    using System.Net;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable User credentials
    /// </summary>
    public class UserLoginCredentials : TokenProviderCredentials {

        /// <summary>
        /// Create device code cred provider with callback
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="config"></param>
        public UserLoginCredentials(Func<string> user, Func<SecureString> password,
            IClientConfig config) : base (config) {
            if (string.IsNullOrEmpty(config.ClientId)) {
                throw new InvalidConfigurationException(
                    "User credential token provider was not configured with " +
                    "a client id.  No credentials can be created.");
            }
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        /// <inheritdoc/>
        public override Task<Rest.TokenCredentials> GetTokenCredentialsAsync(
            string resource) {
            throw new NotSupportedException(
                "Cannot get user credential tokens on .net core");
        }

        /// <inheritdoc/>
        protected override Task<AzureCredentials> CreateCredentialsAsync(
            AzureEnvironment environment) {
            var cred = new NetworkCredential(_user(), _password());
            return Task.FromResult(new AzureCredentials(new UserLoginInformation {
                ClientId = _config.ClientId,
                UserName = cred.UserName,
                Password = cred.Password
            }, _config.TenantId ?? "common", environment));
        }

        private readonly Func<string> _user;
        private readonly Func<SecureString> _password;
    }
}
