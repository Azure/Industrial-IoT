// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    /// <summary>
    /// Azure credential options.
    /// </summary>
    public sealed record class CredentialOptions
    {
        /// <summary>
        /// Allow interactive login.
        /// </summary>
        public bool? AllowInteractiveLogin { get; set; }
    }
}

