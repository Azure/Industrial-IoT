// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using global::Azure.Core;

    /// <summary>
    /// Provides credentials to authenticate to Azure services.
    /// </summary>
    public interface ICredentialProvider
    {
        /// <summary>
        /// Token credential.
        /// </summary>
        TokenCredential Credential { get; }
    }
}

