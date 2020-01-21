// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Server.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Jwt token validator
    /// </summary>
    public class JwtTokenValidator : ITokenValidator {

        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public JwtTokenValidator(IAuthConfig config, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <param name="jwtToken"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> ValidateAsync(string jwtToken,
            CancellationToken ct) {
            if (jwtToken == null) {
                throw new ArgumentNullException(nameof(jwtToken));
            }
            await RefreshSigningKeys(ct);
            var issuer = _issuer;
            var signingKeys = _signingKeys;
            if (string.IsNullOrEmpty(issuer)) {
                return null;
            }
            try {
                // Validate token.
                var claimsPrincipal = _tokenHandler.ValidateToken(jwtToken,
                    new TokenValidationParameters {
                        ValidAudiences = new string[] { _config.Audience },
                        ValidIssuers = new string[] { issuer, $"{issuer}/v2.0" },
                        IssuerSigningKeys = signingKeys
                    }, out var validatedToken);

                if (validatedToken is JwtSecurityToken validateJwt) {
                    return validateJwt.ToTokenResult();
                }
                return null;
            }
            catch (SecurityTokenValidationException ex) {
                _logger.Debug(ex, "Token validation exception");
                return null;
            }
        }

        /// <summary>
        /// The issuer and signingKeys are cached for 24 hours. They are updated if
        /// time expired or they are have not yet been retrieved.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RefreshSigningKeys(CancellationToken ct) {
            if (DateTime.UtcNow.Subtract(_stsMetadataRetrievalTime).TotalHours > 24 ||
                string.IsNullOrEmpty(_issuer) || _signingKeys == null) {

                // Get tenant information that's used to validate incoming jwt tokens
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{_config.GetAuthorityUrl()}/v2.0/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());

                var config = await configManager.GetConfigurationAsync(ct);
                _issuer = config.Issuer;
                _signingKeys = config.SigningKeys;
                _stsMetadataRetrievalTime = DateTime.UtcNow;
            }
        }

        private string _issuer;
        private ICollection<SecurityKey> _signingKeys;
        private DateTime _stsMetadataRetrievalTime = DateTime.MinValue;

        private readonly IAuthConfig _config;
        private readonly ILogger _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;
    }
}
