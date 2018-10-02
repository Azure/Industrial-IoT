// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Api;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils
{
    public class OpcVaultLoginCredentials : ServiceClientCredentials
    {
        private OpcVaultApiOptions opcVaultOptions;
        private AzureADOptions azureADOptions;
        private ITokenCacheService tokenCacheService;
        private ClaimsPrincipal claimsPrincipal;

        public OpcVaultLoginCredentials(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService,
            ClaimsPrincipal claimsPrincipal)
        {
            this.opcVaultOptions = opcVaultOptions;
            this.azureADOptions = azureADOptions;
            this.tokenCacheService = tokenCacheService;
            this.claimsPrincipal = claimsPrincipal;
        }
        private string AuthenticationToken { get; set; }
        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            var tokenCache = tokenCacheService.GetCacheAsync(claimsPrincipal).Result;

            var authenticationContext =
                new AuthenticationContext(azureADOptions.Instance + azureADOptions.TenantId, tokenCache);

            var credential = new ClientCredential(
                clientId: azureADOptions.ClientId,
                clientSecret: azureADOptions.ClientSecret);

            var name = claimsPrincipal.FindFirstValue(ClaimTypes.Upn) ??
                claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            string userObjectId = (claimsPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
            var user = new UserIdentifier(userObjectId, UserIdentifierType.UniqueId);

            var result = authenticationContext.AcquireTokenSilentAsync(
                        resource: opcVaultOptions.ResourceId,
                        clientCredential: credential,
                        userId: user).GetAwaiter().GetResult();

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            AuthenticationToken = result.AccessToken;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (AuthenticationToken == null)
            {
                throw new InvalidOperationException("Token Provider Cannot Be Null");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }

}
