// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth
{
    /// <inheritdoc/>
    public class KeyVaultCredentials : ServiceClientCredentials
    {
        string authority;
        string bearerToken;
        string resourceId;
        string clientId;
        string clientSecret;

        /// <inheritdoc/>
        public KeyVaultCredentials(
            string bearerToken,
            string authority,
            string resourceId,
            string clientId,
            string clientSecret
            )
        {
            this.bearerToken = bearerToken;
            this.authority = authority;
            this.resourceId = resourceId;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        private string AuthenticationToken { get; set; }
        /// <inheritdoc/>
        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            var authenticationContext =
                new AuthenticationContext(authority);

            var credential = new ClientCredential(
                clientId: clientId,
                clientSecret: clientSecret);

            var user = new UserAssertion(bearerToken);

            var result = authenticationContext.AcquireTokenAsync(
                resource: resourceId,
                clientCredential: credential,
                userAssertion: user).GetAwaiter().GetResult();

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            AuthenticationToken = result.AccessToken;
        }

        /// <inheritdoc/>
        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (AuthenticationToken == null)
            {
                throw new InvalidOperationException("Token Provider Cannot Be Null");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //request.Version = new Version(apiVersion);
            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }

}
