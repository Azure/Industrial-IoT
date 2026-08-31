// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Azure credential configuration.
    /// </summary>
    public sealed class CredentialConfig : PostConfigureOptionBase<CredentialOptions>
    {
        /// <summary>
        /// Create configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public CredentialConfig(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public override void PostConfigure(string? name, CredentialOptions options)
        {
            options.AllowInteractiveLogin ??= GetBoolOrNull(kAllowInteractiveLogin);
        }

        /// <inheritdoc/>
        protected override CredentialOptions Bind()
        {
            return NormalizeLegacyBooleanAliases(
                nameof(CredentialOptions.AllowInteractiveLogin))
                .Get<CredentialOptions>() ?? new();
        }

        private const string kAllowInteractiveLogin = "PCS_ALLOW_INTERACTIVE_LOGIN";
    }
}
