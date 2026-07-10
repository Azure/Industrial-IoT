// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Default Azure credential provider.
    /// </summary>
    public sealed class DefaultAzureCredentials : ICredentialProvider
    {
        /// <inheritdoc/>
        public TokenCredential Credential { get; }

        /// <summary>
        /// Create azure credentials.
        /// </summary>
        /// <param name="options"></param>
        public DefaultAzureCredentials(IOptions<CredentialOptions> options)
        {
            Credential = new DefaultAzureCredential(
                options.Value.AllowInteractiveLogin ?? false);
        }
    }
}

