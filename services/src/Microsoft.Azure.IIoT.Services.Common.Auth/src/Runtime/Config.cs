// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Auth.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IItemContainerConfig, ICosmosDbConfig,
        IForwardedHeadersConfig /*IIdentityServerConfig*/ {

        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public int? ThroughputUnits => _cosmos.ThroughputUnits;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(
            PcsVariable.PCS_JOBS_SERVICE_PATH_BASE, () => _host.ServicePathBase);

        /// <summary>
        /// Whether to use role based access
        /// </summary>
        public bool UseRoles => GetBoolOrDefault(PcsVariable.PCS_AUTH_ROLES);

        /// <inheritdoc/>
        public bool AspNetCoreForwardedHeadersEnabled =>
            _fh.AspNetCoreForwardedHeadersEnabled;
        /// <inheritdoc/>
        public int AspNetCoreForwardedHeadersForwardLimit =>
            _fh.AspNetCoreForwardedHeadersForwardLimit;

     //   /// <inheritdoc/>
     //   public IEnumerable<Client> Clients => new List<Client> {
     //       new Client {
     //           ClientId = "cli",
     //           ClientName = "Command line interface",
     //           // no interactive user, use the clientid/secret for authentication
     //           AllowedGrantTypes = GrantTypes.ClientCredentials,
     //           // secret for authentication
     //           ClientSecrets = {
     //               new Secret(GetStringOrDefault("PCS_CLI_CLIENT_SECRET", () => "cli".Sha256()))
     //           },
     //           // scopes that client has access to
     //           AllowedScopes = {
     //               _auth.Audience
     //           }
     //       },
     //       new Client {
     //           ClientId = "frontend",
     //           ClientName = "Engineering tool",
     //           ClientUri = "http://identityserver.io",
     //           AllowedGrantTypes = GrantTypes.Hybrid,
     //           AllowOfflineAccess = true,
     //           ClientSecrets = {
     //               new Secret(GetStringOrDefault("PCS_CLI_CLIENT_SECRET", () => "cli".Sha256()))
     //           },
     //           RedirectUris = {
     //               "http://localhost:21402/signin-oidc"
     //           },
     //           PostLogoutRedirectUris = {
     //               "http://localhost:21402/"
     //           },
     //           FrontChannelLogoutUri = "http://localhost:21402/signout-oidc",
     //           AllowedScopes = {
     //               IdentityServerConstants.StandardScopes.OpenId,
     //               IdentityServerConstants.StandardScopes.Profile,
     //               IdentityServerConstants.StandardScopes.Email,
     //               _auth.Audience
     //           },
     //       }
     //   };
     //
     //   /// <inheritdoc/>
     //   public IEnumerable<ApiResource> Apis => new List<ApiResource> {
     //       new ApiResource(_auth.Audience, _auth.Audience)
     //   };
     //
     //   /// <inheritdoc/>
     //   public IEnumerable<IdentityResource> Ids => null;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _host = new WebHostConfig(configuration);
            _cosmos = new CosmosDbConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
        }

        private readonly WebHostConfig _host;
        private readonly CosmosDbConfig _cosmos;
        private readonly ForwardedHeadersConfig _fh;
    }
}
